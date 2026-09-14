using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class PreparationPiecePlacement
{
    [SerializeField] private PieceDefinition definition;
    [SerializeField] private ChessPiece piece;
    [SerializeField] private Vector2Int boardPosition;
    [SerializeField] private PieceSide side;
    [SerializeField] private int cost;

    public PieceDefinition Definition => definition;
    public ChessPiece Piece => piece;
    public Vector2Int BoardPosition => boardPosition;
    public PieceSide Side => side;
    public int Cost => cost;

    public PreparationPiecePlacement(
        PieceDefinition definition,
        ChessPiece piece,
        Vector2Int boardPosition,
        PieceSide side,
        int cost)
    {
        this.definition = definition;
        this.piece = piece;
        this.boardPosition = boardPosition;
        this.side = side;
        this.cost = cost;
    }
}

public class PreparationManager : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ChessBoard chessBoard;
    [SerializeField] private PieceSide preparingSide = PieceSide.Player;

    [Header("Piece Catalog")]
    [SerializeField] private List<PieceDefinition> pieceDefinitions = new List<PieceDefinition>();
    [SerializeField] private PieceEntryUI pieceEntryPrefab;

    [Header("UI")]
    [SerializeField] private GameObject preparationPanel;
    [SerializeField] private Transform pieceListParent;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text selectedPieceText;
    [SerializeField] private Button readyButton;

    [Header("Runtime Placement")]
    [SerializeField] private List<PreparationPiecePlacement> placedPieces =
        new List<PreparationPiecePlacement>();
    [Min(0f)] [SerializeField] private float cpuPlacementDelay = 0.15f;
    [Min(1)] [SerializeField] private int cpuPlacementAttemptLimit = 100;

    public PieceDefinition SelectedPieceDefinition { get; private set; }
    public PieceSide CurrentPreparationSide => preparingSide;
    public PieceSide PreparingSide => preparingSide;
    public IReadOnlyList<PreparationPiecePlacement> PlacedPieces => placedPieces;
    public int PlacedKingCount => CountPlacedKings(preparingSide);
    public bool IsCpuPreparing => isCpuPreparing;

    private bool isCpuPreparing;
    private Coroutine cpuPreparationRoutine;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (chessBoard == null)
        {
            chessBoard = FindFirstObjectByType<ChessBoard>();
        }
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.PhaseChanged += HandlePhaseChanged;
        }

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }
    }

    private void Start()
    {
        RebuildPieceList();
        BeginPreparationForSide(PieceSide.Player);
    }

    private void Update()
    {
        if (!IsPreparationActive() ||
            isCpuPreparing ||
            gameManager.IsRandomCpu(preparingSide) ||
            Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        Camera inputCamera = Camera.main;
        if (inputCamera == null)
        {
            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = inputCamera.ScreenToWorldPoint(screenPosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);

        ChessPiece clickedPiece = null;
        BoardTile clickedTile = null;

        foreach (Collider2D hit in hits)
        {
            if (clickedPiece == null)
            {
                clickedPiece = hit.GetComponent<ChessPiece>();
            }

            if (clickedTile == null)
            {
                clickedTile = hit.GetComponent<BoardTile>();
            }
        }

        if (clickedPiece != null && TryRemovePreparationPiece(clickedPiece))
        {
            return;
        }

        if (clickedTile != null)
        {
            TryPlaceSelectedPiece(clickedTile.X, clickedTile.Y);
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.PhaseChanged -= HandlePhaseChanged;
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(OnReadyButtonClicked);
        }

        if (cpuPreparationRoutine != null)
        {
            StopCoroutine(cpuPreparationRoutine);
            cpuPreparationRoutine = null;
        }

        isCpuPreparing = false;
    }

    public void SetPreparingSide(PieceSide side)
    {
        BeginPreparationForSide(side);
    }

    public void SelectPiece(PieceDefinition definition)
    {
        if (isCpuPreparing ||
            gameManager == null ||
            gameManager.IsRandomCpu(preparingSide))
        {
            return;
        }

        SelectedPieceDefinition = definition;
        RefreshUI();
    }

    public void RebuildPieceList()
    {
        if (pieceListParent == null || pieceEntryPrefab == null)
        {
            return;
        }

        for (int i = pieceListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(pieceListParent.GetChild(i).gameObject);
        }

        foreach (PieceDefinition definition in pieceDefinitions)
        {
            if (definition == null)
            {
                continue;
            }

            PieceEntryUI entry = Instantiate(pieceEntryPrefab, pieceListParent);
            entry.name = $"{definition.DisplayName}Entry";
            entry.gameObject.SetActive(true);
            entry.Initialize(definition, SelectPiece);
        }
    }

    public bool CanCompletePreparation()
    {
        return gameManager != null &&
               CountPlacedKings(preparingSide) == 1 &&
               gameManager.GetCurrentPoints(preparingSide) >= 0;
    }

    public void OnReadyButtonClicked()
    {
        CompleteCurrentPreparation();
    }

    public bool CompleteCurrentPreparation()
    {
        if (!CanCompletePreparation())
        {
            Debug.Log($"Preparation is not complete for {preparingSide}: exactly one King and a valid point total are required.");
            return false;
        }

        PieceSide completedSide = preparingSide;
        Debug.Log($"Preparation complete: {completedSide}");

        isCpuPreparing = false;
        SelectedPieceDefinition = null;
        chessBoard.SetPreparationZoneHighlight(completedSide, false);

        if (completedSide == PieceSide.Player)
        {
            BeginPreparationForSide(PieceSide.Enemy);
        }
        else
        {
            RefreshUI();
            gameManager.StartBattle();
        }

        return true;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Battle)
        {
            SelectedPieceDefinition = null;
            isCpuPreparing = false;
        }

        RefreshUI();
        RefreshPreparationHighlight();
    }

    public bool IsInPreparationZone(int boardY, PieceSide side)
    {
        if (chessBoard == null)
        {
            return false;
        }

        return side == PieceSide.Player
            ? boardY == 0 || boardY == 1
            : boardY == chessBoard.BoardHeight - 2 || boardY == chessBoard.BoardHeight - 1;
    }

    public bool TryPlaceSelectedPiece(int boardX, int boardY)
    {
        if (!IsPreparationActive() ||
            isCpuPreparing ||
            gameManager.IsRandomCpu(preparingSide) ||
            SelectedPieceDefinition == null ||
            chessBoard == null ||
            !chessBoard.IsInsideBoard(boardX, boardY) ||
            !IsInPreparationZone(boardY, preparingSide) ||
            !chessBoard.IsCellEmpty(boardX, boardY) ||
            (SelectedPieceDefinition.IsKing && CountPlacedKings(preparingSide) >= 1))
        {
            return false;
        }

        return TryPlaceDefinition(SelectedPieceDefinition, boardX, boardY);
    }

    private bool TryPlaceDefinition(PieceDefinition definition, int boardX, int boardY)
    {
        if (!IsPreparationActive() ||
            definition == null ||
            chessBoard == null ||
            !chessBoard.IsInsideBoard(boardX, boardY) ||
            !IsInPreparationZone(boardY, preparingSide) ||
            !chessBoard.IsCellEmpty(boardX, boardY) ||
            (definition.IsKing && CountPlacedKings(preparingSide) >= 1))
        {
            return false;
        }

        ChessPiece prefab = definition.GetPrefab(preparingSide);
        if (prefab == null ||
            gameManager == null ||
            !gameManager.TrySpendPoints(preparingSide, definition.Cost))
        {
            return false;
        }

        if (!chessBoard.PlacePreparationPiece(
                prefab,
                boardX,
                boardY,
                preparingSide,
                out ChessPiece placedPiece))
        {
            gameManager.RefundPoints(preparingSide, definition.Cost);
            return false;
        }

        placedPieces.Add(new PreparationPiecePlacement(
            definition,
            placedPiece,
            new Vector2Int(boardX, boardY),
            preparingSide,
            definition.Cost
        ));

        RefreshUI();
        return true;
    }

    public bool TryRemovePreparationPiece(ChessPiece piece)
    {
        if (!IsPreparationActive() || piece == null || piece.Side != preparingSide)
        {
            return false;
        }

        PreparationPiecePlacement placement = placedPieces.Find(
            item => item.Piece == piece && item.Side == preparingSide
        );

        if (placement == null || !chessBoard.RemovePreparationPiece(piece))
        {
            return false;
        }

        placedPieces.Remove(placement);
        gameManager.RefundPoints(preparingSide, placement.Cost);
        RefreshUI();
        return true;
    }

    private int CountPlacedKings(PieceSide side)
    {
        int count = 0;

        foreach (PreparationPiecePlacement placement in placedPieces)
        {
            if (placement.Side == side &&
                placement.Definition != null &&
                placement.Definition.IsKing &&
                placement.Piece != null)
            {
                count++;
            }
        }

        return count;
    }

    private bool IsPreparationActive()
    {
        return gameManager != null && gameManager.CurrentPhase == GamePhase.Preparation;
    }

    public List<PreparationPiecePlacement> GetPlacements(PieceSide side)
    {
        return placedPieces.FindAll(placement => placement.Side == side);
    }

    private void BeginPreparationForSide(PieceSide side)
    {
        if (!IsPreparationActive())
        {
            return;
        }

        if (chessBoard != null)
        {
            chessBoard.SetPreparationZoneHighlight(preparingSide, false);
        }

        preparingSide = side;
        SelectedPieceDefinition = null;
        isCpuPreparing = gameManager != null && gameManager.IsRandomCpu(side);
        RefreshUI();
        RefreshPreparationHighlight();

        if (isCpuPreparing)
        {
            cpuPreparationRoutine = StartCoroutine(RunCpuPreparation(side));
        }
    }

    private IEnumerator RunCpuPreparation(PieceSide side)
    {
        while (chessBoard == null || !chessBoard.IsInitialized)
        {
            yield return null;
        }

        PieceDefinition king = pieceDefinitions.Find(definition =>
            definition != null && definition.IsKing
        );

        List<Vector2Int> emptyCells = GetEmptyPreparationCells(side);
        if (king == null || emptyCells.Count == 0)
        {
            Debug.LogError($"CPU preparation failed for {side}: King definition or deployment cell is missing.");
            isCpuPreparing = false;
            RefreshUI();
            yield break;
        }

        Vector2Int kingCell = TakeRandomCell(emptyCells);
        if (!TryPlaceDefinition(king, kingCell.x, kingCell.y))
        {
            Debug.LogError($"CPU preparation failed for {side}: King could not be placed.");
            isCpuPreparing = false;
            RefreshUI();
            yield break;
        }

        if (cpuPlacementDelay > 0f)
        {
            yield return new WaitForSeconds(cpuPlacementDelay);
        }

        int attempts = 0;
        int nonKingPlaced = 0;

        while (emptyCells.Count > 0 && attempts < cpuPlacementAttemptLimit)
        {
            attempts++;
            int points = gameManager.GetCurrentPoints(side);
            List<PieceDefinition> affordable = pieceDefinitions.FindAll(definition =>
                definition != null &&
                !definition.IsKing &&
                definition.Cost <= points &&
                definition.GetPrefab(side) != null
            );

            if (affordable.Count == 0)
            {
                break;
            }

            if (nonKingPlaced >= 5 && Random.value < 0.2f)
            {
                break;
            }

            PieceDefinition choice = affordable[Random.Range(0, affordable.Count)];
            Vector2Int cell = TakeRandomCell(emptyCells);
            if (TryPlaceDefinition(choice, cell.x, cell.y))
            {
                nonKingPlaced++;
                if (cpuPlacementDelay > 0f)
                {
                    yield return new WaitForSeconds(cpuPlacementDelay);
                }
            }
        }

        cpuPreparationRoutine = null;
        if (!CanCompletePreparation())
        {
            Debug.LogError($"CPU preparation failed validation for {side}.");
            isCpuPreparing = false;
            RefreshUI();
            yield break;
        }

        CompleteCurrentPreparation();
    }

    private List<Vector2Int> GetEmptyPreparationCells(PieceSide side)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        int firstRow = side == PieceSide.Player ? 0 : chessBoard.BoardHeight - 2;
        int lastRow = firstRow + 1;

        for (int y = firstRow; y <= lastRow; y++)
        {
            for (int x = 0; x < chessBoard.BoardWidth; x++)
            {
                if (chessBoard.IsCellEmpty(x, y))
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }
        }

        return cells;
    }

    private Vector2Int TakeRandomCell(List<Vector2Int> cells)
    {
        int index = Random.Range(0, cells.Count);
        Vector2Int cell = cells[index];
        cells.RemoveAt(index);
        return cell;
    }

    private void RefreshPreparationHighlight()
    {
        if (chessBoard != null)
        {
            chessBoard.SetPreparationZoneHighlight(preparingSide, IsPreparationActive());
        }
    }

    private void RefreshUI()
    {
        bool isPreparation = gameManager != null && gameManager.CurrentPhase == GamePhase.Preparation;

        if (preparationPanel != null)
        {
            preparationPanel.SetActive(isPreparation);
        }

        if (titleText != null)
        {
            string sideName = preparingSide == PieceSide.Player ? "White" : "Black";
            titleText.text = isCpuPreparing
                ? $"{sideName} Preparation - Preparing CPU..."
                : $"{sideName} Preparation";
        }

        if (pointsText != null)
        {
            int points = gameManager != null ? gameManager.GetCurrentPoints(preparingSide) : 0;
            pointsText.text = $"Points: {points}";
        }

        if (selectedPieceText != null)
        {
            string selection = isCpuPreparing
                ? "Preparing CPU..."
                : SelectedPieceDefinition == null
                ? "Selected: None"
                : $"Selected: {SelectedPieceDefinition.DisplayName}";
            string kingStatus = CountPlacedKings(preparingSide) == 1
                ? "King: 1/1"
                : "King: Missing";
            selectedPieceText.text = $"{selection}\n{kingStatus}";
        }

        bool humanCanInteract = IsPreparationActive() &&
                                !isCpuPreparing &&
                                gameManager != null &&
                                !gameManager.IsRandomCpu(preparingSide);

        if (pieceListParent != null)
        {
            foreach (Button button in pieceListParent.GetComponentsInChildren<Button>(true))
            {
                button.interactable = humanCanInteract;
            }
        }

        if (readyButton != null)
        {
            readyButton.interactable = humanCanInteract && CanCompletePreparation();
        }
    }
}

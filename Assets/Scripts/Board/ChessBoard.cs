using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TurnActionState
{
    Action,
    TurnChange
}

public class ChessBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int boardWidth = 10;
    [SerializeField] private int boardHeight = 10;
    [SerializeField] private float tileSize = 1f;

    [Header("Tile Colors")]
    [SerializeField] private Color lightTileColor = new Color(0.95f, 0.95f, 0.95f);
    [SerializeField] private Color darkTileColor = new Color(0.25f, 0.25f, 0.25f);

    [Header("Turn")]
    [SerializeField] private PieceSide currentTurn = PieceSide.Player;
    [SerializeField] private TurnActionState turnActionState = TurnActionState.Action;

    [Header("Game State")]
    [SerializeField] private bool isGameOver;
    [SerializeField] private PieceSide winner;

    [Header("Board References")]
    [SerializeField] private BoardTile tilePrefab;
    [SerializeField] private Camera boardCamera;
    [SerializeField] private GameManager gameManager;

    [Header("White Piece Prefabs")]
    [SerializeField] private ChessPiece pawnWhitePrefab;
    [SerializeField] private ChessPiece rookWhitePrefab;
    [SerializeField] private ChessPiece knightWhitePrefab;
    [SerializeField] private ChessPiece bishopWhitePrefab;
    [SerializeField] private ChessPiece queenWhitePrefab;
    [SerializeField] private ChessPiece kingWhitePrefab;

    [Header("Black Piece Prefabs")]
    [SerializeField] private ChessPiece pawnBlackPrefab;
    [SerializeField] private ChessPiece rookBlackPrefab;
    [SerializeField] private ChessPiece knightBlackPrefab;
    [SerializeField] private ChessPiece bishopBlackPrefab;
    [SerializeField] private ChessPiece queenBlackPrefab;
    [SerializeField] private ChessPiece kingBlackPrefab;

    [Header("Camera Settings")]
    [SerializeField] private float cameraPadding = 0.5f;

    public PieceSide CurrentTurn => currentTurn;
    public TurnActionState CurrentTurnActionState => turnActionState;
    public bool IsGameOver => isGameOver;
    public PieceSide Winner => winner;
    public int BoardWidth => boardWidth;
    public int BoardHeight => boardHeight;
    public bool IsInitialized => tiles != null && pieces != null;

    private BoardTile[,] tiles;
    private ChessPiece[,] pieces;
    private ChessPiece selectedPiece;
    private bool cpuIsActing;

    private void Start()
    {
        tiles = new BoardTile[boardWidth, boardHeight];
        pieces = new ChessPiece[boardWidth, boardHeight];

        currentTurn = PieceSide.Player;
        turnActionState = TurnActionState.Action;
        isGameOver = false;
        cpuIsActing = false;

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            gameManager.PhaseChanged += HandleGamePhaseChanged;
        }

        CreateBoard();
        if (gameManager == null || gameManager.CurrentPhase == GamePhase.Battle)
        {
            SpawnStartingPieces();
        }
        CenterCameraOnBoard();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.PhaseChanged -= HandleGamePhaseChanged;
        }
    }

    private void HandleGamePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Preparation)
        {
            ResetBoardForPreparation();
            return;
        }

        if (phase != GamePhase.Battle)
        {
            return;
        }

        currentTurn = PieceSide.Player;
        turnActionState = TurnActionState.Action;
        isGameOver = false;
        cpuIsActing = false;
        ClearSelection();
    }

    private void ResetBoardForPreparation()
    {
        StopAllCoroutines();
        cpuIsActing = false;
        isGameOver = false;
        currentTurn = PieceSide.Player;
        turnActionState = TurnActionState.Action;
        ClearSelection();
        ClearAllPieces();
    }

    private void Update()
    {
        if (!IsBattlePhase())
        {
            return;
        }

        if (isGameOver)
        {
            return;
        }

        if (turnActionState == TurnActionState.TurnChange)
        {
            ChangeTurn();
            return;
        }

        if (IsCurrentTurnCpu())
        {
            if (!cpuIsActing)
            {
                StartCoroutine(PlayCpuTurn(currentTurn));
            }

            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        HandleMouseClick();
    }

    private bool IsCurrentTurnCpu()
    {
        return gameManager != null && gameManager.IsRandomCpu(currentTurn);
    }

    private IEnumerator PlayCpuTurn(PieceSide cpuSide)
    {
        cpuIsActing = true;

        List<ChessPiece> movablePieces = new List<ChessPiece>();

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                ChessPiece piece = pieces[x, y];

                if (piece == null || piece.Side != cpuSide)
                {
                    continue;
                }

                List<Vector2Int> legalMoves = piece.GetLegalMoves(this);
                if (legalMoves.Count > 0)
                {
                    movablePieces.Add(piece);
                }
            }
        }

        if (movablePieces.Count == 0)
        {
            Debug.Log($"{cpuSide} CPU has no legal moves. Turn skipped.");
            cpuIsActing = false;
            RequestTurnChange();
            yield break;
        }

        ChessPiece chosenPiece = movablePieces[Random.Range(0, movablePieces.Count)];
        SelectPiece(chosenPiece);

        float delay = gameManager != null ? gameManager.CpuMoveDelay : 0.6f;
        yield return new WaitForSeconds(delay);

        if (!IsBattlePhase() || isGameOver || currentTurn != cpuSide || selectedPiece != chosenPiece)
        {
            cpuIsActing = false;
            yield break;
        }

        List<Vector2Int> chosenMoves = chosenPiece.GetLegalMoves(this);

        if (chosenMoves.Count == 0)
        {
            ClearSelection();
            cpuIsActing = false;
            RequestTurnChange();
            yield break;
        }

        Vector2Int target = chosenMoves[Random.Range(0, chosenMoves.Count)];
        MoveSelectedPiece(target.x, target.y);

        cpuIsActing = false;
    }

    private void CreateBoard()
    {
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                Vector3 spawnPosition = GetWorldPosition(x, y);

                BoardTile tile = Instantiate(
                    tilePrefab,
                    spawnPosition,
                    Quaternion.identity,
                    transform
                );

                bool isLightTile = (x + y) % 2 == 0;
                Color tileColor = isLightTile ? lightTileColor : darkTileColor;

                tile.Initialize(x, y, tileColor);
                tiles[x, y] = tile;
            }
        }
    }

    private void SpawnStartingPieces()
    {
        if (boardWidth < 8 || boardHeight < 4)
        {
            Debug.LogWarning("通常チェスの初期配置には横8マス以上、縦4マス以上が必要です。");
            return;
        }

        if (!ArePiecePrefabsAssigned())
        {
            Debug.LogWarning("白黒12種類の駒PrefabをChessBoardに設定してください。");
            return;
        }

        int startX = (boardWidth - 8) / 2;

        int whiteBackRow = 0;
        int whitePawnRow = 1;
        int blackBackRow = boardHeight - 1;
        int blackPawnRow = boardHeight - 2;

        SpawnBackRank(
            PieceSide.Player,
            whiteBackRow,
            startX,
            rookWhitePrefab,
            knightWhitePrefab,
            bishopWhitePrefab,
            queenWhitePrefab,
            kingWhitePrefab
        );

        for (int i = 0; i < 8; i++)
        {
            SpawnPiece(pawnWhitePrefab, startX + i, whitePawnRow, PieceSide.Player);
        }

        SpawnBackRank(
            PieceSide.Enemy,
            blackBackRow,
            startX,
            rookBlackPrefab,
            knightBlackPrefab,
            bishopBlackPrefab,
            queenBlackPrefab,
            kingBlackPrefab
        );

        for (int i = 0; i < 8; i++)
        {
            SpawnPiece(pawnBlackPrefab, startX + i, blackPawnRow, PieceSide.Enemy);
        }
    }

    private void SpawnBackRank(
        PieceSide side,
        int row,
        int startX,
        ChessPiece rookPrefab,
        ChessPiece knightPrefab,
        ChessPiece bishopPrefab,
        ChessPiece queenPrefab,
        ChessPiece kingPrefab)
    {
        SpawnPiece(rookPrefab, startX + 0, row, side);
        SpawnPiece(knightPrefab, startX + 1, row, side);
        SpawnPiece(bishopPrefab, startX + 2, row, side);
        SpawnPiece(queenPrefab, startX + 3, row, side);
        SpawnPiece(kingPrefab, startX + 4, row, side);
        SpawnPiece(bishopPrefab, startX + 5, row, side);
        SpawnPiece(knightPrefab, startX + 6, row, side);
        SpawnPiece(rookPrefab, startX + 7, row, side);
    }

    private bool ArePiecePrefabsAssigned()
    {
        return pawnWhitePrefab != null &&
               rookWhitePrefab != null &&
               knightWhitePrefab != null &&
               bishopWhitePrefab != null &&
               queenWhitePrefab != null &&
               kingWhitePrefab != null &&
               pawnBlackPrefab != null &&
               rookBlackPrefab != null &&
               knightBlackPrefab != null &&
               bishopBlackPrefab != null &&
               queenBlackPrefab != null &&
               kingBlackPrefab != null;
    }

    private void SpawnPiece(ChessPiece piecePrefab, int boardX, int boardY, PieceSide side)
    {
        Vector3 spawnPosition = GetWorldPosition(boardX, boardY);
        spawnPosition.z = -1f;

        ChessPiece piece = Instantiate(
            piecePrefab,
            spawnPosition,
            Quaternion.identity,
            transform
        );

        piece.Initialize(boardX, boardY, side);
        pieces[boardX, boardY] = piece;
    }

    private void HandleMouseClick()
    {
        Camera inputCamera = boardCamera != null ? boardCamera : Camera.main;
        if (inputCamera == null)
        {
            return;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = inputCamera.ScreenToWorldPoint(mouseScreenPosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPosition);

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

        if (clickedPiece != null)
        {
            if (selectedPiece != null &&
                clickedPiece.Side != selectedPiece.Side &&
                IsInsideBoard(clickedPiece.BoardX, clickedPiece.BoardY) &&
                tiles[clickedPiece.BoardX, clickedPiece.BoardY].IsHighlighted)
            {
                MoveSelectedPiece(clickedPiece.BoardX, clickedPiece.BoardY);
                return;
            }

            if (clickedPiece.Side == currentTurn)
            {
                SelectPiece(clickedPiece);
            }
            else
            {
                ClearSelection();
            }

            return;
        }

        if (clickedTile != null)
        {
            HandleTileClick(clickedTile);
            return;
        }

        ClearSelection();
    }

    private void SelectPiece(ChessPiece piece)
    {
        if (!IsBattlePhase() || isGameOver || piece.Side != currentTurn || turnActionState != TurnActionState.Action)
        {
            return;
        }

        if (selectedPiece == piece)
        {
            ClearSelection();
            return;
        }

        ClearSelection();

        selectedPiece = piece;
        selectedPiece.SetSelected(true);
        ShowLegalMoves(selectedPiece);
    }

    private void ShowLegalMoves(ChessPiece piece)
    {
        List<Vector2Int> legalMoves = piece.GetLegalMoves(this);

        foreach (Vector2Int move in legalMoves)
        {
            if (IsInsideBoard(move.x, move.y))
            {
                tiles[move.x, move.y].SetMoveHighlight(true);
            }
        }
    }

    private void HandleTileClick(BoardTile tile)
    {
        if (selectedPiece == null)
        {
            return;
        }

        if (!tile.IsHighlighted)
        {
            ClearSelection();
            return;
        }

        MoveSelectedPiece(tile.X, tile.Y);
    }

    private void MoveSelectedPiece(int targetX, int targetY)
    {
        if (!IsBattlePhase() || isGameOver || selectedPiece == null || turnActionState != TurnActionState.Action)
        {
            return;
        }

        ChessPiece targetPiece = pieces[targetX, targetY];
        bool capturedKing = false;

        if (targetPiece != null)
        {
            if (targetPiece.Side == selectedPiece.Side)
            {
                ClearSelection();
                return;
            }

            capturedKing = targetPiece is KingPiece;
            gameManager?.RegisterCapture(selectedPiece.Side);

            Destroy(targetPiece.gameObject);
            pieces[targetX, targetY] = null;
        }

        PieceSide movingSide = selectedPiece.Side;

        pieces[selectedPiece.BoardX, selectedPiece.BoardY] = null;

        selectedPiece.SetBoardPosition(targetX, targetY);
        selectedPiece.MarkMoved();

        Vector3 targetPosition = GetWorldPosition(targetX, targetY);
        targetPosition.z = -1f;
        selectedPiece.transform.position = targetPosition;

        pieces[targetX, targetY] = selectedPiece;

        if (capturedKing)
        {
            EndGame(movingSide);
            return;
        }

        ClearSelection();
        RequestTurnChange();
    }

    private void EndGame(PieceSide winningSide)
    {
        winner = winningSide;
        isGameOver = true;
        cpuIsActing = false;
        ClearSelection();

        Debug.Log($"Round Over! Winner: {winner}");

        if (gameManager != null)
        {
            gameManager.EndRound(winningSide);
        }
        else
        {
            Debug.Log($"Game Over! Winner: {winner}");
        }
    }

    private void RequestTurnChange()
    {
        if (isGameOver)
        {
            return;
        }

        turnActionState = TurnActionState.TurnChange;
    }

    private void ChangeTurn()
    {
        if (isGameOver)
        {
            return;
        }

        ClearSelection();
        cpuIsActing = false;

        currentTurn = currentTurn == PieceSide.Player
            ? PieceSide.Enemy
            : PieceSide.Player;

        turnActionState = TurnActionState.Action;
        Debug.Log($"Turn changed: {currentTurn}");
    }

    private void ClearSelection()
    {
        if (selectedPiece != null)
        {
            selectedPiece.SetSelected(false);
            selectedPiece = null;
        }

        ClearMoveHighlights();
    }

    private void ClearMoveHighlights()
    {
        if (tiles == null)
        {
            return;
        }

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (tiles[x, y] != null)
                {
                    tiles[x, y].SetMoveHighlight(false);
                }
            }
        }
    }

    public bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < boardWidth && y >= 0 && y < boardHeight;
    }

    public ChessPiece GetPieceAt(int x, int y)
    {
        if (!IsInsideBoard(x, y) || pieces == null)
        {
            return null;
        }

        return pieces[x, y];
    }

    public bool IsCellEmpty(int x, int y)
    {
        return IsInsideBoard(x, y) && pieces != null && pieces[x, y] == null;
    }

    public bool PlacePreparationPiece(
        ChessPiece piecePrefab,
        int boardX,
        int boardY,
        PieceSide side,
        out ChessPiece placedPiece)
    {
        placedPiece = null;

        if (piecePrefab == null ||
            gameManager == null ||
            gameManager.CurrentPhase != GamePhase.Preparation ||
            !IsCellEmpty(boardX, boardY))
        {
            return false;
        }

        Vector3 spawnPosition = GetWorldPosition(boardX, boardY);
        spawnPosition.z = -1f;

        placedPiece = Instantiate(
            piecePrefab,
            spawnPosition,
            Quaternion.identity,
            transform
        );

        placedPiece.Initialize(boardX, boardY, side);
        pieces[boardX, boardY] = placedPiece;
        return true;
    }

    public bool RemovePreparationPiece(ChessPiece piece)
    {
        if (piece == null ||
            gameManager == null ||
            gameManager.CurrentPhase != GamePhase.Preparation ||
            !IsInsideBoard(piece.BoardX, piece.BoardY) ||
            pieces[piece.BoardX, piece.BoardY] != piece)
        {
            return false;
        }

        pieces[piece.BoardX, piece.BoardY] = null;
        Destroy(piece.gameObject);
        return true;
    }

    public void ClearAllPieces()
    {
        if (pieces == null)
        {
            return;
        }

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                ChessPiece piece = pieces[x, y];
                if (piece != null)
                {
                    Destroy(piece.gameObject);
                    pieces[x, y] = null;
                }
            }
        }
    }

    public void SetPreparationZoneHighlight(PieceSide side, bool highlighted)
    {
        if (tiles == null)
        {
            return;
        }

        int firstRow = side == PieceSide.Player ? 0 : boardHeight - 2;
        int lastRow = side == PieceSide.Player ? 1 : boardHeight - 1;

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (tiles[x, y] != null)
                {
                    tiles[x, y].SetPreparationHighlight(
                        highlighted && y >= firstRow && y <= lastRow
                    );
                }
            }
        }
    }

    private bool IsBattlePhase()
    {
        return gameManager == null || gameManager.CurrentPhase == GamePhase.Battle;
    }

    private Vector3 GetWorldPosition(int boardX, int boardY)
    {
        return new Vector3(
            boardX * tileSize,
            boardY * tileSize,
            0f
        );
    }

    private void CenterCameraOnBoard()
    {
        if (boardCamera == null)
        {
            boardCamera = Camera.main;
        }

        if (boardCamera == null)
        {
            Debug.LogWarning("Main Cameraが見つかりません。");
            return;
        }

        float boardPixelWidth = boardWidth * tileSize;
        float boardPixelHeight = boardHeight * tileSize;

        float centerX = ((boardWidth - 1) * tileSize) / 2f;
        float centerY = ((boardHeight - 1) * tileSize) / 2f;

        Vector3 cameraPosition = boardCamera.transform.position;
        cameraPosition.x = centerX;
        cameraPosition.y = centerY;
        boardCamera.transform.position = cameraPosition;

        if (!boardCamera.orthographic)
        {
            return;
        }

        float verticalSize = boardPixelHeight / 2f;
        float horizontalSize = (boardPixelWidth / boardCamera.aspect) / 2f;

        boardCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize) + cameraPadding;
    }
}

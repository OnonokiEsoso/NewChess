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

    [Header("CPU")]
    [SerializeField] private bool enemyCpuEnabled = true;
    [SerializeField] private float cpuMoveDelay = 0.6f;

    [Header("Game State")]
    [SerializeField] private bool isGameOver;
    [SerializeField] private PieceSide winner;

    [Header("Board References")]
    [SerializeField] private BoardTile tilePrefab;
    [SerializeField] private Camera boardCamera;

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

        CreateBoard();
        SpawnStartingPieces();
        CenterCameraOnBoard();
    }

    private void Update()
    {
        if (isGameOver)
        {
            return;
        }

        if (turnActionState == TurnActionState.TurnChange)
        {
            ChangeTurn();
            return;
        }

        if (enemyCpuEnabled && currentTurn == PieceSide.Enemy)
        {
            if (!cpuIsActing)
            {
                StartCoroutine(PlayCpuTurn());
            }

            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        HandleMouseClick();
    }

    private IEnumerator PlayCpuTurn()
    {
        cpuIsActing = true;

        List<ChessPiece> movablePieces = new List<ChessPiece>();

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                ChessPiece piece = pieces[x, y];

                if (piece == null || piece.Side != PieceSide.Enemy)
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
            Debug.Log("CPU has no legal moves. Turn skipped.");
            cpuIsActing = false;
            RequestTurnChange();
            yield break;
        }

        ChessPiece chosenPiece = movablePieces[Random.Range(0, movablePieces.Count)];
        SelectPiece(chosenPiece);

        yield return new WaitForSeconds(cpuMoveDelay);

        if (isGameOver || currentTurn != PieceSide.Enemy || selectedPiece != chosenPiece)
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
        if (isGameOver || piece.Side != currentTurn || turnActionState != TurnActionState.Action)
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
        if (isGameOver || selectedPiece == null || turnActionState != TurnActionState.Action)
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

        Debug.Log($"Game Over! Winner: {winner}");
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

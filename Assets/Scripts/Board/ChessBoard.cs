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

    [Header("References")]
    [SerializeField] private BoardTile tilePrefab;
    [SerializeField] private ChessPiece pawnWhitePrefab;
    [SerializeField] private ChessPiece pawnBlackPrefab;
    [SerializeField] private Camera boardCamera;

    [Header("Camera Settings")]
    [SerializeField] private float cameraPadding = 0.5f;

    public PieceSide CurrentTurn => currentTurn;
    public TurnActionState CurrentTurnActionState => turnActionState;

    private BoardTile[,] tiles;
    private ChessPiece[,] pieces;
    private ChessPiece selectedPiece;

    private void Start()
    {
        tiles = new BoardTile[boardWidth, boardHeight];
        pieces = new ChessPiece[boardWidth, boardHeight];

        currentTurn = PieceSide.Player;
        turnActionState = TurnActionState.Action;

        CreateBoard();
        SpawnStartingPawns();
        CenterCameraOnBoard();
    }

    private void Update()
    {
        if (turnActionState == TurnActionState.TurnChange)
        {
            ChangeTurn();
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        HandleMouseClick();
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

    private void SpawnStartingPawns()
    {
        if (pawnWhitePrefab == null || pawnBlackPrefab == null)
        {
            Debug.LogWarning("Pawn_W または Pawn_B Prefabが設定されていません。");
            return;
        }

        int leftCenterX = (boardWidth / 2) - 1;
        int rightCenterX = boardWidth / 2;

        int playerPawnRow = 1;
        SpawnPiece(pawnWhitePrefab, leftCenterX, playerPawnRow, PieceSide.Player);
        SpawnPiece(pawnWhitePrefab, rightCenterX, playerPawnRow, PieceSide.Player);

        int enemyPawnRow = boardHeight - 2;
        SpawnPiece(pawnBlackPrefab, leftCenterX, enemyPawnRow, PieceSide.Enemy);
        SpawnPiece(pawnBlackPrefab, rightCenterX, enemyPawnRow, PieceSide.Enemy);
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
        if (piece.Side != currentTurn || turnActionState != TurnActionState.Action)
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
        ShowPawnMoves(selectedPiece);
    }

    private void ShowPawnMoves(ChessPiece piece)
    {
        int direction = piece.ForwardDirection;
        int forwardY = piece.BoardY + direction;

        if (IsInsideBoard(piece.BoardX, forwardY) && pieces[piece.BoardX, forwardY] == null)
        {
            tiles[piece.BoardX, forwardY].SetMoveHighlight(true);

            int doubleForwardY = piece.BoardY + (2 * direction);

            if (!piece.HasMoved &&
                IsInsideBoard(piece.BoardX, doubleForwardY) &&
                pieces[piece.BoardX, doubleForwardY] == null)
            {
                tiles[piece.BoardX, doubleForwardY].SetMoveHighlight(true);
            }
        }

        ShowPawnCapture(piece, piece.BoardX - 1, forwardY);
        ShowPawnCapture(piece, piece.BoardX + 1, forwardY);
    }

    private void ShowPawnCapture(ChessPiece piece, int targetX, int targetY)
    {
        if (!IsInsideBoard(targetX, targetY))
        {
            return;
        }

        ChessPiece targetPiece = pieces[targetX, targetY];

        if (targetPiece != null && targetPiece.Side != piece.Side)
        {
            tiles[targetX, targetY].SetMoveHighlight(true);
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
        if (selectedPiece == null || turnActionState != TurnActionState.Action)
        {
            return;
        }

        ChessPiece targetPiece = pieces[targetX, targetY];

        if (targetPiece != null)
        {
            if (targetPiece.Side == selectedPiece.Side)
            {
                ClearSelection();
                return;
            }

            Destroy(targetPiece.gameObject);
            pieces[targetX, targetY] = null;
        }

        pieces[selectedPiece.BoardX, selectedPiece.BoardY] = null;

        selectedPiece.SetBoardPosition(targetX, targetY);
        selectedPiece.MarkMoved();

        Vector3 targetPosition = GetWorldPosition(targetX, targetY);
        targetPosition.z = -1f;
        selectedPiece.transform.position = targetPosition;

        pieces[targetX, targetY] = selectedPiece;

        ClearSelection();
        RequestTurnChange();
    }

    private void RequestTurnChange()
    {
        turnActionState = TurnActionState.TurnChange;
    }

    private void ChangeTurn()
    {
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

    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < boardWidth && y >= 0 && y < boardHeight;
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

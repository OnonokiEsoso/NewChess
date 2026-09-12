using UnityEngine;
using UnityEngine.InputSystem;

public class ChessBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int boardWidth = 10;
    [SerializeField] private int boardHeight = 10;
    [SerializeField] private float tileSize = 1f;

    [Header("Tile Colors")]
    [SerializeField] private Color lightTileColor = new Color(0.95f, 0.95f, 0.95f);
    [SerializeField] private Color darkTileColor = new Color(0.25f, 0.25f, 0.25f);

    [Header("References")]
    [SerializeField] private BoardTile tilePrefab;
    [SerializeField] private ChessPiece pawnPrefab;
    [SerializeField] private Camera boardCamera;

    [Header("Camera Settings")]
    [SerializeField] private float cameraPadding = 0.5f;

    private BoardTile[,] tiles;
    private ChessPiece[,] pieces;
    private ChessPiece selectedPiece;

    private void Start()
    {
        tiles = new BoardTile[boardWidth, boardHeight];
        pieces = new ChessPiece[boardWidth, boardHeight];

        CreateBoard();
        SpawnStartingPawns();
        CenterCameraOnBoard();
    }

    private void Update()
    {
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
        if (pawnPrefab == null)
        {
            Debug.LogWarning("Pawn Prefabが設定されていません。");
            return;
        }

        int pawnRow = 1;
        int leftCenterX = (boardWidth / 2) - 1;
        int rightCenterX = boardWidth / 2;

        SpawnPiece(pawnPrefab, leftCenterX, pawnRow);
        SpawnPiece(pawnPrefab, rightCenterX, pawnRow);
    }

    private void SpawnPiece(ChessPiece piecePrefab, int boardX, int boardY)
    {
        Vector3 spawnPosition = GetWorldPosition(boardX, boardY);
        spawnPosition.z = -1f;

        ChessPiece piece = Instantiate(
            piecePrefab,
            spawnPosition,
            Quaternion.identity,
            transform
        );

        piece.Initialize(boardX, boardY);
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
            SelectPiece(clickedPiece);
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
        int forwardY = piece.BoardY + 1;

        if (IsInsideBoard(piece.BoardX, forwardY) && pieces[piece.BoardX, forwardY] == null)
        {
            tiles[piece.BoardX, forwardY].SetMoveHighlight(true);

            int doubleForwardY = piece.BoardY + 2;

            if (!piece.HasMoved &&
                IsInsideBoard(piece.BoardX, doubleForwardY) &&
                pieces[piece.BoardX, doubleForwardY] == null)
            {
                tiles[piece.BoardX, doubleForwardY].SetMoveHighlight(true);
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
        if (selectedPiece == null)
        {
            return;
        }

        pieces[selectedPiece.BoardX, selectedPiece.BoardY] = null;

        selectedPiece.SetBoardPosition(targetX, targetY);
        selectedPiece.MarkMoved();

        Vector3 targetPosition = GetWorldPosition(targetX, targetY);
        targetPosition.z = -1f;
        selectedPiece.transform.position = targetPosition;

        pieces[targetX, targetY] = selectedPiece;

        ClearSelection();
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

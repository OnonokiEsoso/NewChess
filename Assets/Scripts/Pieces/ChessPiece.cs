using System.Collections.Generic;
using UnityEngine;

public enum PieceSide
{
    Player,
    Enemy
}

public class ChessPiece : MonoBehaviour
{
    public int BoardX { get; private set; }
    public int BoardY { get; private set; }
    public bool IsSelected { get; private set; }
    public bool HasMoved { get; private set; }
    public PieceSide Side { get; private set; }

    public int ForwardDirection => Side == PieceSide.Player ? 1 : -1;

    [Header("Selection")]
    [SerializeField] private Color selectedColor = Color.yellow;

    private SpriteRenderer spriteRenderer;
    private Color normalColor;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }
    }

    public void Initialize(int boardX, int boardY, PieceSide side)
    {
        Side = side;
        SetBoardPosition(boardX, boardY);
        HasMoved = false;
        IsSelected = false;

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }
    }

    public void SetBoardPosition(int boardX, int boardY)
    {
        BoardX = boardX;
        BoardY = boardY;
        gameObject.name = $"{Side}_{GetType().Name}_{boardX}_{boardY}";
    }

    public void MarkMoved()
    {
        HasMoved = true;
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = selected ? selectedColor : normalColor;
        }
    }

    public virtual List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        return new List<Vector2Int>();
    }

    protected void AddMoveIfValid(ChessBoard board, List<Vector2Int> moves, int x, int y)
    {
        if (!board.IsInsideBoard(x, y))
        {
            return;
        }

        ChessPiece target = board.GetPieceAt(x, y);
        if (target == null || target.Side != Side)
        {
            moves.Add(new Vector2Int(x, y));
        }
    }

    protected void AddRayMoves(ChessBoard board, List<Vector2Int> moves, int dx, int dy)
    {
        int x = BoardX + dx;
        int y = BoardY + dy;

        while (board.IsInsideBoard(x, y))
        {
            ChessPiece target = board.GetPieceAt(x, y);

            if (target == null)
            {
                moves.Add(new Vector2Int(x, y));
            }
            else
            {
                if (target.Side != Side)
                {
                    moves.Add(new Vector2Int(x, y));
                }

                break;
            }

            x += dx;
            y += dy;
        }
    }
}

using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    public int BoardX { get; private set; }
    public int BoardY { get; private set; }
    public bool IsSelected { get; private set; }

    private static ChessPiece selectedPiece;

    [Header("Selection")]
    [SerializeField] private Color selectedColor = Color.yellow;

    private SpriteRenderer spriteRenderer;
    private Color normalColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }
    }

    public void Initialize(int boardX, int boardY)
    {
        SetBoardPosition(boardX, boardY);
    }

    public void SetBoardPosition(int boardX, int boardY)
    {
        BoardX = boardX;
        BoardY = boardY;

        gameObject.name = $"{GetType().Name}_{boardX}_{boardY}";
    }

    private void OnMouseDown()
    {
        if (selectedPiece == this)
        {
            Deselect();
            selectedPiece = null;
            return;
        }

        if (selectedPiece != null)
        {
            selectedPiece.Deselect();
        }

        Select();
        selectedPiece = this;
    }

    private void Select()
    {
        IsSelected = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = selectedColor;
        }
    }

    private void Deselect()
    {
        IsSelected = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }
}

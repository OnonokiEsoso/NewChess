using UnityEngine;
using UnityEngine.InputSystem;

public class ChessPiece : MonoBehaviour
{
    public int BoardX { get; private set; }
    public int BoardY { get; private set; }
    public bool IsSelected { get; private set; }

    private static ChessPiece selectedPiece;

    [Header("Selection")]
    [SerializeField] private Color selectedColor = Color.yellow;

    private SpriteRenderer spriteRenderer;
    private Collider2D pieceCollider;
    private Color normalColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        pieceCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (pieceCollider == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        if (pieceCollider.OverlapPoint(mouseWorldPosition))
        {
            ToggleSelection();
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

    private void ToggleSelection()
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

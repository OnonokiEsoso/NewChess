using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BoardTile : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsHighlighted { get; private set; }

    [Header("Highlight")]
    [SerializeField] private Color moveHighlightColor = new Color(0.35f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color preparationHighlightColor = new Color(0.25f, 0.65f, 1f, 1f);

    private SpriteRenderer spriteRenderer;
    private Color normalColor;
    private bool isPreparationHighlighted;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(int x, int y, Color tileColor)
    {
        X = x;
        Y = y;
        normalColor = tileColor;

        gameObject.name = $"Tile_{x}_{y}";
        spriteRenderer.color = normalColor;
    }

    public void SetMoveHighlight(bool highlighted)
    {
        IsHighlighted = highlighted;
        RefreshColor();
    }

    public void SetPreparationHighlight(bool highlighted)
    {
        isPreparationHighlighted = highlighted;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (IsHighlighted)
        {
            spriteRenderer.color = moveHighlightColor;
        }
        else
        {
            spriteRenderer.color = isPreparationHighlighted
                ? Color.Lerp(normalColor, preparationHighlightColor, 0.35f)
                : normalColor;
        }
    }
}

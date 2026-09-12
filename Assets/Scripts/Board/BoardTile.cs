using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BoardTile : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(int x, int y, Color tileColor)
    {
        X = x;
        Y = y;

        gameObject.name = $"Tile_{x}_{y}";
        spriteRenderer.color = tileColor;
    }
}

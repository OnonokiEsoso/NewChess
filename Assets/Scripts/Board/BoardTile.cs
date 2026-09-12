using UnityEngine;

public class BoardTile : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;

        gameObject.name = $"Tile_{x}_{y}";
    }
}

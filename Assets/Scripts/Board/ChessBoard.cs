using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int boardWidth = 10;
    [SerializeField] private int boardHeight = 10;
    [SerializeField] private float tileSize = 1f;

    [Header("References")]
    [SerializeField] private BoardTile tilePrefab;

    private void Start()
    {
        CreateBoard();
    }

    private void CreateBoard()
    {
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                Vector3 spawnPosition = new Vector3(
                    x * tileSize,
                    y * tileSize,
                    0f
                );

                BoardTile tile = Instantiate(
                    tilePrefab,
                    spawnPosition,
                    Quaternion.identity,
                    transform
                );

                tile.Initialize(x, y);
            }
        }
    }
}

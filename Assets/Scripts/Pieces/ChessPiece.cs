using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    public int BoardX { get; private set; }
    public int BoardY { get; private set; }

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
}

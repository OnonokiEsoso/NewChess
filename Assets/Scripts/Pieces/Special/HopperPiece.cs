using System.Collections.Generic;
using UnityEngine;

public class HopperPiece : ChessPiece
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        foreach (Vector2Int direction in Directions)
        {
            AddMoveIfValid(
                board,
                moves,
                BoardX + direction.x * 2,
                BoardY + direction.y * 2
            );
        }

        return moves;
    }
}

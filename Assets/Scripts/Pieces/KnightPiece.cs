using System.Collections.Generic;
using UnityEngine;

public class KnightPiece : ChessPiece
{
    private static readonly Vector2Int[] Offsets =
    {
        new Vector2Int(1, 2),
        new Vector2Int(2, 1),
        new Vector2Int(2, -1),
        new Vector2Int(1, -2),
        new Vector2Int(-1, -2),
        new Vector2Int(-2, -1),
        new Vector2Int(-2, 1),
        new Vector2Int(-1, 2)
    };

    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        foreach (Vector2Int offset in Offsets)
        {
            AddMoveIfValid(board, moves, BoardX + offset.x, BoardY + offset.y);
        }

        return moves;
    }
}

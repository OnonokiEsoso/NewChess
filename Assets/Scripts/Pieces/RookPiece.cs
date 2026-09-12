using System.Collections.Generic;
using UnityEngine;

public class RookPiece : ChessPiece
{
    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        AddRayMoves(board, moves, 1, 0);
        AddRayMoves(board, moves, -1, 0);
        AddRayMoves(board, moves, 0, 1);
        AddRayMoves(board, moves, 0, -1);
        return moves;
    }
}

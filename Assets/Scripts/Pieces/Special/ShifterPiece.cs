using System.Collections.Generic;
using UnityEngine;

public class ShifterPiece : ChessPiece
{
    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        AddShifterMove(board, moves, BoardX, BoardY + ForwardDirection);
        AddShifterMove(board, moves, BoardX + 2, BoardY - ForwardDirection);
        AddShifterMove(board, moves, BoardX - 2, BoardY - ForwardDirection);
        return moves;
    }

    public override bool CanSwapWith(ChessPiece friendlyPiece)
    {
        return friendlyPiece != null &&
               friendlyPiece.Side == Side &&
               !(friendlyPiece is KingPiece);
    }

    private void AddShifterMove(
        ChessBoard board,
        List<Vector2Int> moves,
        int x,
        int y)
    {
        if (!board.IsInsideBoard(x, y) ||
            !board.IsMoveDestinationAllowed(this, x, y))
        {
            return;
        }

        ChessPiece target = board.GetPieceAt(x, y);
        if (target == null ||
            target.Side != Side ||
            CanSwapWith(target))
        {
            moves.Add(new Vector2Int(x, y));
        }
    }
}

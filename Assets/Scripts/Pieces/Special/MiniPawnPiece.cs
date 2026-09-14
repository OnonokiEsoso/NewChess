using System.Collections.Generic;
using UnityEngine;

public class MiniPawnPiece : ChessPiece
{
    public override bool SelfDestructsAfterCapture => true;

    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int y = BoardY + ForwardDirection;

        if (board.IsInsideBoard(BoardX, y) &&
            board.GetPieceAt(BoardX, y) == null &&
            board.IsMoveDestinationAllowed(this, BoardX, y))
        {
            moves.Add(new Vector2Int(BoardX, y));
        }

        AddCapture(board, moves, BoardX - 1, y);
        AddCapture(board, moves, BoardX + 1, y);
        return moves;
    }

    private void AddCapture(ChessBoard board, List<Vector2Int> moves, int x, int y)
    {
        if (!board.IsInsideBoard(x, y) ||
            !board.IsMoveDestinationAllowed(this, x, y))
        {
            return;
        }

        ChessPiece target = board.GetPieceAt(x, y);
        if (target != null && target.Side != Side)
        {
            moves.Add(new Vector2Int(x, y));
        }
    }
}

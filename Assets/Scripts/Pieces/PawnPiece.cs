using System.Collections.Generic;
using UnityEngine;

public class PawnPiece : ChessPiece
{
    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int direction = ForwardDirection;
        int oneStepY = BoardY + direction;

        if (board.IsInsideBoard(BoardX, oneStepY) && board.GetPieceAt(BoardX, oneStepY) == null)
        {
            moves.Add(new Vector2Int(BoardX, oneStepY));

            int twoStepY = BoardY + (2 * direction);
            if (!HasMoved && board.IsInsideBoard(BoardX, twoStepY) && board.GetPieceAt(BoardX, twoStepY) == null)
            {
                moves.Add(new Vector2Int(BoardX, twoStepY));
            }
        }

        AddCaptureIfEnemy(board, moves, BoardX - 1, oneStepY);
        AddCaptureIfEnemy(board, moves, BoardX + 1, oneStepY);

        return moves;
    }

    private void AddCaptureIfEnemy(ChessBoard board, List<Vector2Int> moves, int x, int y)
    {
        if (!board.IsInsideBoard(x, y))
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

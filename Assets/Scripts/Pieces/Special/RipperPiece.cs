using System.Collections.Generic;
using UnityEngine;

public class RipperPiece : ChessPiece
{
    [SerializeField] private int killCount;
    public int KillCount => killCount;

    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int straightRange = killCount >= 2 ? board.BoardWidth + board.BoardHeight : 2;
        AddLimitedRay(board, moves, 1, 0, straightRange);
        AddLimitedRay(board, moves, -1, 0, straightRange);
        AddLimitedRay(board, moves, 0, 1, straightRange);
        AddLimitedRay(board, moves, 0, -1, straightRange);

        if (killCount >= 1)
        {
            int diagonalRange = killCount >= 3
                ? board.BoardWidth + board.BoardHeight
                : 2;
            AddLimitedRay(board, moves, 1, 1, diagonalRange);
            AddLimitedRay(board, moves, 1, -1, diagonalRange);
            AddLimitedRay(board, moves, -1, 1, diagonalRange);
            AddLimitedRay(board, moves, -1, -1, diagonalRange);
        }

        return moves;
    }

    public override void OnCapturedPiece(ChessPiece capturedPiece)
    {
        if (capturedPiece != null && capturedPiece.Side != Side)
        {
            killCount++;
        }
    }

    public override void OnBattleStarted(ChessBoard board)
    {
        killCount = 0;
    }

    private void AddLimitedRay(
        ChessBoard board,
        List<Vector2Int> moves,
        int dx,
        int dy,
        int range)
    {
        for (int distance = 1; distance <= range; distance++)
        {
            int x = BoardX + dx * distance;
            int y = BoardY + dy * distance;
            if (!board.IsInsideBoard(x, y))
            {
                break;
            }

            ChessPiece target = board.GetPieceAt(x, y);
            if (target == null)
            {
                if (board.IsMoveDestinationAllowed(this, x, y))
                {
                    moves.Add(new Vector2Int(x, y));
                }
                continue;
            }

            if (target.Side != Side &&
                board.IsMoveDestinationAllowed(this, x, y))
            {
                moves.Add(new Vector2Int(x, y));
            }

            break;
        }
    }
}

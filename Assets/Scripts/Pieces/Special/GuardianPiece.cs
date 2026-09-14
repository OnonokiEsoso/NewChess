using System.Collections.Generic;
using UnityEngine;

public class GuardianPiece : ChessPiece
{
    [SerializeField] private ChessPiece graceTarget;

    public Vector2Int FrontSquare =>
        new Vector2Int(BoardX, BoardY + ForwardDirection);

    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        AddMoveIfValid(board, moves, BoardX - 1, BoardY);
        AddMoveIfValid(board, moves, BoardX + 1, BoardY);
        AddMoveIfValid(board, moves, BoardX - 1, BoardY + ForwardDirection);
        AddMoveIfValid(board, moves, BoardX + 1, BoardY + ForwardDirection);
        return moves;
    }

    public override void OnMoved()
    {
        graceTarget = null;
        ChessBoard board = FindFirstObjectByType<ChessBoard>();
        if (board == null)
        {
            return;
        }

        Vector2Int front = FrontSquare;
        ChessPiece target = board.GetPieceAt(front.x, front.y);
        if (target != null && target.Side != Side)
        {
            graceTarget = target;
        }
    }

    public override void OnGlobalTurnAdvanced(
        ChessBoard board,
        PieceSide sideWhoseTurnStarted)
    {
        if (sideWhoseTurnStarted != Side || graceTarget == null)
        {
            return;
        }

        Vector2Int front = FrontSquare;
        if (graceTarget.BoardX == front.x &&
            graceTarget.BoardY == front.y &&
            !(graceTarget is KingPiece))
        {
            board.RemovePieceByAbility(graceTarget);
        }

        graceTarget = null;
    }
}

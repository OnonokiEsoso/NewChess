using System.Collections.Generic;
using UnityEngine;

public enum ClockPhase
{
    Forward,
    Right,
    Backward,
    Left
}

public class ClockPiece : ChessPiece
{
    [SerializeField] private ClockPhase debugPhase;
    public ClockPhase CurrentPhase => debugPhase;

    public override void OnBattleStarted(ChessBoard board)
    {
        debugPhase = ClockPhase.Forward;
    }

    public override void OnGlobalTurnAdvanced(
        ChessBoard board,
        PieceSide sideWhoseTurnStarted)
    {
        debugPhase = (ClockPhase)((board.GlobalTurnIndex - 1) % 4);
    }

    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        debugPhase = (ClockPhase)((board.GlobalTurnIndex - 1) % 4);
        List<Vector2Int> moves = new List<Vector2Int>();

        Vector2Int forward = Side == PieceSide.Player
            ? Vector2Int.up
            : Vector2Int.down;
        Vector2Int right = Vector2Int.right;
        Vector2Int depthDirection = forward;
        Vector2Int lateralDirection = right;

        if (debugPhase == ClockPhase.Right)
        {
            depthDirection = right;
            lateralDirection = -forward;
        }
        else if (debugPhase == ClockPhase.Backward)
        {
            depthDirection = -forward;
            lateralDirection = -right;
        }
        else if (debugPhase == ClockPhase.Left)
        {
            depthDirection = -right;
            lateralDirection = forward;
        }

        for (int depth = 1; depth <= 3; depth++)
        {
            for (int lateral = -1; lateral <= 1; lateral++)
            {
                Vector2Int target =
                    new Vector2Int(BoardX, BoardY) +
                    depthDirection * depth +
                    lateralDirection * lateral;

                if (!board.IsInsideBoard(target.x, target.y) ||
                    !board.IsPathClear(BoardX, BoardY, target.x, target.y) ||
                    !board.IsMoveDestinationAllowed(this, target.x, target.y))
                {
                    continue;
                }

                ChessPiece occupant = board.GetPieceAt(target.x, target.y);
                if (occupant == null || occupant.Side != Side)
                {
                    moves.Add(target);
                }
            }
        }

        return moves;
    }
}

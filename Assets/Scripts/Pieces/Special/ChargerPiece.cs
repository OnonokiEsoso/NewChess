using System.Collections.Generic;
using UnityEngine;

public class ChargerPiece : ChessPiece
{
    [SerializeField] private int chargeTurns;
    private bool movedSinceLastTurnAdvance;

    public int ChargeTurns => chargeTurns;

    public override List<Vector2Int> GetLegalMoves(ChessBoard board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        if (chargeTurns <= 2)
        {
            AddMoveIfValid(board, moves, BoardX + 1, BoardY);
            AddMoveIfValid(board, moves, BoardX - 1, BoardY);
            AddMoveIfValid(board, moves, BoardX, BoardY + 1);
            AddMoveIfValid(board, moves, BoardX, BoardY - 1);
        }
        else
        {
            AddRayMoves(board, moves, 1, 0);
            AddRayMoves(board, moves, -1, 0);
            AddRayMoves(board, moves, 0, 1);
            AddRayMoves(board, moves, 0, -1);

            if (chargeTurns >= 6)
            {
                AddRayMoves(board, moves, 1, 1);
                AddRayMoves(board, moves, 1, -1);
                AddRayMoves(board, moves, -1, 1);
                AddRayMoves(board, moves, -1, -1);
            }
        }

        if (chargeTurns >= 9)
        {
            for (int y = 0; y < board.BoardHeight; y++)
            {
                for (int x = 0; x < board.BoardWidth; x++)
                {
                    Vector2Int target = new Vector2Int(x, y);
                    if (board.GetPieceAt(x, y) == null &&
                        !board.IsSquareRestrictedByGuardian(x, y, this) &&
                        !board.IsSquareInEnemyDangerZone(x, y, Side) &&
                        !moves.Contains(target))
                    {
                        moves.Add(target);
                    }
                }
            }
        }

        return moves;
    }

    public override void OnMoved()
    {
        chargeTurns = 0;
        movedSinceLastTurnAdvance = true;
    }

    public override void OnBattleStarted(ChessBoard board)
    {
        chargeTurns = 0;
        movedSinceLastTurnAdvance = false;
    }

    public override void OnGlobalTurnAdvanced(
        ChessBoard board,
        PieceSide sideWhoseTurnStarted)
    {
        if (movedSinceLastTurnAdvance)
        {
            movedSinceLastTurnAdvance = false;
        }
        else
        {
            chargeTurns++;
        }
    }
}

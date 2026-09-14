using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PreparationLoadoutEntry
{
    [SerializeField] private PieceDefinition definition;
    [SerializeField] private Vector2Int boardPosition;
    [SerializeField] private PieceSide side;
    [SerializeField] private int cost;

    public PieceDefinition Definition => definition;
    public Vector2Int BoardPosition => boardPosition;
    public PieceSide Side => side;
    public int Cost => cost;

    public PreparationLoadoutEntry(
        PieceDefinition definition,
        Vector2Int boardPosition,
        PieceSide side,
        int cost)
    {
        this.definition = definition;
        this.boardPosition = boardPosition;
        this.side = side;
        this.cost = cost;
    }
}

[Serializable]
public class PreparationLoadout
{
    [SerializeField] private List<PreparationLoadoutEntry> entries =
        new List<PreparationLoadoutEntry>();

    public IReadOnlyList<PreparationLoadoutEntry> Entries => entries;
    public bool HasEntries => entries.Count > 0;

    public void ReplaceWith(IEnumerable<PreparationPiecePlacement> placements, PieceSide side)
    {
        entries.Clear();

        foreach (PreparationPiecePlacement placement in placements)
        {
            if (placement.Side == side &&
                placement.Definition != null &&
                placement.Piece != null)
            {
                entries.Add(new PreparationLoadoutEntry(
                    placement.Definition,
                    placement.BoardPosition,
                    placement.Side,
                    placement.Cost
                ));
            }
        }
    }

    public void Clear()
    {
        entries.Clear();
    }

    public List<PreparationLoadoutEntry> CreateCopy()
    {
        return new List<PreparationLoadoutEntry>(entries);
    }
}

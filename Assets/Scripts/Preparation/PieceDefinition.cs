using UnityEngine;

[CreateAssetMenu(fileName = "PieceDefinition", menuName = "Chess/Preparation/Piece Definition")]
public class PieceDefinition : ScriptableObject
{
    [SerializeField] private string pieceId;
    [SerializeField] private string displayName;
    [Min(0)] [SerializeField] private int cost;
    [SerializeField] private Sprite uiSprite;
    [SerializeField] private ChessPiece whitePrefab;
    [SerializeField] private ChessPiece blackPrefab;
    [SerializeField] private bool isKing;

    public string PieceId => pieceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int Cost => cost;
    public Sprite UiSprite => uiSprite;
    public ChessPiece WhitePrefab => whitePrefab;
    public ChessPiece BlackPrefab => blackPrefab;
    public bool IsKing => isKing;

    public ChessPiece GetPrefab(PieceSide side)
    {
        return side == PieceSide.Player ? whitePrefab : blackPrefab;
    }
}

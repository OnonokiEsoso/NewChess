using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PieceEntryUI : MonoBehaviour
{
    [SerializeField] private Image pieceImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button button;

    private PieceDefinition definition;
    private Action<PieceDefinition> selectedCallback;

    public void Initialize(PieceDefinition pieceDefinition, Action<PieceDefinition> onSelected)
    {
        definition = pieceDefinition;
        selectedCallback = onSelected;

        if (pieceImage != null)
        {
            pieceImage.sprite = definition != null ? definition.UiSprite : null;
            pieceImage.enabled = pieceImage.sprite != null;
        }

        if (nameText != null)
        {
            nameText.text = definition != null ? definition.DisplayName : "Missing Piece";
        }

        if (costText != null)
        {
            costText.text = definition != null ? $"{definition.Cost} pt" : "-";
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            button.interactable = definition != null;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (definition != null)
        {
            selectedCallback?.Invoke(definition);
        }
    }
}

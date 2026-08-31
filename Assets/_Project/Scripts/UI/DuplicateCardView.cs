using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class DuplicateCardData
    {
        public int id;
        public string cardName;
        public string initials;
        public string rarity;
        public int count;
    }

    public class DuplicateCardView : MonoBehaviour
    {
        [Header("Card UI Elements")]
        [SerializeField] private TMP_Text countBadgeText;
        [SerializeField] private TMP_Text initialsText;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private RoundedRectGraphic cardBorderGraphic;
        [SerializeField] private Button publishButton;

        private DuplicateCardData currentData;
        private Action<DuplicateCardData> onPublishCallback;

        public void Setup(DuplicateCardData data, Action<DuplicateCardData> onPublish)
        {
            currentData = data;
            onPublishCallback = onPublish;

            if (countBadgeText != null) countBadgeText.text = $"×{data.count}";
            if (initialsText != null) initialsText.text = data.initials;
            if (cardNameText != null) cardNameText.text = data.cardName;

            Color rColor = GetRarityColor(data.rarity);
            if (rarityText != null)
            {
                rarityText.text = data.rarity.ToUpper();
                rarityText.color = rColor;
            }

            if (cardBorderGraphic != null)
            {
                cardBorderGraphic.BorderColor = rColor;
                cardBorderGraphic.BorderWidth = data.rarity == "Mítica" ? 2.5f : 1.5f;
            }

            if (publishButton != null)
            {
                publishButton.onClick.RemoveAllListeners();
                publishButton.onClick.AddListener(() => onPublishCallback?.Invoke(currentData));
            }
        }

        private Color GetRarityColor(string rarity)
        {
            switch (rarity)
            {
                case "Mítica":
                case "Mitica":
                    return new Color(0.910f, 0.659f, 0.125f);
                case "Rara":
                    return new Color(0.678f, 0.369f, 0.941f);
                case "Poco común":
                case "Poco comun":
                    return new Color(0.188f, 0.820f, 0.345f);
                case "Común":
                case "Comun":
                default:
                    return new Color(0.188f, 0.820f, 0.345f);
            }
        }
    }
}

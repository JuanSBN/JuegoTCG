using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class ActiveListingData
    {
        public int id;
        public string cardName;
        public string initials;
        public string rarity;
        public int price;
        public string listedAt;
    }

    public class ActiveListingCardView : MonoBehaviour
    {
        [Header("Mini Preview")]
        [SerializeField] private TMP_Text initialsText;
        [SerializeField] private RoundedRectGraphic miniBorderGraphic;

        [Header("Info")]
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text postedAtText;

        [Header("Actions")]
        [SerializeField] private Button editPriceButton;
        [SerializeField] private Button withdrawButton;

        private ActiveListingData currentData;
        private Action<ActiveListingData> onEditCallback;
        private Action<ActiveListingData> onWithdrawCallback;

        public void Setup(ActiveListingData data, Action<ActiveListingData> onEdit, Action<ActiveListingData> onWithdraw)
        {
            currentData = data;
            onEditCallback = onEdit;
            onWithdrawCallback = onWithdraw;

            if (initialsText != null) initialsText.text = data.initials;
            if (cardNameText != null) cardNameText.text = data.cardName;
            if (priceText != null) priceText.text = data.price.ToString();
            if (postedAtText != null) postedAtText.text = $"Publicado {data.listedAt}";

            Color rColor = GetRarityColor(data.rarity);
            if (rarityText != null)
            {
                rarityText.text = data.rarity.ToUpper();
                rarityText.color = rColor;
            }

            if (miniBorderGraphic != null)
            {
                miniBorderGraphic.BorderColor = rColor;
                miniBorderGraphic.BorderWidth = data.rarity == "Mítica" ? 2.5f : 1.5f;
            }

            if (editPriceButton != null)
            {
                editPriceButton.onClick.RemoveAllListeners();
                editPriceButton.onClick.AddListener(() => onEditCallback?.Invoke(currentData));
            }

            if (withdrawButton != null)
            {
                withdrawButton.onClick.RemoveAllListeners();
                withdrawButton.onClick.AddListener(() => onWithdrawCallback?.Invoke(currentData));
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

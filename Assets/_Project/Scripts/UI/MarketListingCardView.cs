using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class MarketListingData
    {
        public int id;
        public string cardName;
        public string initials;
        public string rarity; // "Común", "Poco común", "Rara", "Mítica"
        public string sellerName;
        public string sellerAvatar;
        public int price;
        public string postedAt;
    }

    public class MarketListingCardView : MonoBehaviour
    {
        [Header("Seller Row")]
        [SerializeField] private TMP_Text sellerAvatarText;
        [SerializeField] private TMP_Text sellerNameText;
        [SerializeField] private TMP_Text postedAtText;

        [Header("Card Info")]
        [SerializeField] private TMP_Text initialsText;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private RoundedRectGraphic cardBorderGraphic;

        [Header("Price & Buy")]
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button buyButton;

        private MarketListingData currentData;
        private Action<MarketListingData> onBuyCallback;

        public void Setup(MarketListingData data, Action<MarketListingData> onBuy)
        {
            currentData = data;
            onBuyCallback = onBuy;

            if (sellerAvatarText != null) sellerAvatarText.text = data.sellerAvatar;
            if (sellerNameText != null) sellerNameText.text = data.sellerName;
            if (postedAtText != null) postedAtText.text = data.postedAt;

            if (initialsText != null) initialsText.text = data.initials;
            if (cardNameText != null) cardNameText.text = data.cardName;
            if (priceText != null) priceText.text = data.price.ToString();

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

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => onBuyCallback?.Invoke(currentData));
            }
        }

        private Color GetRarityColor(string rarity)
        {
            switch (rarity)
            {
                case "Mítica":
                case "Mitica":
                    return new Color(0.910f, 0.659f, 0.125f); // Gold
                case "Rara":
                    return new Color(0.678f, 0.369f, 0.941f); // Purple
                case "Poco común":
                case "Poco comun":
                    return new Color(0.188f, 0.820f, 0.345f); // Green
                case "Común":
                case "Comun":
                default:
                    return new Color(0.188f, 0.820f, 0.345f); // Green for common in market or gray
            }
        }
    }
}

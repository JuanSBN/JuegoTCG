using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class VitrineData
    {
        public string userName;
        public string avatarText;
        public List<string> cardRarities = new List<string>();
        public int likesCount;
    }

    public class VitrineCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text userNameText;
        [SerializeField] private TMP_Text avatarText;
        [SerializeField] private TMP_Text likesText;
        [SerializeField] private Button cardButton;
        [SerializeField] private List<RoundedRectGraphic> miniCardBorders = new List<RoundedRectGraphic>();

        private VitrineData currentData;
        private Action<VitrineData> onClickCallback;

        public void Setup(VitrineData data, Action<VitrineData> onClick)
        {
            currentData = data;
            onClickCallback = onClick;

            if (userNameText != null) userNameText.text = data.userName;
            if (avatarText != null) avatarText.text = data.avatarText;
            if (likesText != null) likesText.text = data.likesCount.ToString();

            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => onClickCallback?.Invoke(currentData));
            }

            for (int i = 0; i < miniCardBorders.Count; i++)
            {
                if (i < data.cardRarities.Count)
                {
                    miniCardBorders[i].gameObject.SetActive(true);
                    miniCardBorders[i].BorderColor = GetRarityColor(data.cardRarities[i]);
                }
                else
                {
                    miniCardBorders[i].gameObject.SetActive(false);
                }
            }
        }

        private Color GetRarityColor(string rarity)
        {
            switch (rarity)
            {
                case "Mítica":
                case "Mitica":
                    return new Color(0.910f, 0.659f, 0.125f); // Gold #e8a820
                case "Rara":
                    return new Color(0.678f, 0.369f, 0.941f); // Purple #ad5ef0
                case "Poco común":
                case "Poco comun":
                    return new Color(0.188f, 0.820f, 0.345f); // Green #30d158
                case "Común":
                case "Comun":
                default:
                    return new Color(0.6f, 0.6f, 0.6f, 0.5f); // Gray
            }
        }
    }
}

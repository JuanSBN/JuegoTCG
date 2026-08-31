using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class FriendData
    {
        public int id;
        public string userName;
        public string avatar;
        public int level;
        public int cardsCount;
        public int albumPct;
        public int power;
    }

    public class FriendCardView : MonoBehaviour
    {
        [Header("Header Info")]
        [SerializeField] private TMP_Text avatarText;
        [SerializeField] private TMP_Text userNameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text powerText;

        [Header("Stats & Album")]
        [SerializeField] private TMP_Text cardsCountText;
        [SerializeField] private TMP_Text albumPctText;
        [SerializeField] private RectTransform albumProgressBarFill;

        [Header("Action Buttons")]
        [SerializeField] private Button compareButton;
        [SerializeField] private Button tradeButton;

        private FriendData currentData;
        private Action<FriendData> onCompareCallback;
        private Action<FriendData> onTradeCallback;

        public void Setup(FriendData data, Action<FriendData> onCompare, Action<FriendData> onTrade)
        {
            currentData = data;
            onCompareCallback = onCompare;
            onTradeCallback = onTrade;

            if (avatarText != null) avatarText.text = data.avatar;
            if (userNameText != null) userNameText.text = data.userName;
            if (levelText != null) levelText.text = $"Nivel {data.level}";
            if (powerText != null) powerText.text = data.power.ToString();

            if (cardsCountText != null) cardsCountText.text = $"{data.cardsCount} cartas";
            if (albumPctText != null) albumPctText.text = $"{data.albumPct}%";

            if (albumProgressBarFill != null)
            {
                albumProgressBarFill.anchorMin = new Vector2(0f, 0f);
                albumProgressBarFill.anchorMax = new Vector2(Mathf.Clamp01(data.albumPct / 100f), 1f);
                albumProgressBarFill.sizeDelta = Vector2.zero;
            }

            if (compareButton != null)
            {
                compareButton.onClick.RemoveAllListeners();
                compareButton.onClick.AddListener(() => onCompareCallback?.Invoke(currentData));
            }

            if (tradeButton != null)
            {
                tradeButton.onClick.RemoveAllListeners();
                tradeButton.onClick.AddListener(() => onTradeCallback?.Invoke(currentData));
            }
        }
    }
}

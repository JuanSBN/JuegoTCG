using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class TradeCardData
    {
        public string rarity; // "Mítica", "Rara", "Poco común", "Común"
        public string initials; // "MÍ", "RA", "PO", "CO"
    }

    [Serializable]
    public class TradeData
    {
        public int id;
        public string userName;
        public string avatarText;
        public string timeText;
        public bool isUnread;
        public List<TradeCardData> youGive = new List<TradeCardData>();
        public List<TradeCardData> youReceive = new List<TradeCardData>();
    }

    public class TradeOfferCardView : MonoBehaviour
    {
        [Header("User Info")]
        [SerializeField] private TMP_Text userNameText;
        [SerializeField] private TMP_Text avatarText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private GameObject unreadDot;

        [Header("Cards Containers")]
        [SerializeField] private Transform youGiveParent;
        [SerializeField] private Transform youReceiveParent;

        [Header("Buttons")]
        [SerializeField] private GameObject receivedActionsGroup;
        [SerializeField] private GameObject sentActionsGroup;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;
        [SerializeField] private Button cancelButton;

        private TradeData currentData;
        private Action<TradeData> onAcceptCallback;
        private Action<TradeData> onRejectCallback;
        private Action<TradeData> onCancelCallback;

        public void Setup(TradeData data, bool isReceivedMode, Action<TradeData> onAccept, Action<TradeData> onReject, Action<TradeData> onCancel)
        {
            currentData = data;
            onAcceptCallback = onAccept;
            onRejectCallback = onReject;
            onCancelCallback = onCancel;

            if (userNameText != null) userNameText.text = data.userName;
            if (avatarText != null) avatarText.text = data.avatarText;
            if (timeText != null) timeText.text = data.timeText;
            if (unreadDot != null) unreadDot.SetActive(data.isUnread && isReceivedMode);

            if (receivedActionsGroup != null) receivedActionsGroup.SetActive(isReceivedMode);
            if (sentActionsGroup != null) sentActionsGroup.SetActive(!isReceivedMode);

            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(() => onAcceptCallback?.Invoke(currentData));
            }

            if (rejectButton != null)
            {
                rejectButton.onClick.RemoveAllListeners();
                rejectButton.onClick.AddListener(() => onRejectCallback?.Invoke(currentData));
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(() => onCancelCallback?.Invoke(currentData));
            }

            PopulateCards(youGiveParent, data.youGive);
            PopulateCards(youReceiveParent, data.youReceive);
        }

        private void PopulateCards(Transform parent, List<TradeCardData> cards)
        {
            if (parent == null) return;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (i < cards.Count)
                {
                    child.gameObject.SetActive(true);
                    BindMiniCard(child, cards[i]);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void BindMiniCard(Transform cardTr, TradeCardData cardData)
        {
            TMP_Text iniTMP = cardTr.GetComponentInChildren<TMP_Text>();
            RoundedRectGraphic borderG = cardTr.GetComponent<RoundedRectGraphic>();

            Color rColor = GetRarityColor(cardData.rarity);
            if (iniTMP != null)
            {
                iniTMP.text = string.IsNullOrEmpty(cardData.initials) ? cardData.rarity.Substring(0, 2).ToUpper() : cardData.initials;
                iniTMP.color = cardData.rarity == "Mítica" ? new Color(1f, 1f, 1f, 0.25f) : rColor;
            }

            if (borderG != null)
            {
                borderG.BorderColor = rColor;
                borderG.BorderWidth = 1.8f;
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
                    return new Color(0.6f, 0.6f, 0.6f, 0.5f); // Gray
            }
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class FriendRequestData
    {
        public int id;
        public string userName;
        public string avatar;
    }

    public class FriendRequestCardView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text avatarText;
        [SerializeField] private TMP_Text userNameText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;

        private FriendRequestData currentData;
        private Action<FriendRequestData> onAcceptCallback;
        private Action<FriendRequestData> onRejectCallback;

        public void Setup(FriendRequestData data, Action<FriendRequestData> onAccept, Action<FriendRequestData> onReject)
        {
            currentData = data;
            onAcceptCallback = onAccept;
            onRejectCallback = onReject;

            if (avatarText != null) avatarText.text = data.avatar;
            if (userNameText != null) userNameText.text = data.userName;

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
        }
    }
}

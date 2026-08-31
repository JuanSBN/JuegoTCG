using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class RankingEntryData
    {
        public int position;
        public string userName;
        public string avatar;
        public int power;
        public bool isMe;
    }

    public class FriendRankingRowView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text positionText;
        [SerializeField] private TMP_Text avatarText;
        [SerializeField] private RoundedRectGraphic avatarBorderGraphic;
        [SerializeField] private TMP_Text userNameText;
        [SerializeField] private Image lightningIcon;
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private RoundedRectGraphic rowBorderGraphic;

        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);
        private static readonly Color GoldTint = new Color(0.910f, 0.659f, 0.125f, 0.08f);
        private static readonly Color CardBg = new Color(0.051f, 0.102f, 0.075f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.13f);
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextGray = new Color(1f, 1f, 1f, 0.60f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.35f);

        public void Setup(RankingEntryData data)
        {
            if (positionText != null)
            {
                positionText.text = data.position.ToString();
                if (data.position == 1) positionText.color = Gold;
                else if (data.position == 2) positionText.color = new Color(0.85f, 0.85f, 0.85f);
                else positionText.color = TextDim;
            }

            if (avatarText != null)
            {
                avatarText.text = data.avatar;
                avatarText.color = data.isMe ? Gold : TextGray;
            }

            if (avatarBorderGraphic != null)
            {
                avatarBorderGraphic.BorderColor = data.isMe ? Gold : BorderSubtle;
                avatarBorderGraphic.color = data.isMe ? new Color(0.910f, 0.659f, 0.125f, 0.15f) : new Color(1f, 1f, 1f, 0.07f);
            }

            if (userNameText != null)
            {
                userNameText.text = data.userName;
                userNameText.fontStyle = data.isMe ? FontStyles.Bold : FontStyles.Normal;
                userNameText.color = data.isMe ? TextWhite : TextGray;
            }

            if (lightningIcon != null)
            {
                lightningIcon.color = data.isMe ? Gold : new Color(1f, 1f, 1f, 0.30f);
            }

            if (powerText != null)
            {
                powerText.text = data.power.ToString();
                powerText.color = data.isMe ? Gold : TextGray;
            }

            if (rowBorderGraphic != null)
            {
                rowBorderGraphic.color = data.isMe ? GoldTint : CardBg;
                rowBorderGraphic.BorderColor = data.isMe ? Gold : BorderSubtle;
                rowBorderGraphic.BorderWidth = data.isMe ? 1.5f : 1.0f;
            }
        }
    }
}

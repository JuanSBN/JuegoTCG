using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.Cards
{
    [ExecuteAlways]
    public class CardDisplay : MonoBehaviour
    {
        [Header("Card Data")]
        [SerializeField] private CardData cardData;

        [Header("UI Components")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image playerArtImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text teamText;
        [SerializeField] private TMP_Text positionText;
        [SerializeField] private TMP_Text rarityText;

        [Header("Frame Sprites (Order: Comun, Especial, Epica, Legendaria, Mitica, FullArt)")]
        [SerializeField] private Sprite[] rarityFrames;

        public CardData CardData => cardData;

        private void Start()
        {
            if (cardData != null)
            {
                SetCard(cardData);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cardData != null)
            {
                SetCard(cardData);
            }
        }
#endif

        public void SetCard(CardData data)
        {
            if (data == null) return;
            cardData = data;

            if (nameText != null) nameText.text = data.playerName;
            if (teamText != null) teamText.text = data.teamName;
            if (positionText != null) positionText.text = data.position;
            if (rarityText != null) rarityText.text = data.rarity.ToString().ToUpper();

            // Set player photo if available
            if (playerArtImage != null)
            {
                if (data.defaultArt != null)
                {
                    playerArtImage.sprite = data.defaultArt;
                    playerArtImage.gameObject.SetActive(true);
                }
                else
                {
                    playerArtImage.gameObject.SetActive(false);
                }
            }

            // Set frame sprite by rarity enum index
            int index = (int)data.rarity;
            if (frameImage != null && rarityFrames != null && index >= 0 && index < rarityFrames.Length)
            {
                if (rarityFrames[index] != null)
                {
                    frameImage.sprite = rarityFrames[index];
                }
            }
        }
    }
}

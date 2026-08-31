using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class VitrineDetailCardData
    {
        public string initials;
        public string playerName;
        public string rarity;
    }

    public class VitrineDetailController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private TMP_Text avatarText;
        [SerializeField] private TMP_Text userNameText;
        [SerializeField] private TMP_Text cardCountText;
        [SerializeField] private Button closeButton;

        [Header("Floating Like Pill")]
        [SerializeField] private Button likeButton;
        [SerializeField] private TMP_Text likeCountText;
        [SerializeField] private Image likeIconImage;
        [SerializeField] private RoundedRectGraphic likePillGraphic;

        [Header("Detail Cards")]
        [SerializeField] private Transform cardsGridParent;

        [Header("Colors")]
        [SerializeField] private Color goldColor = new Color(0.910f, 0.659f, 0.125f);
        [SerializeField] private Color goldBorderColor = new Color(0.831f, 0.588f, 0.055f);

        private int currentLikes = 0;
        private bool isLiked = false;
        private VitrineData activeVitrine;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (likeButton != null) likeButton.onClick.AddListener(ToggleLike);
        }

        public void Show(VitrineData vitrine)
        {
            activeVitrine = vitrine;
            currentLikes = vitrine.likesCount;
            isLiked = false;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (detailRoot != null)
            {
                detailRoot.SetActive(true);
                detailRoot.transform.SetAsLastSibling();
            }

            if (avatarText != null) avatarText.text = vitrine.avatarText;
            if (userNameText != null) userNameText.text = vitrine.userName;
            if (cardCountText != null) cardCountText.text = $"Vitrina pública · {GetCardsForUser(vitrine.userName).Count} cartas";

            UpdateLikeDisplay();
            PopulateCards(vitrine.userName);

            Debug.Log($"<color=green>[Vitrina] Mostrando vitrina pública de {vitrine.userName}</color>");
        }

        public void Hide()
        {
            if (detailRoot != null) detailRoot.SetActive(false);
            else gameObject.SetActive(false);
        }

        public void ToggleLike()
        {
            isLiked = !isLiked;
            currentLikes += isLiked ? 1 : -1;
            UpdateLikeDisplay();
        }

        private void UpdateLikeDisplay()
        {
            if (likeCountText != null)
            {
                likeCountText.text = currentLikes.ToString();
                likeCountText.color = isLiked ? new Color(0.05f, 0.10f, 0.07f) : goldColor;
            }

            if (likeIconImage != null)
            {
                likeIconImage.color = isLiked ? new Color(0.05f, 0.10f, 0.07f) : goldColor;
            }

            if (likePillGraphic != null)
            {
                likePillGraphic.color = isLiked ? goldColor : new Color(0.04f, 0.09f, 0.06f, 0.90f);
                likePillGraphic.BorderColor = isLiked ? goldColor : goldBorderColor;
            }
        }

        private void PopulateCards(string userName)
        {
            List<VitrineDetailCardData> cards = GetCardsForUser(userName);
            if (cardsGridParent == null) return;

            // Update children cards
            for (int i = 0; i < cardsGridParent.childCount; i++)
            {
                Transform child = cardsGridParent.GetChild(i);
                if (i < cards.Count)
                {
                    child.gameObject.SetActive(true);
                    BindCardData(child, cards[i]);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void BindCardData(Transform cardTr, VitrineDetailCardData cardData)
        {
            TMP_Text iniTMP = cardTr.Find("InitialsText")?.GetComponent<TMP_Text>();
            TMP_Text rarityTMP = cardTr.Find("RarityText")?.GetComponent<TMP_Text>();
            TMP_Text nameTMP = cardTr.Find("PlayerNameText")?.GetComponent<TMP_Text>();
            RoundedRectGraphic borderG = cardTr.GetComponent<RoundedRectGraphic>();

            if (iniTMP != null) iniTMP.text = cardData.initials;
            if (nameTMP != null) nameTMP.text = cardData.playerName;
            if (rarityTMP != null)
            {
                rarityTMP.text = cardData.rarity.ToUpper();
                rarityTMP.color = GetRarityColor(cardData.rarity);
            }

            if (borderG != null)
            {
                borderG.BorderColor = GetRarityColor(cardData.rarity);
                if (cardData.rarity == "Mítica")
                {
                    borderG.BorderWidth = 2.5f;
                }
                else
                {
                    borderG.BorderWidth = 1.5f;
                }
            }
        }

        private List<VitrineDetailCardData> GetCardsForUser(string userName)
        {
            switch (userName)
            {
                case "ProPlayer_99":
                    return new List<VitrineDetailCardData>
                    {
                        new VitrineDetailCardData { initials = "EH",  playerName = "Haaland",      rarity = "Mítica" },
                        new VitrineDetailCardData { initials = "KM",  playerName = "Mbappé",       rarity = "Rara" },
                        new VitrineDetailCardData { initials = "KDB", playerName = "De Bruyne",    rarity = "Rara" },
                        new VitrineDetailCardData { initials = "MS",  playerName = "Salah",        rarity = "Poco común" },
                        new VitrineDetailCardData { initials = "PE",  playerName = "Pedri",        rarity = "Rara" },
                        new VitrineDetailCardData { initials = "RO",  playerName = "Rodri",        rarity = "Común" },
                    };
                case "CardMaster_X":
                    return new List<VitrineDetailCardData>
                    {
                        new VitrineDetailCardData { initials = "LD",  playerName = "Luis Díaz",    rarity = "Mítica" },
                        new VitrineDetailCardData { initials = "LY",  playerName = "Lamine Yamal", rarity = "Mítica" },
                        new VitrineDetailCardData { initials = "PE",  playerName = "Pedri",        rarity = "Rara" },
                        new VitrineDetailCardData { initials = "EH",  playerName = "Haaland",      rarity = "Rara" },
                        new VitrineDetailCardData { initials = "KDB", playerName = "De Bruyne",    rarity = "Poco común" },
                        new VitrineDetailCardData { initials = "JB",  playerName = "Bellingham",   rarity = "Común" },
                    };
                case "ElChampion":
                    return new List<VitrineDetailCardData>
                    {
                        new VitrineDetailCardData { initials = "VJ",  playerName = "Vinicius Jr.", rarity = "Mítica" },
                        new VitrineDetailCardData { initials = "PE",  playerName = "Pedri",        rarity = "Rara" },
                        new VitrineDetailCardData { initials = "EH",  playerName = "Haaland",      rarity = "Común" },
                        new VitrineDetailCardData { initials = "RO",  playerName = "Rodri",        rarity = "Poco común" },
                    };
                default:
                    return new List<VitrineDetailCardData>
                    {
                        new VitrineDetailCardData { initials = "VJ",  playerName = "Vinicius Jr.", rarity = "Rara" },
                        new VitrineDetailCardData { initials = "JB",  playerName = "Bellingham",   rarity = "Poco común" },
                        new VitrineDetailCardData { initials = "JM",  playerName = "Musiala",      rarity = "Común" },
                        new VitrineDetailCardData { initials = "VO",  playerName = "Osimhen",      rarity = "Poco común" },
                    };
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

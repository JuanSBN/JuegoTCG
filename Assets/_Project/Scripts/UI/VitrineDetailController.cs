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

        private void Start()
        {
            if (activeVitrine == null)
            {
                Show(new VitrineData { userName = "PROPLAYER_99", avatarText = "PP", likesCount = 234 });
            }
        }

        public void Show(VitrineData vitrine)
        {
            activeVitrine = vitrine;
            currentLikes = vitrine.likesCount;
            isLiked = false;

            gameObject.SetActive(true);

            if (detailRoot != null)
            {
                detailRoot.SetActive(true);
            }

            // Keep bottom nav bar in front if it exists
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                Transform bottomBar = parentCanvas.transform.Find("BottomNavigationBar");
                if (bottomBar != null)
                {
                    bottomBar.SetAsLastSibling();
                }
            }

            if (avatarText != null) avatarText.text = vitrine.avatarText;
            if (userNameText != null) userNameText.text = vitrine.userName.ToUpper();
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
            Transform starTr = cardTr.Find("StarIcon");
            RoundedRectGraphic borderG = cardTr.GetComponent<RoundedRectGraphic>();

            Color rarityCol = GetRarityColor(cardData.rarity);

            if (iniTMP != null)
            {
                iniTMP.text = cardData.initials;
                iniTMP.color = new Color(1f, 1f, 1f, 0.22f);
            }

            if (nameTMP != null)
            {
                nameTMP.text = cardData.playerName;
                nameTMP.color = Color.white;
            }

            if (rarityTMP != null)
            {
                rarityTMP.text = cardData.rarity.ToUpper();
                rarityTMP.color = rarityCol;
            }

            if (starTr != null)
            {
                starTr.gameObject.SetActive(cardData.rarity == "Mítica" || cardData.rarity == "Mitica");
            }

            if (borderG != null)
            {
                borderG.BorderColor = rarityCol;
                borderG.BorderWidth = (cardData.rarity == "Mítica" || cardData.rarity == "Mitica") ? 2.5f : 1.8f;
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
                    return new Color(0.706f, 0.784f, 0.765f); // Silver/Cyan #b4c8c3
                case "Común":
                case "Comun":
                default:
                    return new Color(0.153f, 0.788f, 0.416f); // Green #27c96a
            }
        }
    }
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class MarketScreenController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text coinsText;

        [Header("Main Mode Tabs")]
        [SerializeField] private Button tabBuyButton;
        [SerializeField] private Button tabSellButton;
        [SerializeField] private RoundedRectGraphic tabBuyGraphic;
        [SerializeField] private RoundedRectGraphic tabSellGraphic;
        [SerializeField] private TMP_Text tabBuyText;
        [SerializeField] private TMP_Text tabSellText;

        [Header("Containers")]
        [SerializeField] private GameObject rarityFiltersHolderGO;
        [SerializeField] private GameObject buyTabContainer;
        [SerializeField] private GameObject sellTabContainer;

        [Header("Rarity Filter Chips")]
        [SerializeField] private List<Button> rarityFilterButtons = new List<Button>();
        [SerializeField] private List<RoundedRectGraphic> rarityFilterGraphics = new List<RoundedRectGraphic>();
        [SerializeField] private List<TMP_Text> rarityFilterTexts = new List<TMP_Text>();

        [Header("Buy Mode Listings Grid")]
        [SerializeField] private List<MarketListingCardView> listingCardViews = new List<MarketListingCardView>();
        [SerializeField] private GameObject emptyStateGO;

        [Header("Sell Mode Lists")]
        [SerializeField] private List<DuplicateCardView> duplicateCardViews = new List<DuplicateCardView>();
        [SerializeField] private List<ActiveListingCardView> activeListingCardViews = new List<ActiveListingCardView>();

        [Header("Price Modal")]
        [SerializeField] private GameObject priceModalGO;
        [SerializeField] private TMP_InputField priceInputField;
        [SerializeField] private Button confirmPriceButton;
        [SerializeField] private Button cancelPriceButton;

        [Header("Bottom Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        [Header("Colors")]
        [SerializeField] private Color goldColor = new Color(0.910f, 0.659f, 0.125f);
        [SerializeField] private Color darkBgColor = new Color(0.051f, 0.102f, 0.075f);
        [SerializeField] private Color textGrayColor = new Color(1f, 1f, 1f, 0.60f);

        private bool isBuyTab = true;
        private string activeRarityFilter = "Todas";
        private int userCoins = 1240;

        private List<MarketListingData> allListings = new List<MarketListingData>();
        private List<DuplicateCardData> allDuplicates = new List<DuplicateCardData>();
        private List<ActiveListingData> allMyListings = new List<ActiveListingData>();

        private readonly string[] filterNames = { "Todas", "Común", "Poco común", "Rara", "Mítica" };

        private void Awake()
        {
            FindReferencesIfMissing();
            InitializeData();
        }

        private void Start()
        {
            FindReferencesIfMissing();
            BindEvents();
            UpdateCoins();
            UpdateView();
        }

        private void FindReferencesIfMissing()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (tabInicioButton == null && b.name.Contains("Inicio")) tabInicioButton = b;
                else if (tabCartasButton == null && b.name.Contains("cartas")) tabCartasButton = b;
                else if (tabTiendaButton == null && b.name.Contains("Tienda")) tabTiendaButton = b;
                else if (tabComunidadButton == null && b.name.Contains("Comunidad")) tabComunidadButton = b;
                else if (tabPerfilButton == null && b.name.Contains("Perfil")) tabPerfilButton = b;
            }
        }

        private void InitializeData()
        {
            allListings = new List<MarketListingData>
            {
                new MarketListingData { id = 1, cardName = "Musiala", initials = "JM", rarity = "Común", sellerName = "ProPlayer_99", sellerAvatar = "PP", price = 25, postedAt = "hace 5 min" },
                new MarketListingData { id = 2, cardName = "Rodri", initials = "RO", rarity = "Común", sellerName = "ElChampion", sellerAvatar = "EC", price = 30, postedAt = "hace 3 h" },
                new MarketListingData { id = 3, cardName = "Haaland", initials = "EH", rarity = "Común", sellerName = "GoldenShot_7", sellerAvatar = "GS", price = 45, postedAt = "hace 5 h" },
                new MarketListingData { id = 4, cardName = "Salah", initials = "MS", rarity = "Poco común", sellerName = "CardMaster_X", sellerAvatar = "CM", price = 70, postedAt = "hace 8 h" },
                new MarketListingData { id = 5, cardName = "Mbappé", initials = "KM", rarity = "Poco común", sellerName = "FutbolFan_22", sellerAvatar = "FF", price = 80, postedAt = "hace 12 min" },
                new MarketListingData { id = 6, cardName = "Pedri", initials = "PE", rarity = "Rara", sellerName = "FutbolFan_22", sellerAvatar = "FF", price = 180, postedAt = "hace 5 h" },
                new MarketListingData { id = 7, cardName = "Bellingham", initials = "JB", rarity = "Rara", sellerName = "MiAmigo_01", sellerAvatar = "MA", price = 195, postedAt = "hace 2 h" },
                new MarketListingData { id = 8, cardName = "Vinicius Jr.", initials = "VJ", rarity = "Rara", sellerName = "CardMaster_X", sellerAvatar = "CM", price = 220, postedAt = "hace 23 min" },
                new MarketListingData { id = 9, cardName = "Luis Díaz", initials = "LD", rarity = "Mítica", sellerName = "ProPlayer_99", sellerAvatar = "PP", price = 650, postedAt = "hace 1 h" },
                new MarketListingData { id = 10, cardName = "Lamine Yamal", initials = "LY", rarity = "Mítica", sellerName = "GoldenShot_7", sellerAvatar = "GS", price = 750, postedAt = "hace 4 h" }
            };

            allDuplicates = new List<DuplicateCardData>
            {
                new DuplicateCardData { id = 201, cardName = "Bellingham", initials = "JB", rarity = "Rara", count = 2 },
                new DuplicateCardData { id = 202, cardName = "Salah", initials = "MS", rarity = "Poco común", count = 2 },
                new DuplicateCardData { id = 203, cardName = "Musiala", initials = "JM", rarity = "Común", count = 3 },
                new DuplicateCardData { id = 204, cardName = "Osimhen", initials = "VO", rarity = "Poco común", count = 2 }
            };

            allMyListings = new List<ActiveListingData>
            {
                new ActiveListingData { id = 101, cardName = "De Bruyne", initials = "KDB", rarity = "Rara", price = 250, listedAt = "hace 2 h" },
                new ActiveListingData { id = 102, cardName = "Osimhen", initials = "VO", rarity = "Poco común", price = 65, listedAt = "hace 5 h" }
            };
        }

        private void BindEvents()
        {
            if (backButton != null) backButton.onClick.AddListener(OnClickBack);

            if (tabBuyButton != null) tabBuyButton.onClick.AddListener(() => SwitchMainTab(true));
            if (tabSellButton != null) tabSellButton.onClick.AddListener(() => SwitchMainTab(false));

            for (int i = 0; i < rarityFilterButtons.Count; i++)
            {
                int index = i;
                if (rarityFilterButtons[i] != null)
                {
                    rarityFilterButtons[i].onClick.AddListener(() => SelectRarityFilter(filterNames[index]));
                }
            }

            if (cancelPriceButton != null) cancelPriceButton.onClick.AddListener(() => priceModalGO.SetActive(false));

            if (tabInicioButton != null) tabInicioButton.onClick.AddListener(() => SceneManager.LoadScene("HomeScreenScene"));
            if (tabCartasButton != null) tabCartasButton.onClick.AddListener(() => SceneManager.LoadScene("MyCardsScene"));
            if (tabTiendaButton != null) tabTiendaButton.onClick.AddListener(() => SceneManager.LoadScene("StoreScene"));
            if (tabComunidadButton != null) tabComunidadButton.onClick.AddListener(() => SceneManager.LoadScene("CommunityScene"));
            if (tabPerfilButton != null) tabPerfilButton.onClick.AddListener(() => SceneManager.LoadScene("ProfileScene"));
        }

        public void SwitchMainTab(bool buy)
        {
            isBuyTab = buy;
            UpdateView();
        }

        public void SelectRarityFilter(string rarity)
        {
            activeRarityFilter = rarity;
            UpdateView();
        }

        private void UpdateCoins()
        {
            if (coinsText != null) coinsText.text = userCoins.ToString();
        }

        private void UpdateView()
        {
            // Main Mode Tabs
            if (tabBuyGraphic != null)
            {
                tabBuyGraphic.color = isBuyTab ? goldColor : Color.clear;
                tabBuyGraphic.BorderColor = isBuyTab ? goldColor : new Color(1f, 1f, 1f, 0.15f);
            }
            if (tabSellGraphic != null)
            {
                tabSellGraphic.color = !isBuyTab ? goldColor : Color.clear;
                tabSellGraphic.BorderColor = !isBuyTab ? goldColor : new Color(1f, 1f, 1f, 0.15f);
            }

            if (tabBuyText != null) tabBuyText.color = isBuyTab ? darkBgColor : textGrayColor;
            if (tabSellText != null) tabSellText.color = !isBuyTab ? darkBgColor : textGrayColor;

            if (rarityFiltersHolderGO != null) rarityFiltersHolderGO.SetActive(isBuyTab);
            if (buyTabContainer != null) buyTabContainer.SetActive(isBuyTab);
            if (sellTabContainer != null) sellTabContainer.SetActive(!isBuyTab);

            if (isBuyTab)
            {
                // Rarity filters highlight
                for (int i = 0; i < rarityFilterGraphics.Count; i++)
                {
                    bool isActiveFilter = (filterNames[i] == activeRarityFilter);
                    if (rarityFilterGraphics[i] != null)
                    {
                        rarityFilterGraphics[i].color = isActiveFilter ? goldColor : Color.clear;
                        rarityFilterGraphics[i].BorderColor = isActiveFilter ? goldColor : new Color(1f, 1f, 1f, 0.15f);
                    }
                    if (i < rarityFilterTexts.Count && rarityFilterTexts[i] != null)
                    {
                        rarityFilterTexts[i].color = isActiveFilter ? darkBgColor : textGrayColor;
                    }
                }

                // Filter items
                List<MarketListingData> filtered = new List<MarketListingData>();
                foreach (var item in allListings)
                {
                    if (activeRarityFilter == "Todas" || item.rarity == activeRarityFilter)
                    {
                        filtered.Add(item);
                    }
                }

                if (emptyStateGO != null) emptyStateGO.SetActive(filtered.Count == 0);

                for (int i = 0; i < listingCardViews.Count; i++)
                {
                    if (i < filtered.Count)
                    {
                        listingCardViews[i].gameObject.SetActive(true);
                        listingCardViews[i].Setup(filtered[i], OnBuyListing);
                    }
                    else
                    {
                        listingCardViews[i].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // Populate Duplicates
                for (int i = 0; i < duplicateCardViews.Count; i++)
                {
                    if (i < allDuplicates.Count)
                    {
                        duplicateCardViews[i].gameObject.SetActive(true);
                        duplicateCardViews[i].Setup(allDuplicates[i], OnPublishDuplicate);
                    }
                    else
                    {
                        duplicateCardViews[i].gameObject.SetActive(false);
                    }
                }

                // Populate Active Listings
                for (int i = 0; i < activeListingCardViews.Count; i++)
                {
                    if (i < allMyListings.Count)
                    {
                        activeListingCardViews[i].gameObject.SetActive(true);
                        activeListingCardViews[i].Setup(allMyListings[i], OnEditPrice, OnWithdrawListing);
                    }
                    else
                    {
                        activeListingCardViews[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnBuyListing(MarketListingData listing)
        {
            if (userCoins >= listing.price)
            {
                userCoins -= listing.price;
                allListings.Remove(listing);
                UpdateCoins();
                UpdateView();
                Debug.Log($"<color=green>[Mercado] ¡Comprada carta {listing.cardName} ({listing.rarity}) por {listing.price} monedas!</color>");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[Mercado] Monedas insuficientes ({userCoins}/{listing.price}) para comprar {listing.cardName}.</color>");
            }
        }

        private void OnPublishDuplicate(DuplicateCardData card)
        {
            Debug.Log($"<color=green>[Mercado] Publicando duplicado: {card.cardName} ({card.rarity})...</color>");
            allMyListings.Insert(0, new ActiveListingData
            {
                id = (int)DateTime.UtcNow.Ticks,
                cardName = card.cardName,
                initials = card.initials,
                rarity = card.rarity,
                price = 100,
                listedAt = "ahora mismo"
            });
            UpdateView();
        }

        private void OnEditPrice(ActiveListingData listing)
        {
            Debug.Log($"<color=yellow>[Mercado] Editando precio de {listing.cardName} (actual: {listing.price})...</color>");
        }

        private void OnWithdrawListing(ActiveListingData listing)
        {
            Debug.Log($"<color=red>[Mercado] Retirada del mercado la carta {listing.cardName}.</color>");
            allMyListings.Remove(listing);
            UpdateView();
        }

        public void OnClickBack()
        {
            Debug.Log("<color=green>[Mercado] Regresando a Comunidad...</color>");
            SceneManager.LoadScene("CommunityScene");
        }
    }
}

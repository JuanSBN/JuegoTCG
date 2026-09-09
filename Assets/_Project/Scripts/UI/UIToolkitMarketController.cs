using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Cards;
using JuegoTCG.Networking;
using JuegoTCG.Social;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla del Mercado (MarketScreen).
    /// Maneja el modo COMPRAR (con filtrado reactivo por rareza y compra P2P en tiempo real)
    /// y el modo MIS VENTAS (con duplicados para publicar, listados activos para editar precio o retirar).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitMarketController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Button backBtn;
        private Label coinsTextLabel;
        private int currentCoins = 0;

        // Mode tabs
        private Button tabBuy;
        private Button tabSell;
        private VisualElement rarityFiltersRow;
        private VisualElement marketCardsGrid;
        private VisualElement myListingsContainer;
        private VisualElement duplicatesGrid;
        private VisualElement activeListingsList;
        private bool isBuyMode = true;

        // Rarity filter buttons
        private Button filterTodas;
        private Button filterComun;
        private Button filterPocoComun;
        private Button filterRara;
        private Button filterMitica;
        private readonly List<Button> rarityPills = new List<Button>();
        private string currentRarityFilter = "Todas";

        // Empty state labels
        private Label emptyMarketLabel;
        private Label emptyDuplicatesLabel;
        private Label emptyActiveListingsLabel;

        // Dynamic elements
        private readonly List<VisualElement> dynamicMarketCards = new List<VisualElement>();
        private readonly List<VisualElement> dynamicDupCards = new List<VisualElement>();
        private readonly List<VisualElement> dynamicActiveCards = new List<VisualElement>();

        // Buy Feedback Modal
        private VisualElement feedbackModal;
        private Label feedbackModalTitle;
        private Label modalCardDesc;
        private Button btnCloseFeedback;

        // Price Publish / Edit Modal
        private VisualElement priceModal;
        private Label priceModalTitle;
        private Label priceModalCardName;
        private Label priceModalCardRarity;
        private TextField priceInputField;
        private Button btnConfirmPrice;
        private Button btnCancelPrice;
        private bool isPublishing = false;
        private string currentPublishCardId = "";
        private string currentEditingListingDocId = "";

        // Active Listings elements
        private VisualElement cardActive1;
        private VisualElement cardActive2;

        [Header("Marcos Oficiales de Rareza")]
        [SerializeField] private Sprite[] rarityFrames = new Sprite[6];
        private static readonly string[] FrameGuids = new string[]
        {
            "4edcac4ad7f822e4aa7b10b2dd755926", // Comun
            "586794b59d6595341aa4a2f2b59209ce", // Especial
            "ab60ad89df16072448c901abb76cbe3a", // Epica
            "2a89c6d7166430641b49f80a84ac2cd8", // Legendaria
            "8ab77af7592605c48b2e119ccdb7dcb3", // Mitica
            "ae059fc1520988141a79cb933243639f"  // Full Art
        };

        private readonly Dictionary<int, RenderTexture> holoFrameRTs = new Dictionary<int, RenderTexture>();
        private Material holoMaterial;
        private readonly List<CardData> loadedCardAssets = new List<CardData>();

        private Coroutine pollRoutine;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            PlayerCollectionManager.EnsureExists();
            MarketService.EnsureExists();
            EnsureRarityFrames();
            EnsureHoloMaterial();
            LoadCardAssets();

            BindStaticUI();
            SubscribeEvents();

            // Sincronizar monedas iniciales
            if (FirebaseAuthManager.Instance != null)
            {
                currentCoins = FirebaseAuthManager.Instance.Coins;
                UpdateCoinsDisplay();
            }

            // Primer refresco y lanzar polling
            RefreshAllTabs();
            pollRoutine = StartCoroutine(PollMarketRoutine());
        }

        private void OnDisable()
        {
            if (pollRoutine != null)
            {
                StopCoroutine(pollRoutine);
                pollRoutine = null;
            }

            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (MarketService.Instance != null)
            {
                MarketService.Instance.OnMarketUpdated += OnMarketDataChanged;
            }

            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.OnCoinsChanged += OnCoinsUpdated;
            }
        }

        private void UnsubscribeEvents()
        {
            if (MarketService.Instance != null)
            {
                MarketService.Instance.OnMarketUpdated -= OnMarketDataChanged;
            }

            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.OnCoinsChanged -= OnCoinsUpdated;
            }
        }

        private IEnumerator PollMarketRoutine()
        {
            // Consultar inmediatamente al abrir
            if (MarketService.Instance != null)
            {
                _ = MarketService.Instance.RefreshCloudMarketAsync();
            }

            while (true)
            {
                yield return new WaitForSeconds(4f);
                if (MarketService.Instance != null)
                {
                    _ = MarketService.Instance.RefreshCloudMarketAsync();
                }
            }
        }

        private void OnMarketDataChanged()
        {
            RefreshAllTabs();
        }

        private void OnCoinsUpdated(int newCoins)
        {
            currentCoins = newCoins;
            UpdateCoinsDisplay();
        }

        private void BindStaticUI()
        {
            // Back button
            backBtn = root.Q<Button>("BackBtn");
            if (backBtn != null)
            {
                backBtn.clickable = new Clickable(() => SceneManager.LoadScene("CommunitySceneUIToolkit"));
            }

            // Coins
            coinsTextLabel = root.Q<Label>("CoinsText");

            // Mode tabs
            tabBuy = root.Q<Button>("Tab_Buy");
            tabSell = root.Q<Button>("Tab_Sell");
            rarityFiltersRow = root.Q<VisualElement>("RarityFiltersScrollView");
            marketCardsGrid = root.Q<VisualElement>("MarketCardsGrid");
            var marketScroll = root.Q<ScrollView>("MarketScrollView");
            if (marketScroll != null)
            {
                JuegoTCG.Core.MobilePerformanceOptimizer.ApplyOptimalScrollSettings(marketScroll);
            }

            var rarityScroll = root.Q<ScrollView>("RarityFiltersScrollView");
            if (rarityScroll != null)
            {
                JuegoTCG.Core.MobilePerformanceOptimizer.ApplyOptimalScrollSettings(rarityScroll);
            }

            if (tabBuy != null) tabBuy.clickable = new Clickable(() => SwitchMode(true));
            if (tabSell != null) tabSell.clickable = new Clickable(() => SwitchMode(false));

            // Rarity filters
            filterTodas = root.Q<Button>("Filter_Todas");
            filterComun = root.Q<Button>("Filter_Comun");
            filterPocoComun = root.Q<Button>("Filter_PocoComun");
            filterRara = root.Q<Button>("Filter_Rara");
            filterMitica = root.Q<Button>("Filter_Mitica");

            rarityPills.Clear();
            if (filterTodas != null) rarityPills.Add(filterTodas);
            if (filterComun != null) rarityPills.Add(filterComun);
            if (filterPocoComun != null) rarityPills.Add(filterPocoComun);
            if (filterRara != null) rarityPills.Add(filterRara);
            if (filterMitica != null) rarityPills.Add(filterMitica);

            if (filterTodas != null) filterTodas.clickable = new Clickable(() => SetRarityFilter("Todas", filterTodas));
            if (filterComun != null) filterComun.clickable = new Clickable(() => SetRarityFilter("Común", filterComun));
            if (filterPocoComun != null) filterPocoComun.clickable = new Clickable(() => SetRarityFilter("Poco común", filterPocoComun));
            if (filterRara != null) filterRara.clickable = new Clickable(() => SetRarityFilter("Rara", filterRara));
            if (filterMitica != null) filterMitica.clickable = new Clickable(() => SetRarityFilter("Mítica", filterMitica));

            // Active Listings elements
            cardActive1 = root.Q<VisualElement>("Card_Active_1");
            cardActive2 = root.Q<VisualElement>("Card_Active_2");
            duplicatesGrid = root.Q<VisualElement>(className: "duplicates-grid");
            activeListingsList = root.Q<VisualElement>(className: "active-listings-list");

            // Feedback Modal
            feedbackModal = root.Q<VisualElement>("MarketFeedbackModal");
            if (feedbackModal != null)
            {
                feedbackModalTitle = feedbackModal.Q<Label>(className: "market-modal-title");
            }
            modalCardDesc = root.Q<Label>("ModalCardDesc");
            btnCloseFeedback = root.Q<Button>("Btn_CloseFeedback");
            if (btnCloseFeedback != null)
            {
                btnCloseFeedback.clickable = new Clickable(() => feedbackModal?.AddToClassList("modal-hidden"));
            }

            // Price Modal
            priceModal = root.Q<VisualElement>("PriceModal");
            priceModalTitle = root.Q<Label>("PriceModalTitle");
            priceModalCardName = root.Q<Label>("PriceModalCardName");
            priceModalCardRarity = root.Q<Label>("PriceModalCardRarity");
            priceInputField = root.Q<TextField>("PriceInput");
            btnConfirmPrice = root.Q<Button>("Btn_ConfirmPrice");
            btnCancelPrice = root.Q<Button>("Btn_CancelPrice");

            if (btnCancelPrice != null)
            {
                btnCancelPrice.clickable = new Clickable(() => priceModal?.AddToClassList("modal-hidden"));
            }
            if (btnConfirmPrice != null)
            {
                btnConfirmPrice.clickable = new Clickable(ConfirmPriceModal);
            }

            // Bottom Nav
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Comunidad);
        }

        private void SwitchMode(bool buy)
        {
            isBuyMode = buy;
            if (isBuyMode)
            {
                tabBuy?.AddToClassList("mode-tab-pill-active");
                tabSell?.RemoveFromClassList("mode-tab-pill-active");

                if (rarityFiltersRow != null) rarityFiltersRow.style.display = DisplayStyle.Flex;
                if (marketCardsGrid != null) marketCardsGrid.style.display = DisplayStyle.Flex;
                if (myListingsContainer != null) myListingsContainer.style.display = DisplayStyle.None;
            }
            else
            {
                tabSell?.AddToClassList("mode-tab-pill-active");
                tabBuy?.RemoveFromClassList("mode-tab-pill-active");

                if (rarityFiltersRow != null) rarityFiltersRow.style.display = DisplayStyle.None;
                if (marketCardsGrid != null) marketCardsGrid.style.display = DisplayStyle.None;
                if (myListingsContainer != null) myListingsContainer.style.display = DisplayStyle.Flex;
            }

            RefreshAllTabs();
        }

        private void SetRarityFilter(string rarity, Button activeBtn)
        {
            currentRarityFilter = rarity;
            foreach (var pill in rarityPills)
            {
                pill.RemoveFromClassList("rarity-pill-active");
            }
            if (activeBtn != null) activeBtn.AddToClassList("rarity-pill-active");

            RefreshBuyTab();
        }

        private void RefreshAllTabs()
        {
            if (isBuyMode)
            {
                RefreshBuyTab();
            }
            else
            {
                RefreshSellTab();
            }
        }

        private void RefreshBuyTab()
        {
            if (MarketService.Instance == null || root == null) return;

            foreach (var dyn in dynamicMarketCards)
            {
                dyn.RemoveFromHierarchy();
            }
            dynamicMarketCards.Clear();

            var allListings = MarketService.Instance.GetActiveListings();
            var filtered = new List<MarketListingData>();

            string targetFilter = currentRarityFilter.Trim().ToLower();
            foreach (var item in allListings)
            {
                if (targetFilter == "todas")
                {
                    filtered.Add(item);
                }
                else
                {
                    string itemRarity = (item.rarity ?? "").Trim().ToLower();
                    if (itemRarity == targetFilter ||
                       (targetFilter == "comun" && itemRarity == "común") ||
                       (targetFilter == "común" && itemRarity == "comun") ||
                       (targetFilter == "poco comun" && itemRarity == "poco común") ||
                       (targetFilter == "poco común" && itemRarity == "poco comun") ||
                       (targetFilter == "mitica" && itemRarity == "mítica") ||
                       (targetFilter == "mítica" && itemRarity == "mitica"))
                    {
                        filtered.Add(item);
                    }
                }
            }

            if (emptyMarketLabel == null)
            {
                emptyMarketLabel = CreateEmptyLabel("No hay cartas disponibles a la venta en este momento.");
                marketCardsGrid?.Add(emptyMarketLabel);
            }

            emptyMarketLabel.style.display = filtered.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            string myUid = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.UserId : "";

            // Actualizar slots 1 a 10
            for (int i = 1; i <= 10; i++)
            {
                var cardEl = root.Q<VisualElement>($"Card_Market_{i}");
                if (cardEl == null) continue;

                int index = i - 1;
                if (index < filtered.Count)
                {
                    var listing = filtered[index];
                    cardEl.style.display = DisplayStyle.Flex;

                    // Seller info
                    var sellerNameLbl = cardEl.Q<Label>(className: "seller-name");
                    if (sellerNameLbl != null) sellerNameLbl.text = listing.sellerDisplayName;

                    var avatarTextLbl = cardEl.Q<Label>(className: "seller-avatar-text");
                    if (avatarTextLbl != null)
                    {
                        string sName = listing.sellerDisplayName ?? "PL";
                        avatarTextLbl.text = sName.Length >= 2 ? sName.Substring(0, 2).ToUpper() : sName.ToUpper();
                    }

                    // Card body
                    var cardBody = cardEl.Q<VisualElement>(className: "market-card-body");
                    if (cardBody != null)
                    {
                        RenderCardVisual(cardBody, listing.cardId, listing.cardName, listing.initials, listing.rarity);
                    }

                    // Card border/glow class
                    cardEl.RemoveFromClassList("market-card-common");
                    cardEl.RemoveFromClassList("market-card-uncommon");
                    cardEl.RemoveFromClassList("market-card-rare");
                    cardEl.RemoveFromClassList("market-card-mythic");

                    string rLower = (listing.rarity ?? "").ToLower();
                    if (rLower.Contains("mític") || rLower.Contains("mitic") || rLower.Contains("full")) cardEl.AddToClassList("market-card-mythic");
                    else if (rLower.Contains("rar") || rLower.Contains("epic") || rLower.Contains("épic") || rLower.Contains("legen")) cardEl.AddToClassList("market-card-rare");
                    else if (rLower.Contains("poco") || rLower.Contains("especi")) cardEl.AddToClassList("market-card-uncommon");
                    else cardEl.AddToClassList("market-card-common");

                    // Price
                    var priceLbl = cardEl.Q<Label>(className: "price-text");
                    if (priceLbl != null) priceLbl.text = listing.pricePerCard.ToString();

                    // Buy button
                    var buyBtn = root.Q<Button>($"Btn_Buy_{i}");
                    var buyBtnText = buyBtn?.Q<Label>(className: "market-btn-buy-text");

                    bool isMine = listing.isMine || (!string.IsNullOrEmpty(myUid) && listing.sellerUid == myUid);
                    if (isMine)
                    {
                        if (buyBtnText != null) buyBtnText.text = "TU CARTA";
                        buyBtn?.SetEnabled(false);
                        if (buyBtn != null) buyBtn.style.opacity = 0.5f;
                    }
                    else
                    {
                        if (buyBtnText != null) buyBtnText.text = "COMPRAR";
                        buyBtn?.SetEnabled(true);
                        if (buyBtn != null) buyBtn.style.opacity = 1f;

                        string capturedListingId = listing.listingId;
                        string capturedCardName = listing.cardName;
                        int capturedPrice = listing.pricePerCard;
                        if (buyBtn != null)
                        {
                            buyBtn.clickable = new Clickable(() => BuyCard(capturedListingId, capturedCardName, capturedPrice));
                        }
                    }
                }
                else
                {
                    cardEl.style.display = DisplayStyle.None;
                }
            }

            // Renderizar listados más allá del slot 10
            for (int i = 10; i < filtered.Count; i++)
            {
                var extraCard = CreateMarketCard(filtered[i], myUid);
                dynamicMarketCards.Add(extraCard);
                marketCardsGrid?.Add(extraCard);
            }
        }

        private void RefreshSellTab()
        {
            if (MarketService.Instance == null || root == null) return;

            foreach (var dyn in dynamicDupCards)
            {
                dyn.RemoveFromHierarchy();
            }
            dynamicDupCards.Clear();

            foreach (var dyn in dynamicActiveCards)
            {
                dyn.RemoveFromHierarchy();
            }
            dynamicActiveCards.Clear();

            // 1. Duplicados para Publicar
            var duplicates = MarketService.Instance.GetMyDuplicateCards();

            if (emptyDuplicatesLabel == null)
            {
                emptyDuplicatesLabel = CreateEmptyLabel("No tienes cartas duplicadas en tu colección.\n¡Abre sobres en la Tienda para conseguir más!");
                duplicatesGrid?.Add(emptyDuplicatesLabel);
            }

            emptyDuplicatesLabel.style.display = duplicates.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            for (int i = 1; i <= 2; i++)
            {
                var dupCard = root.Q<VisualElement>($"Card_Dup_{i}");
                if (dupCard == null) continue;

                int index = i - 1;
                if (index < duplicates.Count)
                {
                    var dup = duplicates[index];
                    dupCard.style.display = DisplayStyle.Flex;

                    var badgeText = dupCard.Q<Label>(className: "duplicate-badge-text");
                    if (badgeText != null) badgeText.text = $"×{dup.totalOwned}";

                    // Card border/glow class
                    dupCard.RemoveFromClassList("duplicate-card-common");
                    dupCard.RemoveFromClassList("duplicate-card-uncommon");
                    dupCard.RemoveFromClassList("duplicate-card-rare");
                    dupCard.RemoveFromClassList("duplicate-card-mythic");

                    string rLower = (dup.rarity ?? "").ToLower();
                    if (rLower.Contains("mític") || rLower.Contains("mitic") || rLower.Contains("full")) dupCard.AddToClassList("duplicate-card-mythic");
                    else if (rLower.Contains("rar") || rLower.Contains("epic") || rLower.Contains("épic") || rLower.Contains("legen")) dupCard.AddToClassList("duplicate-card-rare");
                    else if (rLower.Contains("poco") || rLower.Contains("especi")) dupCard.AddToClassList("duplicate-card-uncommon");
                    else dupCard.AddToClassList("duplicate-card-common");

                    var cardBody = dupCard.Q<VisualElement>(className: "duplicate-card-body");
                    if (cardBody != null)
                    {
                        RenderCardVisual(cardBody, dup.cardId, dup.cardName, "", dup.rarity);
                    }

                    var publishBtn = root.Q<Button>($"Btn_Publish_{i}");
                    if (publishBtn != null)
                    {
                        string capturedId = dup.cardId;
                        string capturedName = dup.cardName;
                        string capturedRarity = dup.rarity;
                        int capturedPrice = dup.defaultPrice;
                        publishBtn.clickable = new Clickable(() => OpenPublishModal(capturedId, capturedName, capturedRarity, capturedPrice));
                    }
                }
                else
                {
                    dupCard.style.display = DisplayStyle.None;
                }
            }

            for (int i = 2; i < duplicates.Count; i++)
            {
                var extraDup = CreateDuplicateCard(duplicates[i]);
                dynamicDupCards.Add(extraDup);
                duplicatesGrid?.Add(extraDup);
            }

            // 2. Listados Activos
            var myListings = MarketService.Instance.GetMyActiveListings();

            if (emptyActiveListingsLabel == null)
            {
                emptyActiveListingsLabel = CreateEmptyLabel("No tienes cartas publicadas a la venta actualmente.");
                activeListingsList?.Add(emptyActiveListingsLabel);
            }

            emptyActiveListingsLabel.style.display = myListings.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            for (int i = 1; i <= 2; i++)
            {
                var activeCard = root.Q<VisualElement>($"Card_Active_{i}");
                if (activeCard == null) continue;

                int index = i - 1;
                if (index < myListings.Count)
                {
                    var listing = myListings[index];
                    activeCard.style.display = DisplayStyle.Flex;

                    var miniCard = activeCard.Q<VisualElement>(className: "active-mini-card");
                    var miniCardText = activeCard.Q<Label>(className: "active-mini-card-text");

                    if (miniCard != null)
                    {
                        miniCard.RemoveFromClassList("active-mini-card-common");
                        miniCard.RemoveFromClassList("active-mini-card-uncommon");
                        miniCard.RemoveFromClassList("active-mini-card-rare");
                        miniCard.RemoveFromClassList("active-mini-card-mythic");

                        string rLower = (listing.rarity ?? "").ToLower();
                        if (rLower.Contains("mític") || rLower.Contains("mitic") || rLower.Contains("full")) miniCard.AddToClassList("active-mini-card-mythic");
                        else if (rLower.Contains("rar") || rLower.Contains("epic") || rLower.Contains("épic") || rLower.Contains("legen")) miniCard.AddToClassList("active-mini-card-rare");
                        else if (rLower.Contains("poco") || rLower.Contains("especi")) miniCard.AddToClassList("active-mini-card-uncommon");
                        else miniCard.AddToClassList("active-mini-card-common");

                        CardData activeAsset = loadedCardAssets.Find(c => c != null && (!string.IsNullOrEmpty(listing.cardId) && c.cardId == listing.cardId || (c.playerName != null && c.playerName.Equals(listing.cardName, System.StringComparison.OrdinalIgnoreCase))));
                        Sprite defaultArt = activeAsset != null ? activeAsset.defaultArt : null;
                        Sprite resolvedArt = DataPackManager.GetCardArt(listing.cardId, defaultArt);
                        if (resolvedArt != null)
                        {
                            miniCard.style.backgroundImage = new StyleBackground(resolvedArt);
                            if (miniCardText != null) miniCardText.text = "";
                        }
                        else
                        {
                            miniCard.style.backgroundImage = null;
                            if (miniCardText != null)
                            {
                                string cName = listing.cardName ?? "TC";
                                miniCardText.text = cName.Length >= 2 ? cName.Substring(0, 2).ToUpper() : cName.ToUpper();
                            }
                        }
                    }

                    var nameLbl = activeCard.Q<Label>(className: "active-listing-name");
                    if (nameLbl != null) nameLbl.text = listing.cardName;

                    var rarityLbl = activeCard.Q<Label>(className: "active-listing-rarity");
                    if (rarityLbl != null) rarityLbl.text = (listing.rarity ?? "COMÚN").ToUpper();

                    var priceLbl = root.Q<Label>($"Price_Active_{i}");
                    if (priceLbl != null) priceLbl.text = listing.pricePerCard.ToString();

                    var editBtn = root.Q<Button>($"Btn_EditPrice_{i}");
                    var withdrawBtn = root.Q<Button>($"Btn_Withdraw_{i}");

                    string capturedDocId = listing.listingId;
                    string capturedName = listing.cardName;
                    int capturedPrice = listing.pricePerCard;

                    if (editBtn != null)
                    {
                        editBtn.clickable = new Clickable(() => OpenEditPriceModal(capturedDocId, capturedName, capturedPrice));
                    }

                    if (withdrawBtn != null)
                    {
                        withdrawBtn.clickable = new Clickable(() => WithdrawListing(capturedDocId, capturedName));
                    }
                }
                else
                {
                    activeCard.style.display = DisplayStyle.None;
                }
            }

            for (int i = 2; i < myListings.Count; i++)
            {
                var extraActive = CreateActiveListingCard(myListings[i]);
                dynamicActiveCards.Add(extraActive);
                activeListingsList?.Add(extraActive);
            }
        }

        private VisualElement CreateMarketCard(MarketListingData listing, string myUid)
        {
            var cardEl = new VisualElement();
            cardEl.AddToClassList("market-card");
            string rLower = (listing.rarity ?? "").ToLower();
            if (rLower.Contains("mític") || rLower.Contains("mitic") || rLower.Contains("full")) cardEl.AddToClassList("market-card-mythic");
            else if (rLower.Contains("rar") || rLower.Contains("epic") || rLower.Contains("épic") || rLower.Contains("legen")) cardEl.AddToClassList("market-card-rare");
            else if (rLower.Contains("poco") || rLower.Contains("especi")) cardEl.AddToClassList("market-card-uncommon");
            else cardEl.AddToClassList("market-card-common");

            // Seller
            var sellerRow = new VisualElement();
            sellerRow.AddToClassList("market-card-seller");
            var avatar = new VisualElement();
            avatar.AddToClassList("seller-avatar");
            string sName = listing.sellerDisplayName ?? "PL";
            var avatarText = new Label(sName.Length >= 2 ? sName.Substring(0, 2).ToUpper() : sName.ToUpper());
            avatarText.AddToClassList("seller-avatar-text");
            avatar.Add(avatarText);
            sellerRow.Add(avatar);

            var sellerNameLbl = new Label(listing.sellerDisplayName);
            sellerNameLbl.AddToClassList("seller-name");
            sellerRow.Add(sellerNameLbl);

            var timeLbl = new Label(listing.timeAgo ?? "Reciente");
            timeLbl.AddToClassList("seller-time");
            sellerRow.Add(timeLbl);
            cardEl.Add(sellerRow);

            // Body
            var body = new VisualElement();
            body.AddToClassList("market-card-body");
            RenderCardVisual(body, listing.cardId, listing.cardName, listing.initials, listing.rarity);
            cardEl.Add(body);

            // Footer
            var footer = new VisualElement();
            footer.AddToClassList("market-card-footer");

            var pricePill = new VisualElement();
            pricePill.AddToClassList("market-price-pill");
            var coinIcon = new VisualElement();
            coinIcon.AddToClassList("price-coin-icon");
            var priceLbl = new Label(listing.pricePerCard.ToString());
            priceLbl.AddToClassList("price-text");
            pricePill.Add(coinIcon);
            pricePill.Add(priceLbl);
            footer.Add(pricePill);

            var buyBtn = new Button();
            buyBtn.AddToClassList("market-btn-buy");
            var buyBtnText = new Label("COMPRAR");
            buyBtnText.AddToClassList("market-btn-buy-text");
            buyBtn.Add(buyBtnText);

            bool isMine = listing.isMine || (!string.IsNullOrEmpty(myUid) && listing.sellerUid == myUid);
            if (isMine)
            {
                buyBtnText.text = "TU CARTA";
                buyBtn.SetEnabled(false);
                buyBtn.style.opacity = 0.5f;
            }
            else
            {
                buyBtnText.text = "COMPRAR";
                buyBtn.SetEnabled(true);
                buyBtn.style.opacity = 1f;
                string capturedListingId = listing.listingId;
                string capturedCardName = listing.cardName;
                int capturedPrice = listing.pricePerCard;
                buyBtn.clickable = new Clickable(() => BuyCard(capturedListingId, capturedCardName, capturedPrice));
            }
            footer.Add(buyBtn);
            cardEl.Add(footer);

            return cardEl;
        }

        private VisualElement CreateDuplicateCard(DuplicateCardInfo dup)
        {
            var card = new VisualElement();
            card.AddToClassList("duplicate-card");
            string rLower = (dup.rarity ?? "").ToLower();
            if (rLower.Contains("mític") || rLower.Contains("mitic") || rLower.Contains("full")) card.AddToClassList("duplicate-card-mythic");
            else if (rLower.Contains("rar") || rLower.Contains("epic") || rLower.Contains("épic") || rLower.Contains("legen")) card.AddToClassList("duplicate-card-rare");
            else if (rLower.Contains("poco") || rLower.Contains("especi")) card.AddToClassList("duplicate-card-uncommon");
            else card.AddToClassList("duplicate-card-common");

            var badge = new VisualElement();
            badge.AddToClassList("duplicate-badge");
            var badgeText = new Label($"×{dup.totalOwned}");
            badgeText.AddToClassList("duplicate-badge-text");
            badge.Add(badgeText);
            card.Add(badge);

            var body = new VisualElement();
            body.AddToClassList("duplicate-card-body");
            RenderCardVisual(body, dup.cardId, dup.cardName, "", dup.rarity);
            card.Add(body);

            var footer = new VisualElement();
            footer.AddToClassList("duplicate-card-footer");

            var pubBtn = new Button();
            pubBtn.AddToClassList("btn-publish");
            var pubBtnText = new Label("PUBLICAR");
            pubBtnText.AddToClassList("btn-publish-text");
            pubBtn.Add(pubBtnText);
            string capturedId = dup.cardId;
            string capturedName = dup.cardName;
            string capturedRarity = dup.rarity;
            int capturedPrice = dup.defaultPrice;
            pubBtn.clickable = new Clickable(() => OpenPublishModal(capturedId, capturedName, capturedRarity, capturedPrice));
            footer.Add(pubBtn);
            card.Add(footer);

            return card;
        }

        private VisualElement CreateActiveListingCard(MarketListingData listing)
        {
            var card = new VisualElement();
            card.AddToClassList("active-listing-card");

            var top = new VisualElement();
            top.AddToClassList("active-listing-top");

            var miniCard = new VisualElement();
            miniCard.AddToClassList("active-mini-card");
            string rLower = (listing.rarity ?? "").ToLower();
            if (rLower.Contains("mític") || rLower.Contains("mitic") || rLower.Contains("full")) miniCard.AddToClassList("active-mini-card-mythic");
            else if (rLower.Contains("rar") || rLower.Contains("epic") || rLower.Contains("épic") || rLower.Contains("legen")) miniCard.AddToClassList("active-mini-card-rare");
            else if (rLower.Contains("poco") || rLower.Contains("especi")) miniCard.AddToClassList("active-mini-card-uncommon");
            else miniCard.AddToClassList("active-mini-card-common");

            CardData asset = loadedCardAssets.Find(c => c != null && (!string.IsNullOrEmpty(listing.cardId) && c.cardId == listing.cardId || (c.playerName != null && c.playerName.Equals(listing.cardName, System.StringComparison.OrdinalIgnoreCase))));
            Sprite defaultArt = asset != null ? asset.defaultArt : null;
            Sprite resolvedArt = DataPackManager.GetCardArt(listing.cardId, defaultArt);
            if (resolvedArt != null)
            {
                miniCard.style.backgroundImage = new StyleBackground(resolvedArt);
            }
            else
            {
                var miniCardText = new Label(listing.cardName?.Length >= 2 ? listing.cardName.Substring(0, 2).ToUpper() : "TC");
                miniCardText.AddToClassList("active-mini-card-text");
                miniCard.Add(miniCardText);
            }
            top.Add(miniCard);

            var info = new VisualElement();
            info.AddToClassList("active-listing-info");

            var nameLbl = new Label(listing.cardName);
            nameLbl.AddToClassList("active-listing-name");
            info.Add(nameLbl);

            var rarityLbl = new Label((listing.rarity ?? "COMÚN").ToUpper());
            rarityLbl.AddToClassList("active-listing-rarity");
            info.Add(rarityLbl);

            var pricePill = new VisualElement();
            pricePill.AddToClassList("active-price-pill");
            var coinIcon = new VisualElement();
            coinIcon.AddToClassList("price-coin-icon");
            var priceText = new Label(listing.pricePerCard.ToString());
            priceText.AddToClassList("price-text");
            pricePill.Add(coinIcon);
            pricePill.Add(priceText);
            info.Add(pricePill);

            var timeLbl = new Label("Publicado hace un momento");
            timeLbl.AddToClassList("active-listing-time");
            info.Add(timeLbl);

            top.Add(info);
            card.Add(top);

            var actions = new VisualElement();
            actions.AddToClassList("active-listing-actions");

            var editBtn = new Button();
            editBtn.AddToClassList("btn-edit-price");
            var editBtnText = new Label("EDITAR PRECIO");
            editBtnText.AddToClassList("btn-edit-price-text");
            editBtn.Add(editBtnText);
            string capturedDocId = listing.listingId;
            string capturedName = listing.cardName;
            int capturedPrice = listing.pricePerCard;
            editBtn.clickable = new Clickable(() => OpenEditPriceModal(capturedDocId, capturedName, capturedPrice));
            actions.Add(editBtn);

            var withdrawBtn = new Button();
            withdrawBtn.AddToClassList("btn-withdraw");
            var withdrawBtnText = new Label("RETIRAR");
            withdrawBtnText.AddToClassList("btn-withdraw-text");
            withdrawBtn.Add(withdrawBtnText);
            withdrawBtn.clickable = new Clickable(() => WithdrawListing(capturedDocId, capturedName));
            actions.Add(withdrawBtn);

            card.Add(actions);
            return card;
        }

        private Label CreateEmptyLabel(string text)
        {
            var lbl = new Label(text);
            lbl.style.fontSize = 14;
            lbl.style.color = new Color(0.65f, 0.7f, 0.8f, 1f);
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            lbl.style.marginTop = 24;
            lbl.style.marginBottom = 24;
            lbl.style.whiteSpace = WhiteSpace.Normal;
            lbl.style.alignSelf = Align.Center;
            return lbl;
        }

        private async void BuyCard(string listingId, string name, int price)
        {
            if (currentCoins < price)
            {
                ShowFeedbackModal("MONEDAS INSUFICIENTES", $"Necesitas {price} monedas para comprar a {name}, pero solo tienes {currentCoins}.");
                return;
            }

            if (MarketService.Instance == null) return;

            MarketOperationResult result = await MarketService.Instance.BuyListedCardAsync(listingId);
            if (result.success)
            {
                ShowFeedbackModal("¡COMPRA EXITOSA!", $"Has adquirido a {name} por {price} monedas.");
                RefreshAllTabs();
            }
            else
            {
                ShowFeedbackModal("NO DISPONIBLE", string.IsNullOrEmpty(result.message) ? "Esta carta ya no está disponible o ya fue adquirida por otro jugador." : result.message);
                RefreshAllTabs();
            }
        }

        private void OpenPublishModal(string cardId, string cardName, string rarity, int defaultPrice)
        {
            isPublishing = true;
            currentPublishCardId = cardId;
            currentEditingListingDocId = "";

            if (priceModalTitle != null) priceModalTitle.text = "FIJAR PRECIO";
            if (priceModalCardName != null) priceModalCardName.text = cardName;
            if (priceModalCardRarity != null) priceModalCardRarity.text = (rarity ?? "COMÚN").ToUpper();
            if (priceInputField != null) priceInputField.value = defaultPrice > 0 ? defaultPrice.ToString() : "50";
            priceModal?.RemoveFromClassList("modal-hidden");
        }

        private void OpenEditPriceModal(string listingDocId, string cardName, int currentPrice)
        {
            isPublishing = false;
            currentPublishCardId = "";
            currentEditingListingDocId = listingDocId;

            if (priceModalTitle != null) priceModalTitle.text = "EDITAR PRECIO";
            if (priceModalCardName != null) priceModalCardName.text = cardName;
            if (priceModalCardRarity != null) priceModalCardRarity.text = "VENTA ACTIVA";
            if (priceInputField != null) priceInputField.value = currentPrice.ToString();
            priceModal?.RemoveFromClassList("modal-hidden");
        }

        private async void ConfirmPriceModal()
        {
            if (priceInputField != null && int.TryParse(priceInputField.value, out int newPrice) && newPrice > 0)
            {
                priceModal?.AddToClassList("modal-hidden");

                if (MarketService.Instance == null) return;

                if (isPublishing)
                {
                    MarketOperationResult result = await MarketService.Instance.ListCardForSaleAsync(currentPublishCardId, newPrice, 1);
                    if (result.success)
                    {
                        ShowFeedbackModal("¡PUBLICACIÓN EXITOSA!", $"Tu carta fue puesta a la venta por {newPrice} monedas.");
                        RefreshAllTabs();
                    }
                    else
                    {
                        ShowFeedbackModal("ERROR AL PUBLICAR", string.IsNullOrEmpty(result.message) ? "No se pudo poner la carta a la venta. Verifica tus duplicados." : result.message);
                    }
                }
                else
                {
                    MarketOperationResult result = await MarketService.Instance.UpdateListingPriceAsync(currentEditingListingDocId, newPrice);
                    if (result.success)
                    {
                        ShowFeedbackModal("PRECIO ACTUALIZADO", $"El precio fue modificado a {newPrice} monedas.");
                        RefreshAllTabs();
                    }
                    else
                    {
                        ShowFeedbackModal("ERROR", string.IsNullOrEmpty(result.message) ? "No se pudo actualizar el precio del listado." : result.message);
                    }
                }
            }
        }

        private async void WithdrawListing(string listingDocId, string cardName)
        {
            if (MarketService.Instance == null) return;

            MarketOperationResult result = await MarketService.Instance.CancelListingAsync(listingDocId);
            if (result.success)
            {
                ShowFeedbackModal("LISTADO RETIRADO", $"La carta {cardName} fue retirada del mercado y devuelta a tu colección.");
                RefreshAllTabs();
            }
            else
            {
                ShowFeedbackModal("ERROR", string.IsNullOrEmpty(result.message) ? "No se pudo retirar el listado." : result.message);
            }
        }

        private void ShowFeedbackModal(string title, string message)
        {
            if (feedbackModalTitle != null) feedbackModalTitle.text = title;
            if (modalCardDesc != null) modalCardDesc.text = message;
            feedbackModal?.RemoveFromClassList("modal-hidden");
        }

        private void UpdateCoinsDisplay()
        {
            if (coinsTextLabel != null)
            {
                coinsTextLabel.text = currentCoins.ToString();
            }
        }

        #region Card Artwork & Holographic Frame Setup

        private void EnsureRarityFrames()
        {
            if (rarityFrames == null || rarityFrames.Length < 6 || rarityFrames[0] == null)
            {
                // 1. Cargar desde CardPrefab en Resources
                GameObject prefab = Resources.Load<GameObject>("CardPrefab");
                if (prefab != null)
                {
                    CardDisplay cd = prefab.GetComponent<CardDisplay>();
                    if (cd != null && cd.RarityFrames != null && cd.RarityFrames.Length >= 6 && cd.RarityFrames[0] != null)
                    {
                        rarityFrames = cd.RarityFrames;
                    }
                }

#if UNITY_EDITOR
                // 2. Fallback de Unity Editor vía AssetDatabase
                if (rarityFrames == null || rarityFrames.Length < 6 || rarityFrames[0] == null)
                {
                    rarityFrames = new Sprite[6];
                    for (int i = 0; i < 6; i++)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(FrameGuids[i]);
                        rarityFrames[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                }
#endif
            }
        }

        private void EnsureHoloMaterial()
        {
            if (holoMaterial == null)
            {
                GameObject prefab = Resources.Load<GameObject>("CardPrefab");
                if (prefab != null)
                {
                    CardDisplay cd = prefab.GetComponent<CardDisplay>();
                    if (cd != null && cd.HolographicMaterial != null)
                    {
                        holoMaterial = new Material(cd.HolographicMaterial);
                    }
                }

#if UNITY_EDITOR
                if (holoMaterial == null)
                {
                    Material baseMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/HolographicFoilMaterial.mat");
                    if (baseMat != null) holoMaterial = new Material(baseMat);
                }
#endif
                if (holoMaterial == null)
                {
                    Shader s = Shader.Find("Shader Graphs/HolographicFoilShader");
                    if (s != null) holoMaterial = new Material(s);
                }

                if (holoMaterial != null)
                {
                    holoMaterial.SetFloat("_HoloIntensity", 0.85f);
                    holoMaterial.SetFloat("_ShimmerSpeed", 1.6f);
                    holoMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    holoMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                }
            }
        }

        private RenderTexture GetHoloFrameRT(int rarityIndex)
        {
            if (!holoFrameRTs.TryGetValue(rarityIndex, out RenderTexture rt) || rt == null)
            {
                rt = new RenderTexture(720, 1080, 0, RenderTextureFormat.ARGB32);
                rt.name = $"HoloFrame_Market_RT_{rarityIndex}";
                rt.antiAliasing = 1;
                rt.wrapMode = TextureWrapMode.Clamp;
                rt.filterMode = FilterMode.Bilinear;
                rt.Create();

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = prev;

                holoFrameRTs[rarityIndex] = rt;
            }
            return rt;
        }

        private void Update()
        {
        }

        private void OnDestroy()
        {
            foreach (var rt in holoFrameRTs.Values)
            {
                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }
            }
            holoFrameRTs.Clear();

            if (holoMaterial != null)
            {
                Destroy(holoMaterial);
            }
        }

        private void LoadCardAssets()
        {
            loadedCardAssets.Clear();

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null && !loadedCardAssets.Exists(c => c.cardId == card.cardId))
                {
                    loadedCardAssets.Add(card);
                }
            }
#endif

            CardData[] resourcePilotCards = Resources.LoadAll<CardData>("PilotAlbum");
            if (resourcePilotCards != null)
            {
                foreach (var card in resourcePilotCards)
                {
                    if (card != null && !loadedCardAssets.Exists(c => c.cardId == card.cardId))
                    {
                        loadedCardAssets.Add(card);
                    }
                }
            }

            CardData[] resourceAlbumCards = Resources.LoadAll<CardData>("Albums");
            if (resourceAlbumCards != null)
            {
                foreach (var card in resourceAlbumCards)
                {
                    if (card != null && !loadedCardAssets.Exists(c => c.cardId == card.cardId))
                    {
                        loadedCardAssets.Add(card);
                    }
                }
            }
        }

        private Rarity ParseRarity(string rarityStr)
        {
            if (string.IsNullOrEmpty(rarityStr)) return Rarity.Comun;
            string lower = rarityStr.Trim().ToLower();
            if (lower.Contains("mitic") || lower.Contains("mític")) return Rarity.Mitica;
            if (lower.Contains("legend")) return Rarity.Legendaria;
            if (lower.Contains("epic") || lower.Contains("épic")) return Rarity.Epica;
            if (lower.Contains("especi") || lower.Contains("poco")) return Rarity.Especial;
            if (lower.Contains("full")) return Rarity.FullArt;
            return Rarity.Comun;
        }

        private void RenderCardVisual(VisualElement container, string cardId, string cardName, string initials, string rarityStr)
        {
            if (container == null) return;
            container.Clear();

            CardData asset = null;
            if (loadedCardAssets != null && loadedCardAssets.Count > 0)
            {
                asset = loadedCardAssets.Find(c => c != null && (!string.IsNullOrEmpty(cardId) && c.cardId == cardId));
                if (asset == null && !string.IsNullOrEmpty(cardName))
                {
                    asset = loadedCardAssets.Find(c => c != null && c.playerName != null && c.playerName.Equals(cardName, System.StringComparison.OrdinalIgnoreCase));
                }
            }

            Sprite defaultArt = asset != null ? asset.defaultArt : null;
            Sprite resolvedArt = DataPackManager.GetCardArt(cardId, defaultArt);

            Rarity cardRarity = asset != null ? asset.rarity : ParseRarity(rarityStr);
            string displayCardName = !string.IsNullOrEmpty(cardName) ? cardName : (asset != null ? asset.playerName : "Carta");
            string displayInitials = !string.IsNullOrEmpty(initials)
                ? initials
                : (displayCardName.Length >= 2 ? displayCardName.Substring(0, 2).ToUpper() : displayCardName.ToUpper());

            if (resolvedArt != null)
            {
                // 1. Contenedor de Arte
                var artContainer = new VisualElement();
                artContainer.AddToClassList("card-art-container");

                // 2. Foto del Jugador (scale-and-crop)
                var photo = new VisualElement();
                photo.AddToClassList("card-art-photo");
                photo.style.backgroundImage = new StyleBackground(resolvedArt);
                artContainer.Add(photo);

                // 3. Marco Oficial de Rareza (con Holográfico si es Epica/Legendaria/Mitica/FullArt)
                int rIndex = (int)cardRarity;
                bool isHolo = (cardRarity == Rarity.Epica || cardRarity == Rarity.Legendaria || cardRarity == Rarity.Mitica || cardRarity == Rarity.FullArt);

                var frameEl = new VisualElement();
                frameEl.AddToClassList("card-art-frame");

                EnsureRarityFrames();
                EnsureHoloMaterial();

                if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                {
                    frameEl.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                }
                artContainer.Add(frameEl);

                // 4. Nombre del Jugador en el Marco
                var nameLbl = new Label(displayCardName);
                nameLbl.AddToClassList("card-player-name-framed");
                artContainer.Add(nameLbl);

                // 5. Caja Footer del Marco Oficial
                var footerBox = new VisualElement();
                footerBox.AddToClassList("card-frame-footer-box");

                string teamName = asset != null && !string.IsNullOrEmpty(asset.teamName) ? asset.teamName : "FC Barca";
                var teamLbl = new Label(teamName);
                teamLbl.AddToClassList("card-frame-team-name");
                footerBox.Add(teamLbl);

                var subRow = new VisualElement();
                subRow.AddToClassList("card-frame-sub-row");

                string pos = asset != null && !string.IsNullOrEmpty(asset.position) ? asset.position : "Delantero";
                var posLbl = new Label(pos);
                posLbl.AddToClassList("card-frame-pos-text");
                subRow.Add(posLbl);

                var rarityLbl = new Label(cardRarity.ToString().ToUpper());
                rarityLbl.AddToClassList("card-frame-rarity-text");
                subRow.Add(rarityLbl);

                footerBox.Add(subRow);
                artContainer.Add(footerBox);

                container.Add(artContainer);
            }
            else
            {
                // ----------------------------------------------------
                // DISEÑO FALLBACK ALARGADO CON ESTILO TÁCTICO
                // ----------------------------------------------------
                var fallbackContainer = new VisualElement();
                fallbackContainer.AddToClassList("card-fallback-container");

                var avatarCircle = new VisualElement();
                avatarCircle.AddToClassList("card-avatar-circle");

                var initialsLbl = new Label(displayInitials);
                initialsLbl.AddToClassList("card-avatar-initials");
                avatarCircle.Add(initialsLbl);
                fallbackContainer.Add(avatarCircle);

                var nameLbl = new Label(displayCardName);
                nameLbl.AddToClassList("card-player-name");
                fallbackContainer.Add(nameLbl);

                string teamName = asset != null && !string.IsNullOrEmpty(asset.teamName) ? asset.teamName : "Equipo TCG";
                var teamLbl = new Label(teamName);
                teamLbl.AddToClassList("card-player-team");
                fallbackContainer.Add(teamLbl);

                var rarityBadge = new Label(cardRarity.ToString().ToUpper());
                rarityBadge.AddToClassList("card-rarity-badge");

                switch (cardRarity)
                {
                    case Rarity.Mitica:
                    case Rarity.FullArt:
                        rarityBadge.AddToClassList("card-rarity-badge-mythic");
                        break;
                    case Rarity.Legendaria:
                    case Rarity.Epica:
                        rarityBadge.AddToClassList("card-rarity-badge-rare");
                        break;
                    case Rarity.Especial:
                        rarityBadge.AddToClassList("card-rarity-badge-uncommon");
                        break;
                    default:
                        rarityBadge.AddToClassList("card-rarity-badge-common");
                        break;
                }

                fallbackContainer.Add(rarityBadge);
                container.Add(fallbackContainer);
            }
        }

        #endregion
    }
}

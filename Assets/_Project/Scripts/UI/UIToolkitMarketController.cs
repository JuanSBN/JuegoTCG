using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
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
        private bool isBuyMode = true;

        // Rarity filter buttons
        private Button filterTodas;
        private Button filterComun;
        private Button filterPocoComun;
        private Button filterRara;
        private Button filterMitica;
        private readonly List<Button> rarityPills = new List<Button>();
        private string currentRarityFilter = "Todas";

        // Buy Feedback Modal
        private VisualElement feedbackModal;
        private Label feedbackModalTitle;
        private Label modalCardDesc;
        private Button btnCloseFeedback;

        // Price Publish / Edit Modal
        private VisualElement priceModal;
        private Label priceModalTitle;
        private Label priceModalCardName;
        private TextField priceInputField;
        private Button btnConfirmPrice;
        private Button btnCancelPrice;
        private bool isPublishing = false;
        private string currentPublishCardId = "";
        private string currentEditingListingDocId = "";

        // Active Listings elements
        private VisualElement cardActive1;
        private VisualElement cardActive2;

        private Coroutine pollRoutine;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            MarketService.EnsureExists();

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
            myListingsContainer = root.Q<VisualElement>("MyListingsContainer");

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
                    var initialsLbl = cardEl.Q<Label>(className: "card-big-initials");
                    if (initialsLbl != null)
                    {
                        string cName = listing.cardName ?? "TC";
                        initialsLbl.text = cName.Length >= 2 ? cName.Substring(0, 2).ToUpper() : cName.ToUpper();
                    }

                    var nameLbl = cardEl.Q<Label>(className: "card-player-name");
                    if (nameLbl != null) nameLbl.text = listing.cardName;

                    var rarityBadge = cardEl.Q<Label>(className: "card-rarity-badge");
                    if (rarityBadge != null) rarityBadge.text = listing.rarity.ToUpper();

                    // Card border/glow class
                    cardEl.RemoveFromClassList("market-card-common");
                    cardEl.RemoveFromClassList("market-card-uncommon");
                    cardEl.RemoveFromClassList("market-card-rare");
                    cardEl.RemoveFromClassList("market-card-mythic");

                    string rLower = (listing.rarity ?? "").ToLower();
                    if (rLower.Contains("mític") || rLower.Contains("mitic")) cardEl.AddToClassList("market-card-mythic");
                    else if (rLower.Contains("rar")) cardEl.AddToClassList("market-card-rare");
                    else if (rLower.Contains("poco")) cardEl.AddToClassList("market-card-uncommon");
                    else cardEl.AddToClassList("market-card-common");

                    // Price
                    var priceLbl = cardEl.Q<Label>(className: "price-text");
                    if (priceLbl != null) priceLbl.text = listing.pricePerCard.ToString();

                    // Buy button
                    var buyBtn = root.Q<Button>($"Btn_Buy_{i}");
                    if (buyBtn != null)
                    {
                        string capturedListingId = listing.listingId;
                        string capturedCardName = listing.cardName;
                        int capturedPrice = listing.pricePerCard;
                        buyBtn.clickable = new Clickable(() => BuyCard(capturedListingId, capturedCardName, capturedPrice));
                    }
                }
                else
                {
                    cardEl.style.display = DisplayStyle.None;
                }
            }
        }

        private void RefreshSellTab()
        {
            if (MarketService.Instance == null || root == null) return;

            // 1. Duplicados para Publicar
            var duplicates = MarketService.Instance.GetMyDuplicateCards();
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

                    var initialsLbl = dupCard.Q<Label>(className: "card-big-initials");
                    if (initialsLbl != null)
                    {
                        string cName = dup.cardName ?? "TC";
                        initialsLbl.text = cName.Length >= 2 ? cName.Substring(0, 2).ToUpper() : cName.ToUpper();
                    }

                    var rarityBadge = dupCard.Q<Label>(className: "card-rarity-badge");
                    if (rarityBadge != null) rarityBadge.text = dup.rarity.ToUpper();

                    var nameLbl = dupCard.Q<Label>(className: "duplicate-player-name");
                    if (nameLbl != null) nameLbl.text = dup.cardName;

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

            // 2. Listados Activos
            var myListings = MarketService.Instance.GetMyActiveListings();
            for (int i = 1; i <= 2; i++)
            {
                var activeCard = root.Q<VisualElement>($"Card_Active_{i}");
                if (activeCard == null) continue;

                int index = i - 1;
                if (index < myListings.Count)
                {
                    var listing = myListings[index];
                    activeCard.style.display = DisplayStyle.Flex;

                    var miniCardText = activeCard.Q<Label>(className: "active-mini-card-text");
                    if (miniCardText != null)
                    {
                        string cName = listing.cardName ?? "TC";
                        miniCardText.text = cName.Length >= 2 ? cName.Substring(0, 2).ToUpper() : cName.ToUpper();
                    }

                    var nameLbl = activeCard.Q<Label>(className: "active-listing-name");
                    if (nameLbl != null) nameLbl.text = listing.cardName;

                    var rarityLbl = activeCard.Q<Label>(className: "active-listing-rarity");
                    if (rarityLbl != null) rarityLbl.text = listing.rarity.ToUpper();

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
            }
            else
            {
                ShowFeedbackModal("NO DISPONIBLE", string.IsNullOrEmpty(result.message) ? "Esta carta ya no está disponible o ya fue adquirida por otro jugador." : result.message);
            }
        }

        private void OpenPublishModal(string cardId, string cardName, string rarity, int defaultPrice)
        {
            isPublishing = true;
            currentPublishCardId = cardId;
            currentEditingListingDocId = "";

            if (priceModalTitle != null) priceModalTitle.text = "FIJAR PRECIO";
            if (priceModalCardName != null) priceModalCardName.text = $"{cardName} ({rarity.ToUpper()})";
            if (priceInputField != null) priceInputField.value = defaultPrice.ToString();
            priceModal?.RemoveFromClassList("modal-hidden");
        }

        private void OpenEditPriceModal(string listingDocId, string cardName, int currentPrice)
        {
            isPublishing = false;
            currentPublishCardId = "";
            currentEditingListingDocId = listingDocId;

            if (priceModalTitle != null) priceModalTitle.text = "EDITAR PRECIO";
            if (priceModalCardName != null) priceModalCardName.text = cardName;
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
    }
}

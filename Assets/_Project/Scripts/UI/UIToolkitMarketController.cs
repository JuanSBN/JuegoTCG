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
    /// Maneja el modo COMPRAR (con filtrado reactivo por rareza y compra) y el modo
    /// MIS VENTAS (con duplicados para publicar, listados activos para editar precio o retirar).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitMarketController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Button backBtn;
        private Label coinsTextLabel;
        private int currentCoins = 1240;

        // Mode tabs
        private Button tabBuy;
        private Button tabSell;
        private VisualElement rarityFiltersRow;
        private VisualElement marketCardsGrid;
        private VisualElement myListingsContainer;

        // Rarity filter buttons
        private Button filterTodas;
        private Button filterComun;
        private Button filterPocoComun;
        private Button filterRara;
        private Button filterMitica;
        private readonly List<Button> rarityPills = new List<Button>();

        // Buy Feedback Modal
        private VisualElement feedbackModal;
        private Label modalCardDesc;
        private Button btnCloseFeedback;

        // Price Publish / Edit Modal
        private VisualElement priceModal;
        private Label priceModalTitle;
        private Label priceModalCardName;
        private TextField priceInputField;
        private Button btnConfirmPrice;
        private Button btnCancelPrice;
        private int currentEditingListingId = -1;
        private string currentPublishCardId = "JM";

        // Active Listings elements
        private VisualElement cardActive1;
        private VisualElement cardActive2;
        private Label priceActive1;
        private Label priceActive2;

        private struct ListingItem
        {
            public int id;
            public VisualElement element;
            public string rarity;
            public int price;
            public string cardName;
        }

        private readonly List<ListingItem> listings = new List<ListingItem>();

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            BindUI();
        }

        private void BindUI()
        {
            // Back button
            backBtn = root.Q<Button>("BackBtn");
            if (backBtn != null)
            {
                backBtn.clicked += () => SceneManager.LoadScene("CommunitySceneUIToolkit");
            }

            // Coins
            coinsTextLabel = root.Q<Label>("CoinsText");
            if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.Coins > 0)
            {
                currentCoins = FirebaseAuthManager.Instance.Coins;
            }
            UpdateCoinsDisplay();

            // Mode tabs
            tabBuy = root.Q<Button>("Tab_Buy");
            tabSell = root.Q<Button>("Tab_Sell");
            rarityFiltersRow = root.Q<VisualElement>("RarityFiltersScrollView");
            marketCardsGrid = root.Q<VisualElement>("MarketCardsGrid");
            myListingsContainer = root.Q<VisualElement>("MyListingsContainer");

            if (tabBuy != null) tabBuy.clicked += () => SwitchMode(true);
            if (tabSell != null) tabSell.clicked += () => SwitchMode(false);

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

            if (filterTodas != null) filterTodas.clicked += () => FilterByRarity("Todas", filterTodas);
            if (filterComun != null) filterComun.clicked += () => FilterByRarity("Común", filterComun);
            if (filterPocoComun != null) filterPocoComun.clicked += () => FilterByRarity("Poco común", filterPocoComun);
            if (filterRara != null) filterRara.clicked += () => FilterByRarity("Rara", filterRara);
            if (filterMitica != null) filterMitica.clicked += () => FilterByRarity("Mítica", filterMitica);

            // Wire 10 cards in COMPRAR
            listings.Clear();
            RegisterListing(1, "Musiala", "Común", 25);
            RegisterListing(2, "Rodri", "Común", 30);
            RegisterListing(3, "Haaland", "Común", 45);
            RegisterListing(4, "Salah", "Poco común", 70);
            RegisterListing(5, "Mbappé", "Poco común", 80);
            RegisterListing(6, "Pedri", "Rara", 180);
            RegisterListing(7, "Bellingham", "Rara", 195);
            RegisterListing(8, "Vinicius Jr.", "Rara", 220);
            RegisterListing(9, "Luis Díaz", "Mítica", 650);
            RegisterListing(10, "Lamine Yamal", "Mítica", 750);

            // Wire Duplicates to Publish
            var btnPublish1 = root.Q<Button>("Btn_Publish_1");
            if (btnPublish1 != null)
            {
                btnPublish1.clicked += () => OpenPublishModal("JM", "Musiala", "COMÚN", 35);
            }

            var btnPublish2 = root.Q<Button>("Btn_Publish_2");
            if (btnPublish2 != null)
            {
                btnPublish2.clicked += () => OpenPublishModal("VO", "Osimhen", "POCO COMÚN", 75);
            }

            // Wire Active Listings
            cardActive1 = root.Q<VisualElement>("Card_Active_1");
            cardActive2 = root.Q<VisualElement>("Card_Active_2");
            priceActive1 = root.Q<Label>("Price_Active_1");
            priceActive2 = root.Q<Label>("Price_Active_2");

            var btnEditPrice1 = root.Q<Button>("Btn_EditPrice_1");
            var btnWithdraw1 = root.Q<Button>("Btn_Withdraw_1");
            if (btnEditPrice1 != null) btnEditPrice1.clicked += () => OpenEditPriceModal(1, "De Bruyne", 250);
            if (btnWithdraw1 != null) btnWithdraw1.clicked += () => WithdrawListing(cardActive1, "De Bruyne");

            var btnEditPrice2 = root.Q<Button>("Btn_EditPrice_2");
            var btnWithdraw2 = root.Q<Button>("Btn_Withdraw_2");
            if (btnEditPrice2 != null) btnEditPrice2.clicked += () => OpenEditPriceModal(2, "Osimhen", 65);
            if (btnWithdraw2 != null) btnWithdraw2.clicked += () => WithdrawListing(cardActive2, "Osimhen");

            // Feedback Modal
            feedbackModal = root.Q<VisualElement>("MarketFeedbackModal");
            modalCardDesc = root.Q<Label>("ModalCardDesc");
            btnCloseFeedback = root.Q<Button>("Btn_CloseFeedback");
            if (btnCloseFeedback != null)
            {
                btnCloseFeedback.clicked += () => feedbackModal.AddToClassList("modal-hidden");
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
                btnCancelPrice.clicked += () => priceModal.AddToClassList("modal-hidden");
            }
            if (btnConfirmPrice != null)
            {
                btnConfirmPrice.clicked += ConfirmPriceModal;
            }

            // Bottom Nav
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Comunidad);
        }

        private void RegisterListing(int id, string name, string rarity, int price)
        {
            var cardEl = root.Q<VisualElement>($"Card_Market_{id}");
            var buyBtn = root.Q<Button>($"Btn_Buy_{id}");

            if (cardEl != null)
            {
                listings.Add(new ListingItem { id = id, element = cardEl, cardName = name, rarity = rarity, price = price });
            }

            if (buyBtn != null && cardEl != null)
            {
                buyBtn.clicked += () => BuyCard(id, name, price, cardEl);
            }
        }

        private void SwitchMode(bool isBuy)
        {
            if (isBuy)
            {
                tabBuy.AddToClassList("mode-tab-pill-active");
                tabSell.RemoveFromClassList("mode-tab-pill-active");

                if (rarityFiltersRow != null) rarityFiltersRow.style.display = DisplayStyle.Flex;
                if (marketCardsGrid != null) marketCardsGrid.style.display = DisplayStyle.Flex;
                if (myListingsContainer != null) myListingsContainer.style.display = DisplayStyle.None;
            }
            else
            {
                tabSell.AddToClassList("mode-tab-pill-active");
                tabBuy.RemoveFromClassList("mode-tab-pill-active");

                if (rarityFiltersRow != null) rarityFiltersRow.style.display = DisplayStyle.None;
                if (marketCardsGrid != null) marketCardsGrid.style.display = DisplayStyle.None;
                if (myListingsContainer != null) myListingsContainer.style.display = DisplayStyle.Flex;
            }
        }

        private void FilterByRarity(string rarity, Button activeBtn)
        {
            foreach (var pill in rarityPills)
            {
                pill.RemoveFromClassList("rarity-pill-active");
            }
            if (activeBtn != null) activeBtn.AddToClassList("rarity-pill-active");

            foreach (var item in listings)
            {
                if (item.element == null) continue;
                bool show = rarity == "Todas" || item.rarity == rarity;
                item.element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void BuyCard(int id, string name, int price, VisualElement cardEl)
        {
            if (currentCoins >= price)
            {
                currentCoins -= price;
                UpdateCoinsDisplay();
                if (cardEl != null) cardEl.style.display = DisplayStyle.None;

                MarketService.EnsureExists();
                if (MarketService.Instance != null)
                {
                    _ = MarketService.Instance.BuyListedCardAsync($"m_{id}");
                }

                if (modalCardDesc != null)
                {
                    modalCardDesc.text = $"Has adquirido a {name} por {price} monedas.";
                }
                if (feedbackModal != null)
                {
                    feedbackModal.RemoveFromClassList("modal-hidden");
                }
                Debug.Log($"<color=green>[Mercado] ¡Comprado {name} por {price} monedas!</color>");
            }
            else
            {
                Debug.LogWarning("<color=red>[Mercado] Monedas insuficientes para comprar esta carta.</color>");
            }
        }

        private void OpenPublishModal(string cardId, string cardName, string rarity, int defaultPrice)
        {
            currentEditingListingId = 0;
            currentPublishCardId = cardId;
            if (priceModalTitle != null) priceModalTitle.text = "FIJAR PRECIO";
            if (priceModalCardName != null) priceModalCardName.text = $"{cardName} ({rarity})";
            if (priceInputField != null) priceInputField.value = defaultPrice.ToString();
            if (priceModal != null) priceModal.RemoveFromClassList("modal-hidden");
        }

        private void OpenEditPriceModal(int listingId, string cardName, int currentPrice)
        {
            currentEditingListingId = listingId;
            if (priceModalTitle != null) priceModalTitle.text = "EDITAR PRECIO";
            if (priceModalCardName != null) priceModalCardName.text = cardName;
            if (priceInputField != null) priceInputField.value = currentPrice.ToString();
            if (priceModal != null) priceModal.RemoveFromClassList("modal-hidden");
        }

        private void ConfirmPriceModal()
        {
            if (priceInputField != null && int.TryParse(priceInputField.value, out int newPrice) && newPrice > 0)
            {
                MarketService.EnsureExists();

                if (currentEditingListingId == 1 && priceActive1 != null)
                {
                    priceActive1.text = newPrice.ToString();
                    if (MarketService.Instance != null)
                    {
                        _ = MarketService.Instance.UpdateListingPriceAsync("my_list_1", newPrice);
                    }
                }
                else if (currentEditingListingId == 2 && priceActive2 != null)
                {
                    priceActive2.text = newPrice.ToString();
                    if (MarketService.Instance != null)
                    {
                        _ = MarketService.Instance.UpdateListingPriceAsync("my_list_2", newPrice);
                    }
                }
                else if (currentEditingListingId == 0)
                {
                    if (MarketService.Instance != null)
                    {
                        _ = MarketService.Instance.ListCardForSaleAsync(currentPublishCardId, newPrice, 1);
                    }
                    Debug.Log($"<color=gold>[Mercado] Carta {currentPublishCardId} publicada por {newPrice} monedas.</color>");
                }

                if (priceModal != null) priceModal.AddToClassList("modal-hidden");
            }
        }

        private void WithdrawListing(VisualElement card, string name)
        {
            if (card != null) card.style.display = DisplayStyle.None;

            MarketService.EnsureExists();
            if (MarketService.Instance != null)
            {
                string targetId = (card == cardActive1) ? "my_list_1" : "my_list_2";
                _ = MarketService.Instance.CancelListingAsync(targetId);
            }

            Debug.Log($"<color=yellow>[Mercado] Carta {name} retirada del mercado y devuelta a tu colección.</color>");
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
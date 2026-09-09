using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    public class UIToolkitStoreController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Label coinsCountLabel;
        private Label adCounterNumber;

        // Feedback Modal
        private VisualElement storeFeedbackModal;
        private Label modalIcon;
        private Label modalTitle;
        private Label modalDesc;
        private Button btnCloseFeedback;

        private int currentCoins = 240;
        private int currentAdCount = 2;
        private const int maxAdCount = 3;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            // Header Elements
            coinsCountLabel = root.Q<Label>("CoinsCountLabel");
            adCounterNumber = root.Q<Label>("AdCounterNumber");

            // Modal Elements
            storeFeedbackModal = root.Q<VisualElement>("StoreFeedbackModal");
            if (storeFeedbackModal != null)
            {
                storeFeedbackModal.style.display = DisplayStyle.None;
                storeFeedbackModal.AddToClassList("modal-hidden");
            }
            modalIcon = root.Q<Label>("ModalIcon");
            modalTitle = root.Q<Label>("ModalTitle");
            modalDesc = root.Q<Label>("ModalDesc");
            btnCloseFeedback = root.Q<Button>("Btn_CloseFeedback");
            if (btnCloseFeedback != null)
            {
                btnCloseFeedback.clicked += CloseFeedback;
            }

            // Sync with Firebase if available
            if (FirebaseAuthManager.Instance != null)
            {
                currentCoins = FirebaseAuthManager.Instance.Coins;
            }
            UpdateCoinsDisplay();

            // Wire Pack Buttons
            WirePack("Pack_A", "Btn_BuyPack_A", "Sobre A", 100);
            WirePack("Pack_B", "Btn_BuyPack_B", "Sobre B", 300);
            WirePack("Pack_C", "Btn_BuyPack_C", "Sobre C", 600);

            // Wire Ad Banner
            Button adBanner = root.Q<Button>("AdBannerButton");
            if (adBanner != null)
            {
                adBanner.clicked += OnClickWatchAd;
            }

            // Wire Coin Packs
            WireCoinPack("CoinPack_1", 150, "$0.99");
            WireCoinPack("CoinPack_2", 400, "$1.99");
            WireCoinPack("CoinPack_3", 900, "$3.99");
            WireCoinPack("CoinPack_4", 2000, "$7.99");

            // Wire Liquid Glass Bottom Nav Bar
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Tienda);
        }

        private void WirePack(string packBtnName, string buyBtnName, string packName, int price)
        {
            Button packBtn = root.Q<Button>(packBtnName);
            Button buyBtn = root.Q<Button>(buyBtnName);

            System.Action buyAction = () => BuyPack(packName, price);

            if (packBtn != null) packBtn.clicked += buyAction;
            if (buyBtn != null) buyBtn.clicked += buyAction;
        }

        private void BuyPack(string packName, int price)
        {
            if (currentCoins >= price)
            {
                currentCoins -= price;
                UpdateCoinsDisplay();
                if (FirebaseAuthManager.Instance != null)
                {
                    FirebaseAuthManager.Instance.AddCoins(-price);
                }

                Debug.Log($"<color=green>[Tienda] ¡Comprado {packName} por {price} monedas! Abriendo experiencia cinemática...</color>");

                bool isGuaranteedHolo = packName.Contains("C") || price >= 600;
                UIToolkitPackOpeningController.ConfigureSession(packName, count: 1, forceHolo: isGuaranteedHolo);
                SceneManager.LoadScene("PackOpeningSceneUIToolkit");
            }
            else
            {
                ShowFeedback("⚠️", "MONEDAS INSUFICIENTES", $"Necesitas {price} monedas para comprar {packName}. Tienes {currentCoins} monedas.");
            }
        }

        private void OnClickWatchAd()
        {
            if (currentAdCount < maxAdCount)
            {
                currentAdCount++;
                if (adCounterNumber != null)
                {
                    adCounterNumber.text = $"{currentAdCount}/{maxAdCount}";
                }

                currentCoins += 50;
                UpdateCoinsDisplay();
                if (FirebaseAuthManager.Instance != null)
                {
                    FirebaseAuthManager.Instance.AddCoins(50);
                }

                Debug.Log("<color=gold>[Tienda] ¡Anuncio visto! Recompensa: 1 Sobre Gratis + 50 Monedas. Abriendo sobre...</color>");
                UIToolkitPackOpeningController.ConfigureSession("Sobre Anuncio Recompensa", count: 1, forceHolo: false);
                SceneManager.LoadScene("PackOpeningSceneUIToolkit");
            }
            else
            {
                ShowFeedback("⏳", "LÍMITE ALCANZADO", "Has alcanzado el límite diario de 3 anuncios por hoy.");
            }
        }

        private void WireCoinPack(string packBtnName, int coins, string priceTag)
        {
            Button packBtn = root.Q<Button>(packBtnName);
            if (packBtn != null)
            {
                packBtn.clicked += () =>
                {
                    currentCoins += coins;
                    UpdateCoinsDisplay();
                    if (FirebaseAuthManager.Instance != null)
                    {
                        FirebaseAuthManager.Instance.AddCoins(coins);
                    }

                    ShowFeedback("💰", "¡MONEDAS AÑADIDAS!", $"Has adquirido {coins} monedas por {priceTag}.");
                };
            }
        }

        private void UpdateCoinsDisplay()
        {
            if (coinsCountLabel != null)
            {
                coinsCountLabel.text = currentCoins.ToString();
            }
        }

        private void ShowFeedback(string icon, string title, string desc)
        {
            if (storeFeedbackModal != null)
            {
                if (modalIcon != null) modalIcon.text = icon;
                if (modalTitle != null) modalTitle.text = title;
                if (modalDesc != null) modalDesc.text = desc;
                storeFeedbackModal.RemoveFromClassList("modal-hidden");
                storeFeedbackModal.style.display = DisplayStyle.Flex;
            }
        }

        private void CloseFeedback()
        {
            if (storeFeedbackModal != null)
            {
                storeFeedbackModal.AddToClassList("modal-hidden");
                storeFeedbackModal.style.display = DisplayStyle.None;
            }
        }
    }
}
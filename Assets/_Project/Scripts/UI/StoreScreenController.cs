using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    public class StoreScreenController : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text coinsText;

        [Header("Packs")]
        [SerializeField] private Button packAButton;
        [SerializeField] private Button packBButton;
        [SerializeField] private Button packCButton;

        [Header("Rewarded Ad")]
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TMP_Text adCountText;

        [Header("Coin Packs")]
        [SerializeField] private Button coinPack1Button;
        [SerializeField] private Button coinPack2Button;
        [SerializeField] private Button coinPack3Button;
        [SerializeField] private Button coinPack4Button;

        [Header("Bottom Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private int currentAdCount = 2;
        private const int maxAdCount = 3;
        private int currentCoins = 240;

        private void Awake()
        {
            FindReferencesIfMissing();
        }

        private void Start()
        {
            FindReferencesIfMissing();
            UpdateUI();
            BindButtonEvents();
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

        private void UpdateUI()
        {
            if (coinsText != null) coinsText.text = currentCoins.ToString();
            if (adCountText != null) adCountText.text = $"{currentAdCount}/{maxAdCount}";
        }

        private void BindButtonEvents()
        {
            if (packAButton != null) packAButton.onClick.AddListener(() => BuyPack("Sobre A", 100));
            if (packBButton != null) packBButton.onClick.AddListener(() => BuyPack("Sobre B", 300));
            if (packCButton != null) packCButton.onClick.AddListener(() => BuyPack("Sobre C", 600));

            if (watchAdButton != null) watchAdButton.onClick.AddListener(OnClickWatchAd);

            if (coinPack1Button != null) coinPack1Button.onClick.AddListener(() => BuyCoins(150, "$0.99"));
            if (coinPack2Button != null) coinPack2Button.onClick.AddListener(() => BuyCoins(400, "$1.99"));
            if (coinPack3Button != null) coinPack3Button.onClick.AddListener(() => BuyCoins(1000, "$3.99")); // 900 + 100
            if (coinPack4Button != null) coinPack4Button.onClick.AddListener(() => BuyCoins(2300, "$7.99")); // 2000 + 300

            if (tabInicioButton != null) tabInicioButton.onClick.AddListener(OnClickInicio);
            if (tabCartasButton != null) tabCartasButton.onClick.AddListener(OnClickMisCartas);
            if (tabComunidadButton != null) tabComunidadButton.onClick.AddListener(OnClickComunidad);
            if (tabPerfilButton != null) tabPerfilButton.onClick.AddListener(OnClickPerfil);
        }

        public void BuyPack(string packName, int price)
        {
            if (currentCoins >= price)
            {
                currentCoins -= price;
                UpdateUI();
                Debug.Log($"<color=green>[Tienda] ¡Comprado {packName} por {price} monedas! Abriendo sobre...</color>");
                SceneManager.LoadScene("PackOpeningScene");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[Tienda] Monedas insuficientes ({currentCoins}/{price}) para comprar {packName}.</color>");
            }
        }

        public void OnClickWatchAd()
        {
            if (currentAdCount < maxAdCount)
            {
                currentAdCount++;
                UpdateUI();
                Debug.Log($"<color=gold>[Tienda] ¡Anuncio visto ({currentAdCount}/{maxAdCount})! Recompensa: 1 Sobre Gratis.</color>");
                SceneManager.LoadScene("PackOpeningScene");
            }
            else
            {
                Debug.Log("<color=yellow>[Tienda] Ya has visto el máximo de anuncios de hoy (3/3).</color>");
            }
        }

        public void BuyCoins(int amount, string priceTag)
        {
            // GDD 10.1 Momento 1: Invitar a vincular cuenta antes de la primera compra real con dinero
            if (FirebaseAuthManager.Instance != null && !FirebaseAuthManager.Instance.IsLinked)
            {
                Debug.LogWarning("<color=yellow>[GDD 10.1 Momento 1] Cuenta no vinculada: Para proteger tu compra real (" + priceTag + "), es necesario vincular tu cuenta con Google o Email.</color>");
                SceneManager.LoadScene("LoginScene");
                return;
            }

            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.AddCoins(amount);
                currentCoins = FirebaseAuthManager.Instance.Coins;
            }
            else
            {
                currentCoins += amount;
            }

            UpdateUI();
            Debug.Log($"<color=green>[Tienda] ¡Paquete de {amount} monedas comprado con éxito ({priceTag})! Nuevo saldo: {currentCoins}</color>");
        }

        public void OnClickInicio()
        {
            Debug.Log("<color=green>[Tienda] Navegando a Inicio...</color>");
            SceneManager.LoadScene("HomeScreenScene");
        }

        public void OnClickMisCartas()
        {
            Debug.Log("<color=green>[Tienda] Navegando a Mis Cartas...</color>");
            SceneManager.LoadScene("MyCardsScene");
        }

        public void OnClickComunidad()
        {
            Debug.Log("<color=green>[Tienda] Navegando a Comunidad...</color>");
            SceneManager.LoadScene("CommunityScene");
        }

        public void OnClickPerfil()
        {
            Debug.Log("<color=green>[Tienda] Navegando a Perfil...</color>");
            SceneManager.LoadScene("ProfileScene");
        }
    }
}

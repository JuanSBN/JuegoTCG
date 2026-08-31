using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class CommunityScreenController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;

        [Header("Feature Buttons")]
        [SerializeField] private Button showcasesButton;    // Vitrinas públicas
        [SerializeField] private Button exchangeButton;     // Intercambio
        [SerializeField] private Button sellButton;         // Vender duplicados
        [SerializeField] private Button friendsButton;      // Amigos

        [Header("Bottom Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private void Start()
        {
            InitializeHeader();
            BindNavigationEvents();
        }

        private void InitializeHeader()
        {
            if (titleText != null) titleText.text = "COMUNIDAD";
        }

        private void BindNavigationEvents()
        {
            if (tabInicioButton != null)
            {
                tabInicioButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[CommunityScreen] Regresando a Pantalla de Inicio...</color>");
                    SceneManager.LoadScene("HomeScreenScene");
                });
            }

            if (tabCartasButton != null)
            {
                tabCartasButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[CommunityScreen] Navegando a Mis Cartas...</color>");
                    SceneManager.LoadScene("MyCardsScene");
                });
            }

            if (tabTiendaButton != null)
            {
                tabTiendaButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[CommunityScreen] Navegando a Tienda...</color>");
                    SceneManager.LoadScene("StoreScene");
                });
            }

            if (tabPerfilButton != null)
            {
                tabPerfilButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[CommunityScreen] Navegando a Perfil...</color>");
                    SceneManager.LoadScene("ProfileScene");
                });
            }

            if (showcasesButton != null)
            {
                showcasesButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[Community] Navegando a Vitrinas Públicas...</color>");
                    SceneManager.LoadScene("VitrinesScene");
                });
            }

            if (exchangeButton != null)
            {
                exchangeButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[Community] Navegando a Intercambio...</color>");
                    SceneManager.LoadScene("TradeScene");
                });
            }
            if (sellButton != null)
            {
                sellButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[Community] Navegando a Mercado / Vender Duplicados...</color>");
                    SceneManager.LoadScene("MarketScene");
                });
            }
            if (friendsButton != null)
            {
                friendsButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[Community] Navegando a Amigos...</color>");
                    SceneManager.LoadScene("FriendsScene");
                });
            }
        }
    }
}

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

            if (tabPerfilButton != null)
            {
                tabPerfilButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[CommunityScreen] Navegando a Perfil...</color>");
                    SceneManager.LoadScene("ProfileScene");
                });
            }

            if (showcasesButton != null) showcasesButton.onClick.AddListener(() => Debug.Log("<color=yellow>[Community] Vitrinas públicas clicked</color>"));
            if (exchangeButton != null) exchangeButton.onClick.AddListener(() => Debug.Log("<color=yellow>[Community] Intercambio clicked</color>"));
            if (sellButton != null) sellButton.onClick.AddListener(() => Debug.Log("<color=yellow>[Community] Vender duplicados clicked</color>"));
            if (friendsButton != null) friendsButton.onClick.AddListener(() => Debug.Log("<color=yellow>[Community] Amigos clicked</color>"));
        }
    }
}

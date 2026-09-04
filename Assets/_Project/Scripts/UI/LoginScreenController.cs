using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    public class LoginScreenController : MonoBehaviour
    {
        [Header("Logo & Branding Slot")]
        [SerializeField] private RectTransform logoSlotContainer;
        [SerializeField] private Image logoCardIcon;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Provider Buttons")]
        [SerializeField] private Button googleButton;
        [SerializeField] private Button emailButton;
        [SerializeField] private Button guestButton; // "Continuar como invitado" o "Ahora no"

        [Header("Mode")]
        [SerializeField] private bool isLinkingMode = false; // true si viene de Ajustes o Tienda para vincular cuenta

        private void Start()
        {
            BindButtons();
            UpdateTexts();
        }

        public void SetLinkingMode(bool linking)
        {
            isLinkingMode = linking;
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            if (titleText != null)
            {
                titleText.text = isLinkingMode ? "GUARDA TU PROGRESO" : "BIENVENIDO";
            }

            if (subtitleText != null)
            {
                subtitleText.text = isLinkingMode
                    ? "Vincula tu cuenta para no perder tu colección si cambias de dispositivo."
                    : "Inicia sesión para acceder a tu colección y conectarte con otros jugadores.";
            }

            if (guestButton != null)
            {
                TMP_Text guestText = guestButton.GetComponentInChildren<TMP_Text>();
                if (guestText != null)
                {
                    guestText.text = isLinkingMode ? "Ahora no" : "Continuar como invitado";
                }
            }
        }

        private void BindButtons()
        {
            if (googleButton != null)
            {
                googleButton.onClick.RemoveAllListeners();
                googleButton.onClick.AddListener(OnClickGoogleLogin);
            }

            if (emailButton != null)
            {
                emailButton.onClick.RemoveAllListeners();
                emailButton.onClick.AddListener(OnClickEmailLogin);
            }

            if (guestButton != null)
            {
                guestButton.onClick.RemoveAllListeners();
                guestButton.onClick.AddListener(OnClickContinueAsGuest);
            }
        }

        private void OnClickGoogleLogin()
        {
            Debug.Log("<color=cyan>[Login] Iniciando selector nativo de Google Sign-In...</color>");
            if (googleButton != null) googleButton.interactable = false;

            GoogleSignInManager.EnsureExists();
            GoogleSignInManager.Instance.SignIn(
                async (googleUser) =>
                {
                    Debug.Log($"<color=green>[Login] Cuenta seleccionada con éxito: {googleUser.DisplayName} ({googleUser.Email})</color>");
                    if (FirebaseAuthManager.Instance != null)
                    {
                        await FirebaseAuthManager.Instance.LinkGoogleAccountAsync(googleUser);
                    }
                    SceneManager.LoadScene("HomeScreenUIToolkitScene");
                },
                (error) =>
                {
                    Debug.LogWarning($"<color=yellow>[Login] Google Sign-In cancelado o falló: {error}</color>");
                    if (googleButton != null) googleButton.interactable = true;
                }
            );
        }

        private async void OnClickEmailLogin()
        {
            Debug.Log("<color=cyan>[Login] Autenticando con Email / Vinculando credencial (linkWithCredential)...</color>");
            if (FirebaseAuthManager.Instance != null)
            {
                await FirebaseAuthManager.Instance.LinkAccountAsync("email", "usuario@futbol.com");
            }
            SceneManager.LoadScene("HomeScreenUIToolkitScene");
        }

        private void OnClickContinueAsGuest()
        {
            Debug.Log("<color=yellow>[Login] Continuando con cuenta anónima / invitado...</color>");
            SceneManager.LoadScene("HomeScreenUIToolkitScene");
        }
    }
}

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla de Login / Bienvenida (Figma Fidelity 100%).
    /// Maneja el inicio de sesión con Google, Email y el acceso sin fricción como invitado (GDD 10.1, TDD 2.12).
    /// Soporta dos variantes:
    /// - "nosession": Primera vez sin cuenta previa -> "BIENVENIDO" / "Continuar como invitado".
    /// - "link": Vinculación desde Ajustes o Perfil -> "GUARDA TU PROGRESO" / "Ahora no".
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitLoginController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Label titleText;
        private Label subtitleText;
        private Button btnGoogle;
        private Button btnEmail;
        private Button btnGuest;
        private Label guestBtnText;

        [Header("Mode Configuration")]
        [SerializeField] private bool isLinkingMode = false;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            BindUI();
            UpdateTexts();
        }

        public void SetLinkingMode(bool linking)
        {
            isLinkingMode = linking;
            UpdateTexts();
        }

        private void BindUI()
        {
            titleText = root.Q<Label>("TitleText");
            subtitleText = root.Q<Label>("SubtitleText");
            btnGoogle = root.Q<Button>("Btn_Google");
            btnEmail = root.Q<Button>("Btn_Email");
            btnGuest = root.Q<Button>("Btn_Guest");
            guestBtnText = root.Q<Label>("GuestBtnText");

            if (btnGoogle != null)
            {
                btnGoogle.clicked += OnClickGoogleLogin;
            }

            if (btnEmail != null)
            {
                btnEmail.clicked += OnClickEmailLogin;
            }

            if (btnGuest != null)
            {
                btnGuest.clicked += OnClickContinueAsGuest;
            }
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

            if (guestBtnText != null)
            {
                guestBtnText.text = isLinkingMode ? "Ahora no" : "Continuar como invitado";
            }
        }

        private void OnClickGoogleLogin()
        {
            Debug.Log("<color=cyan>[Login UI Toolkit] Iniciando selector nativo de Google Sign-In...</color>");
            if (btnGoogle != null) btnGoogle.SetEnabled(false);

            GoogleSignInManager.EnsureExists();
            GoogleSignInManager.Instance.SignIn(
                async (googleUser) =>
                {
                    Debug.Log($"<color=green>[Login UI Toolkit] Cuenta seleccionada con éxito: {googleUser.DisplayName} ({googleUser.Email})</color>");
                    if (FirebaseAuthManager.Instance != null)
                    {
                        await FirebaseAuthManager.Instance.LinkGoogleAccountAsync(googleUser);
                    }
                    SceneManager.LoadScene("HomeScreenUIToolkitScene");
                },
                (error) =>
                {
                    Debug.LogWarning($"<color=yellow>[Login UI Toolkit] Google Sign-In cancelado o falló: {error}</color>");
                    if (btnGoogle != null) btnGoogle.SetEnabled(true);
                }
            );
        }

        private async void OnClickEmailLogin()
        {
            Debug.Log("<color=cyan>[Login UI Toolkit] Autenticando con Email / Vinculando credencial (linkWithCredential)...</color>");
            if (FirebaseAuthManager.Instance != null)
            {
                await FirebaseAuthManager.Instance.LinkAccountAsync("email", "usuario@futbol.com");
            }
            SceneManager.LoadScene("HomeScreenUIToolkitScene");
        }

        private async void OnClickContinueAsGuest()
        {
            if (isLinkingMode)
            {
                Debug.Log("<color=yellow>[Login UI Toolkit] Omitiendo vinculación (Ahora no)...</color>");
                SceneManager.LoadScene("HomeScreenUIToolkitScene");
            }
            else
            {
                Debug.Log("<color=yellow>[Login UI Toolkit] Continuando con cuenta anónima automática (signInAnonymously)...</color>");
                if (FirebaseAuthManager.Instance != null)
                {
                    await FirebaseAuthManager.Instance.SignInAnonymouslyAsync();
                }
                SceneManager.LoadScene("HomeScreenUIToolkitScene");
            }
        }
    }
}

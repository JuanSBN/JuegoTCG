using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla de Ajustes / Configuración.
    /// Respeta al 100% el diseño de Figma:
    /// - Cabecera con botón de retroceso (< AJUSTES)
    /// - Tarjeta espaciosa con 4 opciones: Música, Notificaciones, Términos y Vincular Cuenta
    /// - Botón CERRAR SESIÓN con diálogo de confirmación
    /// - Versión y barra Liquid Glass integrada
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitSettingsController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Button btnBack;
        private Button btnToggleMusic;
        private Button btnToggleNotifs;
        private Button btnTerms;
        private Button btnLinkAccount;
        private Button btnLogout;

        // Modals
        private VisualElement logoutModal;
        private Button btnConfirmLogout;
        private Button btnCancelLogout;

        private VisualElement feedbackModal;
        private Label feedbackTitle;
        private Label feedbackDesc;
        private Button btnCloseFeedback;

        private bool isMusicOn = true;
        private bool isNotifsOn = true;

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
            // Back Button -> Return to ProfileSceneUIToolkit
            btnBack = root.Q<Button>("Btn_Back");
            if (btnBack != null)
            {
                btnBack.clicked += () => SceneManager.LoadScene("ProfileSceneUIToolkit");
            }

            // Toggles
            btnToggleMusic = root.Q<Button>("Btn_ToggleMusic");
            if (btnToggleMusic != null)
            {
                btnToggleMusic.clicked += () =>
                {
                    isMusicOn = !isMusicOn;
                    UpdateToggleVisual(btnToggleMusic, isMusicOn);
                };
            }

            btnToggleNotifs = root.Q<Button>("Btn_ToggleNotifs");
            if (btnToggleNotifs != null)
            {
                btnToggleNotifs.clicked += () =>
                {
                    isNotifsOn = !isNotifsOn;
                    UpdateToggleVisual(btnToggleNotifs, isNotifsOn);
                };
            }

            // Interactive Buttons
            btnTerms = root.Q<Button>("Btn_Terms");
            if (btnTerms != null)
            {
                btnTerms.clicked += () => ShowFeedback("TÉRMINOS Y PRIVACIDAD", "Juego TCG Football v0.1.0\nTodos los derechos reservados. Tus datos se encuentran protegidos bajo cifrado Firebase.");
            }

            btnLinkAccount = root.Q<Button>("Btn_LinkAccount");
            if (btnLinkAccount != null)
            {
                btnLinkAccount.clicked += () => ShowFeedback("VINCULAR CUENTA", "Tu cuenta actual se encuentra sincronizada de manera anónima/Google Play Games.");
            }

            // Logout Flow
            btnLogout = root.Q<Button>("Btn_Logout");
            logoutModal = root.Q<VisualElement>("LogoutModal");
            btnConfirmLogout = root.Q<Button>("Btn_ConfirmLogout");
            btnCancelLogout = root.Q<Button>("Btn_CancelLogout");

            if (btnLogout != null && logoutModal != null)
            {
                btnLogout.clicked += () => logoutModal.RemoveFromClassList("modal-hidden");
            }

            if (btnCancelLogout != null && logoutModal != null)
            {
                btnCancelLogout.clicked += () => logoutModal.AddToClassList("modal-hidden");
            }

            if (btnConfirmLogout != null)
            {
                btnConfirmLogout.clicked += () =>
                {
                    if (FirebaseAuthManager.Instance != null)
                    {
                        FirebaseAuthManager.Instance.SignOut();
                    }
                    SceneManager.LoadScene("SplashScreen");
                };
            }

            // Feedback Modal
            feedbackModal = root.Q<VisualElement>("FeedbackModal");
            feedbackTitle = root.Q<Label>("FeedbackTitle");
            feedbackDesc = root.Q<Label>("FeedbackDesc");
            btnCloseFeedback = root.Q<Button>("Btn_CloseFeedback");

            if (btnCloseFeedback != null && feedbackModal != null)
            {
                btnCloseFeedback.clicked += () => feedbackModal.AddToClassList("modal-hidden");
            }

            // Bottom Nav
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Perfil);
        }

        private void UpdateToggleVisual(Button btn, bool isOn)
        {
            if (isOn)
            {
                btn.RemoveFromClassList("toggle-off");
            }
            else
            {
                btn.AddToClassList("toggle-off");
            }
        }

        private void ShowFeedback(string title, string desc)
        {
            if (feedbackTitle != null) feedbackTitle.text = title;
            if (feedbackDesc != null) feedbackDesc.text = desc;
            if (feedbackModal != null) feedbackModal.RemoveFromClassList("modal-hidden");
        }
    }
}
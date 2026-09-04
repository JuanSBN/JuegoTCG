using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla de Perfil (ProfileScreen).
    /// Maneja el avatar, nombre de usuario, copiado de código de amigo,
    /// la cancha táctica con el 11 Ideal interactivo, las cartas destacadas
    /// y la navegación global de las 5 pestañas.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitProfileController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Button btnSettings;
        private Button btnEditAvatar;
        private Button btnEditUsername;
        private Button btnCopyFriendCode;
        private Label friendCodeText;
        private Label usernameText;

        // Feedback Modal
        private VisualElement feedbackModal;
        private Label modalTitle;
        private Label modalDesc;
        private Button btnCloseModal;

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
            // Settings Button
            btnSettings = root.Q<Button>("Btn_Settings");
            if (btnSettings != null)
            {
                btnSettings.clicked += () => SceneManager.LoadScene("SettingsSceneUIToolkit");
            }

            // Copy Friend Code
            btnCopyFriendCode = root.Q<Button>("Btn_CopyFriendCode");
            friendCodeText = root.Q<Label>("FriendCodeText");
            if (btnCopyFriendCode != null)
            {
                btnCopyFriendCode.clicked += CopyFriendCode;
            }

            // Edit Profile Actions
            btnEditAvatar = root.Q<Button>("Btn_EditAvatar");
            btnEditUsername = root.Q<Button>("Btn_EditUsername");
            usernameText = root.Q<Label>("UsernameText");

            if (FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(FirebaseAuthManager.Instance.DisplayName))
            {
                if (usernameText != null) usernameText.text = FirebaseAuthManager.Instance.DisplayName.ToUpper();
            }

            if (btnEditAvatar != null)
            {
                btnEditAvatar.clicked += () => ShowModal("EDITAR AVATAR", "Elige un nuevo marco o ícono de jugador para personalizar tu perfil.");
            }

            if (btnEditUsername != null)
            {
                btnEditUsername.clicked += () => ShowModal("EDITAR NOMBRE", "Puedes modificar tu apodo de entrenador desde Ajustes.");
            }

            // Wire 11 Ideal Slots
            WirePitchSlot("Slot_F1", "Delantero Izquierdo (Rara)", true);
            WirePitchSlot("Slot_F2", "Delantero Centro (Vacío)", false);
            WirePitchSlot("Slot_F3", "Delantero Derecho (Vacío)", false);
            WirePitchSlot("Slot_M1", "Mediocentro Ofensivo (Mítica)", true);
            WirePitchSlot("Slot_M2", "Mediocentro (Vacío)", false);
            WirePitchSlot("Slot_M3", "Mediocentro Derecho (Común)", true);
            WirePitchSlot("Slot_D1", "Lateral Izquierdo (Poco común)", true);
            WirePitchSlot("Slot_D2", "Defensa Central (Vacío)", false);
            WirePitchSlot("Slot_D3", "Defensa Central (Vacío)", false);
            WirePitchSlot("Slot_D4", "Lateral Derecho (Rara)", true);
            WirePitchSlot("Slot_G1", "Portero (Vacío)", false);

            // Wire Featured Cards
            var featured1 = root.Q<VisualElement>("Featured_Card_1");
            var featured2 = root.Q<VisualElement>("Featured_Card_2");
            var featured3 = root.Q<VisualElement>("Featured_Card_3");

            if (featured1 != null) featured1.RegisterCallback<ClickEvent>(evt => ShowModal("CARTA DESTACADA", "Luis Díaz (MÍTICA) - Posición estelar en tu vitrina de perfil."));
            if (featured2 != null) featured2.RegisterCallback<ClickEvent>(evt => ShowModal("CARTA DESTACADA", "Bellingham (RARA) - Carta clave en tu mediocampo."));
            if (featured3 != null) featured3.RegisterCallback<ClickEvent>(evt => ShowModal("ESPACIO DISPONIBLE", "Selecciona una carta de tu colección para destacarla aquí."));

            // Feedback Modal
            feedbackModal = root.Q<VisualElement>("ProfileFeedbackModal");
            modalTitle = root.Q<Label>("ModalTitle");
            modalDesc = root.Q<Label>("ModalDesc");
            btnCloseModal = root.Q<Button>("Btn_CloseModal");

            if (btnCloseModal != null)
            {
                btnCloseModal.clicked += () => feedbackModal.AddToClassList("modal-hidden");
            }

            // Bottom Nav
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Perfil);
        }

        private void WirePitchSlot(string slotName, string slotDesc, bool isOccupied)
        {
            var slotEl = root.Q<VisualElement>(slotName);
            if (slotEl != null)
            {
                slotEl.RegisterCallback<ClickEvent>(evt =>
                {
                    string status = isOccupied ? "Ocupado con una de tus mejores cartas." : "Espacio vacío. Asigna un jugador desde Mis Cartas.";
                    ShowModal("MI 11 IDEAL", $"{slotDesc}: {status}");
                });
            }
        }

        private void CopyFriendCode()
        {
            GUIUtility.systemCopyBuffer = "4872-1093";
            ShowModal("CÓDIGO COPIADO", "Tu código de amigo (4872-1093) se ha copiado al portapapeles.");
            Debug.Log("<color=gold>[Perfil] Código 4872-1093 copiado.</color>");
        }

        private void ShowModal(string title, string desc)
        {
            if (modalTitle != null) modalTitle.text = title;
            if (modalDesc != null) modalDesc.text = desc;
            if (feedbackModal != null) feedbackModal.RemoveFromClassList("modal-hidden");
        }
    }
}
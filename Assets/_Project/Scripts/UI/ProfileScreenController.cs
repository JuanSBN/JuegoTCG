using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JuegoTCG.Cards;
using JuegoTCG.Core;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    public class ProfileScreenController : MonoBehaviour
    {
        [Header("User Profile")]
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private Image avatarFrameImage;
        [SerializeField] private TMP_Text friendCodeText;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private TMP_Text copyStatusText;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button editProfileButton;

        [Header("Tactical Pitch (11 Ideal)")]
        [SerializeField] private Image pitchBackgroundImage;
        [SerializeField] private TMP_Text formationCounterText;
        [SerializeField] private TMP_Text squadPowerText;

        [Header("Bottom Tabs (5 Tabs)")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private const string FriendCode = "4872-1093";

        private void Awake()
        {
            FindReferencesIfMissing();
        }

        private void Start()
        {
            FindReferencesIfMissing();
            EnsureManagers();
            InitializeProfile();
            BindNavigationEvents();
            RenderPitchLineup();
        }

        private void EnsureManagers()
        {
            if (Ideal11SquadManager.Instance == null)
            {
                GameObject smGO = new GameObject("Ideal11SquadManager");
                smGO.AddComponent<Ideal11SquadManager>();
            }

            if (ProfileCustomizationManager.Instance == null)
            {
                GameObject cmGO = new GameObject("ProfileCustomizationManager");
                cmGO.AddComponent<ProfileCustomizationManager>();
            }

            Ideal11SquadManager.Instance.OnSquadUpdated -= OnSquadChanged;
            Ideal11SquadManager.Instance.OnSquadUpdated += OnSquadChanged;

            ProfileCustomizationManager.Instance.OnCustomizationUpdated -= OnCustomizationChanged;
            ProfileCustomizationManager.Instance.OnCustomizationUpdated += OnCustomizationChanged;
        }

        private void OnDestroy()
        {
            if (Ideal11SquadManager.Instance != null)
            {
                Ideal11SquadManager.Instance.OnSquadUpdated -= OnSquadChanged;
            }

            if (ProfileCustomizationManager.Instance != null)
            {
                ProfileCustomizationManager.Instance.OnCustomizationUpdated -= OnCustomizationChanged;
            }
        }

        private void OnSquadChanged()
        {
            InitializeProfile();
            RenderPitchLineup();
        }

        private void OnCustomizationChanged()
        {
            InitializeProfile();
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

        private void InitializeProfile()
        {
            string currentName = PlayerPrefs.GetString("Firebase_DisplayName", "JUGADOR_01");
            if (usernameText != null) usernameText.text = currentName;

            if (avatarFrameImage != null && ProfileCustomizationManager.Instance != null)
            {
                avatarFrameImage.color = ProfileCustomizationManager.Instance.GetFrameColor();
            }

            if (friendCodeText != null) friendCodeText.text = $"Código de amigo: <color=#b0c0b8>{FriendCode}</color>";

            int filledSlots = Ideal11SquadManager.Instance != null ? Ideal11SquadManager.Instance.GetFilledSlotsCount() : 6;
            int squadPower = Ideal11SquadManager.Instance != null ? Ideal11SquadManager.Instance.CalculateSquadPower() : 118;

            if (formationCounterText != null) formationCounterText.text = $"{filledSlots} / 11 espacios";
            if (squadPowerText != null) squadPowerText.text = $"Poder del 11: {squadPower} pts";

            if (copyStatusText != null) copyStatusText.gameObject.SetActive(false);

            if (copyCodeButton != null)
            {
                copyCodeButton.onClick.RemoveAllListeners();
                copyCodeButton.onClick.AddListener(OnClickCopyCode);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[Profile] Navegando a Ajustes...</color>");
                    SceneManager.LoadScene("SettingsScene");
                });
            }

            if (editProfileButton != null)
            {
                editProfileButton.onClick.RemoveAllListeners();
                editProfileButton.onClick.AddListener(OnClickEditProfile);
            }
        }

        private void OnClickEditProfile()
        {
            Debug.Log("<color=cyan>[Profile] Abriendo panel de personalización de marco y fondo...</color>");
        }

        public void RenderPitchLineup()
        {
            if (Ideal11SquadManager.Instance == null) return;

            var slots = Ideal11SquadManager.Instance.GetSlots();
            foreach (var slot in slots)
            {
                string status = string.IsNullOrEmpty(slot.assignedCardId) ? "[VACÍO]" : $"[{slot.assignedCardId}]";
                Debug.Log($"<color=cyan>[11 Ideal] Posición {slot.positionName} ({slot.positionCode}): {status}</color>");
            }
        }

        private void OnClickCopyCode()
        {
            GUIUtility.systemCopyBuffer = FriendCode;
            Debug.Log($"<color=gold>[Profile] Código de amigo copiado: {FriendCode}</color>");
            if (copyStatusText != null)
            {
                copyStatusText.gameObject.SetActive(true);
                copyStatusText.text = "¡Copiado!";
                CancelInvoke(nameof(HideCopyStatus));
                Invoke(nameof(HideCopyStatus), 2f);
            }
        }

        private void HideCopyStatus()
        {
            if (copyStatusText != null) copyStatusText.gameObject.SetActive(false);
        }

        private void BindNavigationEvents()
        {
            if (tabInicioButton != null)
            {
                tabInicioButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[Profile] Navegando a Inicio...</color>");
                    SceneManager.LoadScene("HomeScreenScene");
                });
            }

            if (tabCartasButton != null)
            {
                tabCartasButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[Profile] Navegando a Mis Cartas...</color>");
                    SceneManager.LoadScene("MyCardsScene");
                });
            }

            if (tabTiendaButton != null)
            {
                tabTiendaButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[Profile] Navegando a Tienda...</color>");
                    SceneManager.LoadScene("StoreScene");
                });
            }

            if (tabComunidadButton != null)
            {
                tabComunidadButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[Profile] Navegando a Comunidad...</color>");
                    SceneManager.LoadScene("CommunityScene");
                });
            }
        }
    }
}

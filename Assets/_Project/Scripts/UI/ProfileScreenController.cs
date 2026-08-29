using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class ProfileScreenController : MonoBehaviour
    {
        [Header("User Profile")]
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private TMP_Text friendCodeText;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private TMP_Text copyStatusText;
        [SerializeField] private Button settingsButton;

        [Header("Tactical Pitch (11 Ideal)")]
        [SerializeField] private TMP_Text formationCounterText;

        [Header("Bottom Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private const string FriendCode = "4872-1093";

        private void Start()
        {
            InitializeProfile();
            BindNavigationEvents();
        }

        private void InitializeProfile()
        {
            if (usernameText != null) usernameText.text = "JUGADOR_01";
            if (friendCodeText != null) friendCodeText.text = $"Código de amigo: <color=#b0c0b8>{FriendCode}</color>";
            if (formationCounterText != null) formationCounterText.text = "5 / 11 espacios";
            if (copyStatusText != null) copyStatusText.gameObject.SetActive(false);

            if (copyCodeButton != null)
            {
                copyCodeButton.onClick.AddListener(OnClickCopyCode);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() => Debug.Log("<color=yellow>[Profile] Ajustes clicked</color>"));
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
                    Debug.Log("<color=green>[ProfileScreen] Regresando a Pantalla de Inicio...</color>");
                    SceneManager.LoadScene("HomeScreenScene");
                });
            }

            if (tabCartasButton != null)
            {
                tabCartasButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[ProfileScreen] Navegando a Mis Cartas...</color>");
                    SceneManager.LoadScene("MyCardsScene");
                });
            }

            if (tabComunidadButton != null)
            {
                tabComunidadButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[ProfileScreen] Navegando a Comunidad...</color>");
                    SceneManager.LoadScene("CommunityScene");
                });
            }
        }
    }
}

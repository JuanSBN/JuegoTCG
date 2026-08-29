using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class HomeScreenController : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text playerLevelText;
        [SerializeField] private TMP_Text coinsText;

        [Header("Daily Streak (Racha Diaria)")]
        [SerializeField] private Slider streakProgressBar;
        [SerializeField] private TMP_Text streakProgressText;
        [SerializeField] private GameObject[] streakDayCheckIcons;

        [Header("Packs")]
        [SerializeField] private Button packAButton;
        [SerializeField] private Button packBButton;
        [SerializeField] private Button packCButton;

        [Header("Quick Actions")]
        [SerializeField] private Button specialEventButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button missionsButton;

        [Header("Bottom Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private void Start()
        {
            InitializeUserData();
            InitializeDailyStreak();
            BindButtonEvents();
        }

        private void InitializeUserData()
        {
            if (playerNameText != null) playerNameText.text = "JUGADOR_01";
            if (playerLevelText != null) playerLevelText.text = "Nivel 7";
            if (coinsText != null) coinsText.text = "240";
        }

        private void InitializeDailyStreak()
        {
            int currentStreak = 3;
            int totalDays = 5;

            if (streakProgressBar != null)
            {
                streakProgressBar.minValue = 0;
                streakProgressBar.maxValue = totalDays;
                streakProgressBar.value = currentStreak;
            }

            if (streakProgressText != null)
            {
                streakProgressText.text = $"{currentStreak} / {totalDays} días";
            }

            if (streakDayCheckIcons != null)
            {
                for (int i = 0; i < streakDayCheckIcons.Length; i++)
                {
                    if (streakDayCheckIcons[i] != null)
                    {
                        streakDayCheckIcons[i].SetActive(i < currentStreak);
                    }
                }
            }
        }

        private void BindButtonEvents()
        {
            if (packAButton != null) packAButton.onClick.AddListener(() => OpenPack("Sobre A"));
            if (packBButton != null) packBButton.onClick.AddListener(() => OpenPack("Sobre B"));
            if (packCButton != null) packCButton.onClick.AddListener(() => OpenPack("Sobre C"));

            if (missionsButton != null) missionsButton.onClick.AddListener(OnClickMissions);
            if (specialEventButton != null) specialEventButton.onClick.AddListener(OnClickSpecialEvent);
            if (shopButton != null) shopButton.onClick.AddListener(OnClickShop);

            if (tabCartasButton != null) tabCartasButton.onClick.AddListener(OnClickMisCartas);
            if (tabComunidadButton != null) tabComunidadButton.onClick.AddListener(OnClickComunidad);
            if (tabPerfilButton != null) tabPerfilButton.onClick.AddListener(OnClickPerfil);
        }

        public void OpenPack(string packName)
        {
            Debug.Log($"<color=gold>[HomeScreen] Abriendo {packName}...</color>");
            SceneManager.LoadScene("PackOpeningScene");
        }

        public void OnClickMissions()
        {
            Debug.Log("<color=yellow>[HomeScreen] Misiones clicked (Fase 4.5)</color>");
        }

        public void OnClickSpecialEvent()
        {
            Debug.Log("<color=cyan>[HomeScreen] Evento Especial clicked</color>");
        }

        public void OnClickShop()
        {
            Debug.Log("<color=green>[HomeScreen] Tienda clicked</color>");
        }

        public void OnClickMisCartas()
        {
            Debug.Log("<color=green>[HomeScreen] Navegando a Mis Cartas...</color>");
            SceneManager.LoadScene("MyCardsScene");
        }

        public void OnClickComunidad()
        {
            Debug.Log("<color=green>[HomeScreen] Navegando a Comunidad...</color>");
            SceneManager.LoadScene("CommunityScene");
        }

        public void OnClickPerfil()
        {
            Debug.Log("<color=green>[HomeScreen] Navegando a Perfil...</color>");
            SceneManager.LoadScene("ProfileScene");
        }
    }
}

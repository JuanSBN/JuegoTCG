using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JuegoTCG.Networking;

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

        [Header("Modals")]
        [SerializeField] private MissionsModalController missionsModal;

        [Header("Bottom Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private void Awake()
        {
            FindReferencesIfMissing();
        }

        private void Start()
        {
            FindReferencesIfMissing();
            InitializeUserData();
            InitializeDailyStreak();
            BindButtonEvents();
        }

        private void FindReferencesIfMissing()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

            if (missionsModal == null)
            {
                if (canvas != null) missionsModal = canvas.GetComponentInChildren<MissionsModalController>(true);
                if (missionsModal == null) missionsModal = FindFirstObjectByType<MissionsModalController>(FindObjectsInactive.Include);
            }

            if (canvas != null)
            {
                var buttons = canvas.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (missionsButton == null && b.name.Contains("Misiones"))
                    {
                        missionsButton = b;
                        if (b.GetComponent<MissionsButtonTrigger>() == null)
                        {
                            b.gameObject.AddComponent<MissionsButtonTrigger>();
                        }

                        // Formatear botón Misiones responsivo para evitar salto de línea
                        RectTransform rt = b.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            Vector2 sd = rt.sizeDelta;
                            sd.x = Mathf.Max(sd.x, 240f);
                            sd.y = Mathf.Max(sd.y, 68f);
                            rt.sizeDelta = sd;
                        }

                        TMP_Text[] bTexts = b.GetComponentsInChildren<TMP_Text>(true);
                        foreach (var bt in bTexts)
                        {
                            bt.enableWordWrapping = false;
                            bt.overflowMode = TextOverflowModes.Overflow;
                            bt.alignment = TextAlignmentOptions.Center;
                            if (bt.text.Contains("MISIONE")) bt.text = "MISIONES";
                        }
                    }
                    else if (specialEventButton == null && b.name.Contains("SpecialEvent")) specialEventButton = b;
                    else if (shopButton == null && b.name.Contains("ShopButton")) shopButton = b;
                    else if (packAButton == null && (b.name.Contains("Envelope_0") || b.name == "PackAButton")) packAButton = b;
                    else if (packBButton == null && (b.name.Contains("Envelope_1") || b.name == "PackBButton")) packBButton = b;
                    else if (packCButton == null && (b.name.Contains("Envelope_2") || b.name == "PackCButton")) packCButton = b;
                    else if (tabCartasButton == null && b.name.Contains("cartas")) tabCartasButton = b;
                    else if (tabTiendaButton == null && b.name.Contains("Tienda")) tabTiendaButton = b;
                    else if (tabComunidadButton == null && b.name.Contains("Comunidad")) tabComunidadButton = b;
                    else if (tabPerfilButton == null && b.name.Contains("Perfil")) tabPerfilButton = b;
                }
            }
        }

        private void InitializeUserData()
        {
            if (FirebaseAuthManager.Instance != null)
            {
                if (playerNameText != null) playerNameText.text = FirebaseAuthManager.Instance.DisplayName;
                if (playerLevelText != null) playerLevelText.text = $"Nivel {FirebaseAuthManager.Instance.PlayerLevel}";
                if (coinsText != null) coinsText.text = FirebaseAuthManager.Instance.Coins.ToString("N0");

                FirebaseAuthManager.Instance.OnCoinsChanged -= OnCoinsUpdated;
                FirebaseAuthManager.Instance.OnCoinsChanged += OnCoinsUpdated;
            }
            else
            {
                if (playerNameText != null) playerNameText.text = "JUGADOR_01";
                if (playerLevelText != null) playerLevelText.text = "Nivel 1";
                if (coinsText != null) coinsText.text = "300";
            }
        }

        private void OnCoinsUpdated(int newCoins)
        {
            if (coinsText != null) coinsText.text = newCoins.ToString("N0");
        }

        private void OnDestroy()
        {
            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.OnCoinsChanged -= OnCoinsUpdated;
            }
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
            if (tabTiendaButton != null) tabTiendaButton.onClick.AddListener(OnClickShop);
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
            Debug.Log("<color=yellow>[HomeScreen] Abriendo sub-pantalla de Misiones con blur de fondo...</color>");
            if (missionsModal == null)
            {
                FindReferencesIfMissing();
            }

            if (missionsModal != null)
            {
                StartCoroutine(CaptureScreenAndOpenMissionsRoutine());
            }
            else
            {
                Debug.LogWarning("<color=red>[HomeScreen] No se encontró el componente MissionsModalController en la escena!</color>");
            }
        }

        private System.Collections.IEnumerator CaptureScreenAndOpenMissionsRoutine()
        {
            yield return new WaitForEndOfFrame();

            Texture2D blurTex = null;
            try
            {
                Texture2D screenTex = ScreenCapture.CaptureScreenshotAsTexture();
                if (screenTex != null)
                {
                    int w = Mathf.Max(256, screenTex.width / 2);
                    int h = Mathf.Max(256, screenTex.height / 2);

                    Shader blurShader = Shader.Find("Hidden/GaussianBlur");
                    if (blurShader != null)
                    {
                        Material blurMat = new Material(blurShader);
                        blurMat.SetFloat("_Offset", 1.25f);

                        RenderTexture rt1 = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                        RenderTexture rt2 = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                        rt1.filterMode = FilterMode.Bilinear;
                        rt2.filterMode = FilterMode.Bilinear;

                        Graphics.Blit(screenTex, rt1);

                        // 3 Continuous Gaussian Passes (Zero grid dots, zero mosaic)
                        Graphics.Blit(rt1, rt2, blurMat, 0);
                        Graphics.Blit(rt2, rt1, blurMat, 1);

                        Graphics.Blit(rt1, rt2, blurMat, 0);
                        Graphics.Blit(rt2, rt1, blurMat, 1);

                        Graphics.Blit(rt1, rt2, blurMat, 0);
                        Graphics.Blit(rt2, rt1, blurMat, 1);

                        blurTex = new Texture2D(w, h, TextureFormat.RGB24, false);
                        RenderTexture.active = rt1;
                        blurTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                        blurTex.filterMode = FilterMode.Bilinear;
                        blurTex.Apply();

                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(rt1);
                        RenderTexture.ReleaseTemporary(rt2);
                        Destroy(blurMat);
                    }
                    Destroy(screenTex);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HomeScreen] Error capturando blur: {ex.Message}");
            }

            missionsModal.ShowWithBlur(blurTex);
        }

        public void OnClickSpecialEvent()
        {
            Debug.Log("<color=cyan>[HomeScreen] Evento Especial clicked</color>");
        }

        public void OnClickShop()
        {
            Debug.Log("<color=green>[HomeScreen] Navegando a Tienda...</color>");
            SceneManager.LoadScene("StoreScene");
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

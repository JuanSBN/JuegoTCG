using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno basado en UI Toolkit (Flexbox / CSS) para la Pantalla de Inicio.
    /// Garantiza adaptación matemática 100% responsiva y fluida a cualquier celular.
    /// Incluye desenfoque gaussiano en tiempo real para el modal de misiones.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitHomeScreenController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Label playerNameLabel;
        private Label playerLevelLabel;
        private Label coinsLabel;
        private VisualElement avatarCircle;
        private VisualElement avatarIcon;

        private Button packAButton;
        private Button packBButton;
        private Button packCButton;
        private Button missionsButton;
        private Button eventButton;
        private Button shopButton;

        private VisualElement missionsModal;
        private VisualElement modalBlurBackdrop;
        private Button closeMissionsBtn;
        private Texture2D blurredTexture;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            var root = uiDocument.rootVisualElement;

            // Bind Labels
            playerNameLabel = root.Q<Label>("PlayerName");
            playerLevelLabel = root.Q<Label>("PlayerLevel");
            coinsLabel = root.Q<Label>("CoinsText");

            // Bind Avatar Elements
            avatarCircle = root.Q<VisualElement>("AvatarCircle") ?? root.Q<VisualElement>(className: "avatar-circle");
            avatarIcon = root.Q<VisualElement>("AvatarIcon") ?? root.Q<VisualElement>(className: "avatar-icon");

            // Bind Buttons
            packAButton = root.Q<Button>("PackA");
            packBButton = root.Q<Button>("PackB");
            packCButton = root.Q<Button>("PackC");
            missionsButton = root.Q<Button>("MissionsBtn");
            eventButton = root.Q<Button>("EventBtn");
            shopButton = root.Q<Button>("ShopBtn");

            missionsModal = root.Q<VisualElement>("MissionsModal");
            modalBlurBackdrop = root.Q<VisualElement>("ModalBlurBackdrop");
            closeMissionsBtn = root.Q<Button>("CloseMissionsBtn");

            // Wire Pack Selection
            WirePackCard(packAButton, "pack_bronce", root);
            WirePackCard(packBButton, "pack_oro", root);
            WirePackCard(packCButton, "pack_diamante", root);

            // Wire Quick Actions
            if (shopButton != null) shopButton.clicked += () => SceneManager.LoadScene("StoreSceneUIToolkit");
            if (eventButton != null) eventButton.clicked += () => Debug.Log("<color=cyan>[UI Toolkit] Evento especial presionado</color>");

            // Wire Missions Modal with Gaussian Blur
            if (missionsButton != null)
            {
                missionsButton.clicked += () => StartCoroutine(OpenMissionsWithBlur());
            }

            if (closeMissionsBtn != null)
            {
                closeMissionsBtn.clicked += CloseMissions;
            }

            // Wire Bottom Navigation Bar
            WireBottomNav(root);

            UpdatePlayerData();
        }

        private void WirePackCard(Button packBtn, string packType, VisualElement root)
        {
            if (packBtn == null) return;
            packBtn.clicked += () =>
            {
                // Unhighlight all
                SetPackActive(root.Q<Button>("PackA"), false);
                SetPackActive(root.Q<Button>("PackB"), false);
                SetPackActive(root.Q<Button>("PackC"), false);

                // Highlight selected
                SetPackActive(packBtn, true);

                Debug.Log($"<color=gold>[UIToolkit:Home] Sobre seleccionado: {packType}</color>");
            };
        }

        private void SetPackActive(Button packBtn, bool active)
        {
            if (packBtn == null) return;
            var title = packBtn.Q<Label>(className: "pack-card-title");
            if (active)
            {
                packBtn.AddToClassList("pack-card-active");
                if (title != null) title.AddToClassList("pack-card-title-active");
            }
            else
            {
                packBtn.RemoveFromClassList("pack-card-active");
                if (title != null) title.RemoveFromClassList("pack-card-title-active");
            }
        }

        private void WireBottomNav(VisualElement root)
        {
            var navBarController = gameObject.GetComponent<LiquidGlassNavBarController>();
            if (navBarController == null)
            {
                navBarController = gameObject.AddComponent<LiquidGlassNavBarController>();
            }
            navBarController.Initialize(root, LiquidGlassNavBarController.TabType.Inicio);
        }

        private void UpdatePlayerData()
        {
            if (FirebaseAuthManager.Instance != null)
            {
                if (playerNameLabel != null) playerNameLabel.text = FirebaseAuthManager.Instance.DisplayName;
                if (playerLevelLabel != null) playerLevelLabel.text = $"Nivel {FirebaseAuthManager.Instance.PlayerLevel}";
                if (coinsLabel != null) coinsLabel.text = FirebaseAuthManager.Instance.Coins.ToString("N0");

                FirebaseAuthManager.Instance.OnCoinsChanged -= OnCoinsChanged;
                FirebaseAuthManager.Instance.OnCoinsChanged += OnCoinsChanged;

                FirebaseAuthManager.Instance.OnAvatarChanged -= OnAvatarChanged;
                FirebaseAuthManager.Instance.OnAvatarChanged += OnAvatarChanged;

                UserAvatarLoader.LoadAvatar(this, avatarCircle, avatarIcon);
            }
        }

        private void OnAvatarChanged(string newPhotoUrl)
        {
            UserAvatarLoader.LoadAvatar(this, avatarCircle, avatarIcon);
        }

        private void OnCoinsChanged(int newCoins)
        {
            if (coinsLabel != null) coinsLabel.text = newCoins.ToString("N0");
        }

        private void OpenPack(string packType)
        {
            Debug.Log($"<color=gold>[UIToolkit:Home] Abriendo sobre: {packType}</color>");
            PlayerPrefs.SetString("PendingPackType", packType);
            SceneManager.LoadScene("PackOpeningScene");
        }

        private IEnumerator OpenMissionsWithBlur()
        {
            // Espera al final del frame para capturar la pantalla completa con los elementos del Home
            yield return new WaitForEndOfFrame();

            Texture2D screenShot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenShot != null)
            {
                // Downsample a 1/8 para un desenfoque bokeh profundo y máximo rendimiento móvil (60 FPS)
                int width = Mathf.Max(96, Screen.width / 8);
                int height = Mathf.Max(192, Screen.height / 8);

                RenderTexture rt1 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                RenderTexture rt2 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                rt1.filterMode = FilterMode.Bilinear;
                rt2.filterMode = FilterMode.Bilinear;

                Graphics.Blit(screenShot, rt1);
                Destroy(screenShot);

                // Aplicar shader continuo de 7-Tap Gaussian Blur con 3 pasadas amplias
                Shader blurShader = Shader.Find("Hidden/GaussianBlur");
                if (blurShader != null)
                {
                    Material blurMat = new Material(blurShader);

                    // Pasada 1: Desenfoque gaussiano inicial
                    blurMat.SetFloat("_Offset", 3.0f);
                    Graphics.Blit(rt1, rt2, blurMat, 0);
                    Graphics.Blit(rt2, rt1, blurMat, 1);

                    // Pasada 2: Desenfoque intermedio profundo
                    blurMat.SetFloat("_Offset", 5.5f);
                    Graphics.Blit(rt1, rt2, blurMat, 0);
                    Graphics.Blit(rt2, rt1, blurMat, 1);

                    // Pasada 3: Suavizado bokeh ultra-difuso
                    blurMat.SetFloat("_Offset", 7.5f);
                    Graphics.Blit(rt1, rt2, blurMat, 0);
                    Graphics.Blit(rt2, rt1, blurMat, 1);

                    Destroy(blurMat);
                }

                // Guardar en textura persistente para UI Toolkit
                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = rt1;

                if (blurredTexture != null) Destroy(blurredTexture);
                blurredTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
                blurredTexture.filterMode = FilterMode.Bilinear;
                blurredTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                blurredTexture.Apply();

                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt1);
                RenderTexture.ReleaseTemporary(rt2);

                if (modalBlurBackdrop != null)
                {
                    modalBlurBackdrop.style.backgroundImage = Background.FromTexture2D(blurredTexture);
                }
            }

            if (missionsModal != null)
            {
                missionsModal.RemoveFromClassList("modal-hidden");
                Debug.Log("<color=gold>[UI Toolkit] Modal de Misiones abierto con Desenfoque Gaussiano</color>");
            }
        }

        private void CloseMissions()
        {
            if (missionsModal != null)
            {
                missionsModal.AddToClassList("modal-hidden");
                Debug.Log("<color=gold>[UI Toolkit] Modal de Misiones cerrado</color>");
            }

            if (modalBlurBackdrop != null)
            {
                modalBlurBackdrop.style.backgroundImage = null;
            }

            if (blurredTexture != null)
            {
                Destroy(blurredTexture);
                blurredTexture = null;
            }
        }

        private void OnDisable()
        {
            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.OnCoinsChanged -= OnCoinsChanged;
                FirebaseAuthManager.Instance.OnAvatarChanged -= OnAvatarChanged;
            }

            if (blurredTexture != null)
            {
                Destroy(blurredTexture);
                blurredTexture = null;
            }
        }
    }
}

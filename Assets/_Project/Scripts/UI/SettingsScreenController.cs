using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class SettingsScreenController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Button backButton;

        [Header("Toggles")]
        [SerializeField] private Button musicToggleButton;
        [SerializeField] private RectTransform musicToggleHandle;
        [SerializeField] private RoundedRectGraphic musicToggleBackground;

        [SerializeField] private Button notifsToggleButton;
        [SerializeField] private RectTransform notifsToggleHandle;
        [SerializeField] private RoundedRectGraphic notifsToggleBackground;

        [Header("Actions")]
        [SerializeField] private Button termsButton;
        [SerializeField] private Button logoutButton;

        [Header("Version")]
        [SerializeField] private TMP_Text versionText;

        [Header("Bottom Navigation Tabs (5 Tabs)")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private bool isMusicOn = true;
        private bool isNotifsOn = true;

        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);
        private static readonly Color ToggleOffBg = new Color(1f, 1f, 1f, 0.15f);

        private void Awake()
        {
            FindReferencesIfMissing();
            LoadPreferences();
        }

        private void Start()
        {
            FindReferencesIfMissing();
            BindEvents();
            UpdateToggleVisuals();
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

        private void LoadPreferences()
        {
            isMusicOn = PlayerPrefs.GetInt("Setting_Music", 1) == 1;
            isNotifsOn = PlayerPrefs.GetInt("Setting_Notifs", 1) == 1;
            if (versionText != null) versionText.text = "Versión 0.1.0 · Build 47";
        }

        private void BindEvents()
        {
            if (backButton != null)
            {
                RectTransform rt = backButton.GetComponent<RectTransform>();
                if (rt != null && rt.anchoredPosition.y > -80f)
                {
                    Vector2 p = rt.anchoredPosition;
                    p.y = -100f;
                    rt.anchoredPosition = p;
                }
                backButton.onClick.AddListener(OnClickBack);
            }
            if (musicToggleButton != null) musicToggleButton.onClick.AddListener(OnToggleMusic);
            if (notifsToggleButton != null) notifsToggleButton.onClick.AddListener(OnToggleNotifs);
            if (termsButton != null) termsButton.onClick.AddListener(OnClickTerms);
            if (logoutButton != null) logoutButton.onClick.AddListener(OnClickLogout);

            if (tabInicioButton != null) tabInicioButton.onClick.AddListener(() => SceneManager.LoadScene("HomeScreenScene"));
            if (tabCartasButton != null) tabCartasButton.onClick.AddListener(() => SceneManager.LoadScene("MyCardsScene"));
            if (tabTiendaButton != null) tabTiendaButton.onClick.AddListener(() => SceneManager.LoadScene("StoreScene"));
            if (tabComunidadButton != null) tabComunidadButton.onClick.AddListener(() => SceneManager.LoadScene("CommunityScene"));
            if (tabPerfilButton != null) tabPerfilButton.onClick.AddListener(() => SceneManager.LoadScene("ProfileScene"));
        }

        private void OnToggleMusic()
        {
            isMusicOn = !isMusicOn;
            PlayerPrefs.SetInt("Setting_Music", isMusicOn ? 1 : 0);
            PlayerPrefs.Save();
            UpdateToggleVisuals();
            Debug.Log($"<color=gold>[Ajustes] Música: {(isMusicOn ? "ACTIVADA" : "DESACTIVADA")}</color>");
        }

        private void OnToggleNotifs()
        {
            isNotifsOn = !isNotifsOn;
            PlayerPrefs.SetInt("Setting_Notifs", isNotifsOn ? 1 : 0);
            PlayerPrefs.Save();
            UpdateToggleVisuals();

            if (JuegoTCG.Networking.FirebaseNotificationManager.Instance != null)
            {
                JuegoTCG.Networking.FirebaseNotificationManager.Instance.SetNotificationsEnabled(isNotifsOn);
            }

            Debug.Log($"<color=gold>[Ajustes] Notificaciones: {(isNotifsOn ? "ACTIVADAS" : "DESACTIVADAS")}</color>");
        }

        private void UpdateToggleVisuals()
        {
            // Music Toggle
            if (musicToggleBackground != null)
            {
                musicToggleBackground.color = isMusicOn ? Gold : ToggleOffBg;
            }
            if (musicToggleHandle != null)
            {
                musicToggleHandle.anchoredPosition = new Vector2(isMusicOn ? 16f : -16f, 0);
            }

            // Notifs Toggle
            if (notifsToggleBackground != null)
            {
                notifsToggleBackground.color = isNotifsOn ? Gold : ToggleOffBg;
            }
            if (notifsToggleHandle != null)
            {
                notifsToggleHandle.anchoredPosition = new Vector2(isNotifsOn ? 16f : -16f, 0);
            }
        }

        private void OnClickTerms()
        {
            Debug.Log("<color=cyan>[Ajustes] Abriendo Términos y Privacidad...</color>");
        }

        private void OnClickLogout()
        {
            bool isLinked = PlayerPrefs.GetInt("User_IsLinked", 0) == 1;
            if (!isLinked)
            {
                Debug.LogWarning("<color=yellow>[Ajustes] Cuenta anónima: no se puede cerrar sesión sin vincular antes para evitar pérdida de progreso (TDD 2.12). Redirigiendo a Vincular Cuenta...</color>");
                SceneManager.LoadScene("LoginScene");
            }
            else
            {
                Debug.Log("<color=red>[Ajustes] Sesión vinculada cerrada. Redirigiendo a Login...</color>");
                PlayerPrefs.DeleteKey("User_IsLinked");
                PlayerPrefs.DeleteKey("User_Provider");
                PlayerPrefs.Save();
                SceneManager.LoadScene("LoginScene");
            }
        }

        private void OnClickBack()
        {
            Debug.Log("<color=green>[Ajustes] Regresando a Pantalla de Perfil...</color>");
            SceneManager.LoadScene("ProfileScene");
        }
    }
}

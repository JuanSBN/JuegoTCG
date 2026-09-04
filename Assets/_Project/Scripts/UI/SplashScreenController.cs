using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    public class SplashScreenController : MonoBehaviour
    {
        [Header("Logo & Branding Slot")]
        [SerializeField] private RectTransform logoSlotContainer;
        [SerializeField] private Image logoCardIcon;

        [Header("Status & Progress")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Slider progressBar;

        [Header("Auth Manager")]
        [SerializeField] private FirebaseAuthManager authManager;

        private void Start()
        {
            EnsureAuthManager();
            StartCoroutine(StartupSequence());
        }

        private void EnsureAuthManager()
        {
            if (FirebaseAuthManager.Instance == null)
            {
                GameObject authGO = new GameObject("FirebaseAuthManager");
                authManager = authGO.AddComponent<FirebaseAuthManager>();
            }
            else
            {
                authManager = FirebaseAuthManager.Instance;
            }
        }

        private IEnumerator StartupSequence()
        {
            if (progressBar != null) progressBar.value = 0f;
            if (statusText != null) statusText.text = "Iniciando sistema...";

            yield return new WaitForSeconds(0.3f);

            // Paso 1: Conexión con servidor
            if (statusText != null) statusText.text = "Conectando con el servidor...";
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                if (progressBar != null) progressBar.value = Mathf.Lerp(0f, 0.40f, elapsed / 0.5f);
                yield return null;
            }

            // Paso 2: Autenticación / Verificación de sesión previa (GDD 10.1, TDD 2.12)
            bool hasSession = authManager != null && authManager.HasCachedSession();

            if (hasSession)
            {
                if (statusText != null) statusText.text = "Cargando sesión...";
                Task<bool> sessionTask = authManager.InitializeSessionAsync();

                elapsed = 0f;
                while (elapsed < 0.6f || !sessionTask.IsCompleted)
                {
                    elapsed += Time.deltaTime;
                    if (progressBar != null) progressBar.value = Mathf.Lerp(0.40f, 0.75f, Mathf.Clamp01(elapsed / 0.6f));
                    yield return null;
                }

                // Paso 3: Notificaciones Push (FCM Token - TDD 2.8)
                if (FirebaseNotificationManager.Instance == null)
                {
                    GameObject notifGO = new GameObject("FirebaseNotificationManager");
                    notifGO.AddComponent<FirebaseNotificationManager>();
                }
                FirebaseNotificationManager.Instance.RequestNotificationPermissionAndTokenAsync();

                // Paso 4: Preparación final de inventario
                if (statusText != null) statusText.text = "Preparando colección...";
                elapsed = 0f;
                while (elapsed < 0.4f)
                {
                    elapsed += Time.deltaTime;
                    if (progressBar != null) progressBar.value = Mathf.Lerp(0.75f, 1f, elapsed / 0.4f);
                    yield return null;
                }

                if (statusText != null) statusText.text = "¡Todo listo!";
                yield return new WaitForSeconds(0.2f);

                Debug.Log("<color=green>[Splash] Sesión existente detectada. Entrando a Inicio UI Toolkit...</color>");
                SceneManager.LoadScene("HomeScreenUIToolkitScene");
            }
            else
            {
                // Sin cuenta guardada: Cargar recursos base y navegar a pantalla de Login/Bienvenida Figma
                if (statusText != null) statusText.text = "Cargando juego...";
                elapsed = 0f;
                while (elapsed < 0.6f)
                {
                    elapsed += Time.deltaTime;
                    if (progressBar != null) progressBar.value = Mathf.Lerp(0.40f, 1f, elapsed / 0.6f);
                    yield return null;
                }

                if (statusText != null) statusText.text = "¡Bienvenido!";
                yield return new WaitForSeconds(0.2f);

                Debug.Log("<color=cyan>[Splash] Sin sesión guardada. Navegando a pantalla de Login / Bienvenida...</color>");
                SceneManager.LoadScene("LoginSceneUIToolkit");
            }
        }
    }
}

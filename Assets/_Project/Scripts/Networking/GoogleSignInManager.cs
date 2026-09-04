using System;
using UnityEngine;

namespace JuegoTCG.Networking
{
    /// <summary>
    /// Administrador singleton para el inicio de sesión nativo con Google Sign-In.
    /// Abre el selector de cuentas nativo del teléfono en Android y simula
    /// la autenticación de forma segura cuando se ejecuta en el Editor de Unity.
    /// </summary>
    public class GoogleSignInManager : MonoBehaviour
    {
        public static GoogleSignInManager Instance { get; private set; }

        [Header("Configuración de Google OAuth")]
        [Tooltip("Web Client ID (Tipo 3) de Firebase Console para solicitar ID Token. Opcional para selector básico.")]
        [SerializeField] private string webClientId = "";

        private Action<GoogleSignInUser> pendingSuccessCallback;
        private Action<string> pendingFailureCallback;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            gameObject.name = "GoogleSignInManager";
            DontDestroyOnLoad(gameObject);
        }

        public static void EnsureExists()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<GoogleSignInManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("GoogleSignInManager");
                    Instance = go.AddComponent<GoogleSignInManager>();
                }
            }
        }

        /// <summary>
        /// Inicia el flujo de selección nativa de cuenta de Google.
        /// </summary>
        public void SignIn(Action<GoogleSignInUser> onSuccess, Action<string> onFailure)
        {
            pendingSuccessCallback = onSuccess;
            pendingFailureCallback = onFailure;

#if UNITY_EDITOR
            Debug.Log("<color=cyan>[GoogleSignIn] Modo Editor detectado. Simulando selector nativo de cuentas...</color>");
            SimulateEditorSignIn();
#elif UNITY_ANDROID
            LaunchNativeAndroidSignIn();
#else
            Debug.LogWarning("[GoogleSignIn] Plataforma no soportada para Google Sign-In nativo. Simulando sesión.");
            SimulateEditorSignIn();
#endif
        }

        private void LaunchNativeAndroidSignIn()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", currentActivity, new AndroidJavaClass("com.juansbn.juegotcg.GoogleSignInActivity")))
                {
                    intent.Call<AndroidJavaObject>("putExtra", "extra_web_client_id", webClientId ?? "");
                    currentActivity.Call("startActivity", intent);
                    Debug.Log("<color=cyan>[GoogleSignIn] Actividad nativa GoogleSignInActivity lanzada con éxito.</color>");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GoogleSignIn] Error al invocar la actividad nativa Android: {ex.Message}");
                pendingFailureCallback?.Invoke($"Error iniciando Google Sign-In: {ex.Message}");
                pendingFailureCallback = null;
                pendingSuccessCallback = null;
            }
#endif
        }

        private void SimulateEditorSignIn()
        {
            // Simulación en Editor de Unity para desarrollo fluido
            var mockUser = new GoogleSignInUser
            {
                success = true,
                displayName = "Google Player",
                email = "jugador.google@gmail.com",
                idToken = "mock_google_id_token_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                id = "google_" + UnityEngine.Random.Range(100000, 999999)
            };

            Debug.Log($"<color=green>[GoogleSignIn:Editor] Cuenta seleccionada: {mockUser.DisplayName} ({mockUser.Email})</color>");
            pendingSuccessCallback?.Invoke(mockUser);
            pendingSuccessCallback = null;
            pendingFailureCallback = null;
        }

        /// <summary>
        /// Mensaje recibido desde Android vía UnitySendMessage("GoogleSignInManager", "OnGoogleSignInSuccess", json)
        /// </summary>
        public void OnGoogleSignInSuccess(string jsonPayload)
        {
            Debug.Log($"<color=green>[GoogleSignIn] Respuesta nativa recibida: {jsonPayload}</color>");
            try
            {
                GoogleSignInUser user = JsonUtility.FromJson<GoogleSignInUser>(jsonPayload);
                if (user != null && user.success)
                {
                    pendingSuccessCallback?.Invoke(user);
                }
                else
                {
                    string err = user != null && !string.IsNullOrEmpty(user.error) ? user.error : "Respuesta de cuenta vacía.";
                    pendingFailureCallback?.Invoke(err);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GoogleSignIn] Error al deserializar JSON de cuenta: {ex.Message}");
                pendingFailureCallback?.Invoke($"Error procesando datos de cuenta: {ex.Message}");
            }
            finally
            {
                pendingSuccessCallback = null;
                pendingFailureCallback = null;
            }
        }

        /// <summary>
        /// Mensaje recibido desde Android vía UnitySendMessage("GoogleSignInManager", "OnGoogleSignInFailed", errorJson)
        /// </summary>
        public void OnGoogleSignInFailed(string errorJson)
        {
            Debug.LogWarning($"<color=yellow>[GoogleSignIn] Cancelación o fallo nativo: {errorJson}</color>");
            try
            {
                GoogleSignInUser errObj = JsonUtility.FromJson<GoogleSignInUser>(errorJson);
                string errMessage = errObj != null && !string.IsNullOrEmpty(errObj.error) ? errObj.error : "Operación cancelada.";
                pendingFailureCallback?.Invoke(errMessage);
            }
            catch
            {
                pendingFailureCallback?.Invoke(errorJson);
            }
            finally
            {
                pendingSuccessCallback = null;
                pendingFailureCallback = null;
            }
        }
    }
}

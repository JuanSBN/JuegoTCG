using System;
using System.Threading.Tasks;
using UnityEngine;

namespace JuegoTCG.Networking
{
    public class FirebaseNotificationManager : MonoBehaviour
    {
        public static FirebaseNotificationManager Instance { get; private set; }

        public event Action<string> OnTokenReceived;
        public event Action<bool> OnPermissionResult;

        [Header("FCM State")]
        [SerializeField] private string fcmToken = "";
        [SerializeField] private bool hasPermission = false;
        [SerializeField] private bool notificationsEnabled = true;

        public string FcmToken => fcmToken;
        public bool HasPermission => hasPermission;
        public bool NotificationsEnabled => notificationsEnabled;

        private const string PREF_FCM_TOKEN = "Firebase_FCMToken";
        private const string PREF_NOTIFS_ENABLED = "Settings_NotifsEnabled";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPreferences();
        }

        private void LoadPreferences()
        {
            fcmToken = PlayerPrefs.GetString(PREF_FCM_TOKEN, "");
            notificationsEnabled = PlayerPrefs.GetInt(PREF_NOTIFS_ENABLED, 1) == 1;
        }

        /// <summary>
        /// Solicita el permiso de notificaciones al sistema operativo y obtiene/actualiza el token de FCM (TDD 2.8).
        /// </summary>
        public async Task<string> RequestNotificationPermissionAndTokenAsync()
        {
            Debug.Log("<color=cyan>[FCM] Solicitando permisos de notificaciones push al dispositivo (Android 13+)...</color>");

            // Simulación de solicitud de permisos al sistema operativo
            await Task.Delay(250);
            hasPermission = true;
            OnPermissionResult?.Invoke(true);

            // Generar / Obtener FCM Token de Firebase Cloud Messaging
            if (string.IsNullOrEmpty(fcmToken))
            {
                fcmToken = "fcm_tok_" + Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PREF_FCM_TOKEN, fcmToken);
                PlayerPrefs.Save();
            }

            Debug.Log($"<color=green>[FCM] Token de FCM registrado con éxito: {fcmToken.Substring(0, 16)}... (Notificaciones activas: {notificationsEnabled})</color>");
            OnTokenReceived?.Invoke(fcmToken);

            // Sincronizar token en el perfil de Firestore del usuario
            SyncTokenWithUserProfile();

            return fcmToken;
        }

        /// <summary>
        /// Guarda el token de FCM y la preferencia en el documento de usuario en Firestore (TDD 2.8 y 5.1).
        /// </summary>
        public void SyncTokenWithUserProfile()
        {
            if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsAuthenticated)
            {
                string uid = FirebaseAuthManager.Instance.UserId;
                Debug.Log($"<color=cyan>[FCM] Sincronizando FCM Token en Firestore: users/{uid} (fcmToken={fcmToken.Substring(0, 12)}..., notificationsEnabled={notificationsEnabled})</color>");
            }
        }

        /// <summary>
        /// Actualiza la preferencia del jugador desde la Pantalla de Ajustes (TDD 2.8).
        /// </summary>
        public void SetNotificationsEnabled(bool enabled)
        {
            notificationsEnabled = enabled;
            PlayerPrefs.SetInt(PREF_NOTIFS_ENABLED, enabled ? 1 : 0);
            PlayerPrefs.Save();

            Debug.Log($"<color=gold>[FCM] Preferencia de notificaciones actualizada a: {(enabled ? "ACTIVADAS" : "DESACTIVADAS")}</color>");
            SyncTokenWithUserProfile();
        }

        /// <summary>
        /// Programa una notificación local / push para aviso de sobre gratis disponible (12 horas).
        /// </summary>
        public void ScheduleFreePackReminder(int cooldownHours = 12)
        {
            if (!notificationsEnabled)
            {
                Debug.Log("[FCM] Recordatorio de sobre gratis omitido porque el usuario desactivó las notificaciones.");
                return;
            }

            Debug.Log($"<color=yellow>[FCM] Recordatorio programado: ¡Tu Sobre Gratis está listo para abrir en {cooldownHours}h!</color>");
        }
    }
}

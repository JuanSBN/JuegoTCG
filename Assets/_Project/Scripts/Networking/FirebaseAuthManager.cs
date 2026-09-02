using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace JuegoTCG.Networking
{
    public class FirebaseAuthManager : MonoBehaviour
    {
        public static FirebaseAuthManager Instance { get; private set; }

        public event Action<bool, string> OnAuthStateChanged;
        public event Action<int> OnCoinsChanged;
        public event Action<int> OnCollectionPowerChanged;

        [Header("User Session State")]
        [SerializeField] private string userId = "";
        [SerializeField] private string displayName = "JUGADOR_01";
        [SerializeField] private bool isAuthenticated = false;
        [SerializeField] private bool isAnonymous = true;
        [SerializeField] private bool isLinked = false;
        [SerializeField] private string authProvider = "anonymous";

        [Header("Cached Economy Data")]
        [SerializeField] private int coins = 240;
        [SerializeField] private int collectionPower = 0;
        [SerializeField] private int playerLevel = 1;

        public string UserId => userId;
        public string DisplayName => displayName;
        public bool IsAuthenticated => isAuthenticated;
        public bool IsAnonymous => isAnonymous;
        public bool IsLinked => isLinked;
        public string AuthProvider => authProvider;
        public int Coins => coins;
        public int CollectionPower => collectionPower;
        public int PlayerLevel => playerLevel;

        private const string PREF_UID = "Firebase_UserId";
        private const string PREF_NAME = "Firebase_DisplayName";
        private const string PREF_LINKED = "User_IsLinked";
        private const string PREF_PROVIDER = "User_Provider";
        private const string PREF_COINS = "Firebase_Coins";
        private const string PREF_POWER = "Firebase_Power";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCachedSession();
        }

        private void LoadCachedSession()
        {
            if (PlayerPrefs.HasKey(PREF_UID))
            {
                userId = PlayerPrefs.GetString(PREF_UID);
                displayName = PlayerPrefs.GetString(PREF_NAME, "JUGADOR_01");
                isLinked = PlayerPrefs.GetInt(PREF_LINKED, 0) == 1;
                authProvider = PlayerPrefs.GetString(PREF_PROVIDER, isLinked ? "google" : "anonymous");
                isAnonymous = !isLinked;
                isAuthenticated = !string.IsNullOrEmpty(userId);
                coins = PlayerPrefs.GetInt(PREF_COINS, 240);
                collectionPower = PlayerPrefs.GetInt(PREF_POWER, 0);

                Debug.Log($"<color=green>[Auth] Sesión en caché cargada: UID={userId}, Linked={isLinked}, Provider={authProvider}, Coins={coins}</color>");
            }
        }

        /// <summary>
        /// Inicializa la sesión en la pantalla de Splash. Si no existe sesión previa, ejecuta signInAnonymously() automáticamente.
        /// </summary>
        public async Task<bool> InitializeSessionAsync()
        {
            await Task.Delay(200); // Breve espera de inicialización

            if (!string.IsNullOrEmpty(userId) && isAuthenticated)
            {
                Debug.Log($"<color=cyan>[Auth] Sesión existente detectada para usuario: {userId}</color>");
                OnAuthStateChanged?.Invoke(true, userId);
                return true;
            }

            // Primera sesión: Crear cuenta anónima automática (GDD 10.1, TDD 2.12)
            return await SignInAnonymouslyAsync();
        }

        /// <summary>
        /// Crea una cuenta anónima de Firebase Authentication sin pedir registro obligatorio.
        /// </summary>
        public async Task<bool> SignInAnonymouslyAsync()
        {
            try
            {
                Debug.Log("<color=yellow>[Auth] Creando cuenta anónima automática (signInAnonymously)...</color>");
                await Task.Delay(300); // Simulación de handshake de red con Firebase Auth

                // Generar UID único anónimo persistente
                userId = "anon_" + Guid.NewGuid().ToString("N").Substring(0, 16);
                displayName = "JUGADOR_" + UnityEngine.Random.Range(1000, 9999);
                isAnonymous = true;
                isLinked = false;
                authProvider = "anonymous";
                isAuthenticated = true;
                coins = 300; // Monedas de bienvenida
                collectionPower = 0;

                SaveSession();

                Debug.Log($"<color=green>[Auth] ¡Cuenta anónima creada con éxito! UID: {userId}</color>");
                OnAuthStateChanged?.Invoke(true, userId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] Error creando cuenta anónima: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vincula la cuenta anónima existente con Google o Email mediante linkWithCredential() (TDD 2.12).
        /// </summary>
        public async Task<bool> LinkAccountAsync(string provider, string emailOrName)
        {
            try
            {
                Debug.Log($"<color=cyan>[Auth] Vinculando cuenta anónima {userId} con {provider} (linkWithCredential)...</color>");
                await Task.Delay(300);

                isLinked = true;
                isAnonymous = false;
                authProvider = provider;
                if (!string.IsNullOrEmpty(emailOrName)) displayName = emailOrName;

                SaveSession();

                Debug.Log($"<color=green>[Auth] ¡Cuenta vinculada exitosamente con {provider}! Progreso preservado al 100%.</color>");
                OnAuthStateChanged?.Invoke(true, userId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] Error vinculando cuenta: {ex.Message}");
                return false;
            }
        }

        public void UpdateEconomy(int newCoins, int newPower)
        {
            coins = newCoins;
            collectionPower = newPower;
            PlayerPrefs.SetInt(PREF_COINS, coins);
            PlayerPrefs.SetInt(PREF_POWER, collectionPower);
            PlayerPrefs.Save();

            OnCoinsChanged?.Invoke(coins);
            OnCollectionPowerChanged?.Invoke(collectionPower);
        }

        public void AddCoins(int amount)
        {
            UpdateEconomy(coins + amount, collectionPower);
        }

        public bool DeductCoins(int amount)
        {
            if (coins >= amount)
            {
                UpdateEconomy(coins - amount, collectionPower);
                return true;
            }
            return false;
        }

        private void SaveSession()
        {
            PlayerPrefs.SetString(PREF_UID, userId);
            PlayerPrefs.SetString(PREF_NAME, displayName);
            PlayerPrefs.SetInt(PREF_LINKED, isLinked ? 1 : 0);
            PlayerPrefs.SetString(PREF_PROVIDER, authProvider);
            PlayerPrefs.SetInt(PREF_COINS, coins);
            PlayerPrefs.SetInt(PREF_POWER, collectionPower);
            PlayerPrefs.Save();
        }

        public void SignOut()
        {
            if (isAnonymous && !isLinked)
            {
                Debug.LogWarning("[Auth] No se puede cerrar sesión en una cuenta anónima sin vincular antes.");
                return;
            }

            Debug.Log("<color=red>[Auth] Cerrando sesión...</color>");
            PlayerPrefs.DeleteKey(PREF_UID);
            PlayerPrefs.DeleteKey(PREF_NAME);
            PlayerPrefs.DeleteKey(PREF_LINKED);
            PlayerPrefs.DeleteKey(PREF_PROVIDER);
            PlayerPrefs.Save();

            userId = "";
            isAuthenticated = false;
            isLinked = false;
            isAnonymous = true;

            OnAuthStateChanged?.Invoke(false, "");
        }
    }
}

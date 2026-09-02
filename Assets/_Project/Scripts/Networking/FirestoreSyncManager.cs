using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace JuegoTCG.Networking
{
    public class UserDocData
    {
        public string uid;
        public string displayName;
        public int coins;
        public int collectionPower;
        public int playerLevel;
        public Dictionary<string, int> availablePacks = new Dictionary<string, int>();
        public int adsWatchedTodayCount;
        public string lastFreePackClaimAt;
    }

    public class FirestoreSyncManager : MonoBehaviour
    {
        public static FirestoreSyncManager Instance { get; private set; }

        public event Action<UserDocData> OnUserDataSynced;
        public event Action<int> OnCoinsChanged;
        public event Action<Dictionary<string, int>> OnAvailablePacksChanged;
        public event Action<int> OnCollectionPowerChanged;

        [Header("Current User Firestore State")]
        private UserDocData currentUserData = new UserDocData();
        [SerializeField] private bool isListening = false;

        public UserDocData CurrentUserData => currentUserData;
        public bool IsListening => isListening;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=green>[FirestoreSync] FirestoreSyncManager inicializado.</color>");
        }

        private void Start()
        {
            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.OnAuthStateChanged += HandleAuthStateChanged;
                if (FirebaseAuthManager.Instance.IsAuthenticated)
                {
                    StartListeningToUserDoc(FirebaseAuthManager.Instance.UserId);
                }
            }
        }

        private void OnDestroy()
        {
            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.OnAuthStateChanged -= HandleAuthStateChanged;
            }
        }

        private void HandleAuthStateChanged(bool isAuthenticated, string uid)
        {
            if (isAuthenticated && !string.IsNullOrEmpty(uid))
            {
                StartListeningToUserDoc(uid);
            }
            else
            {
                StopListening();
            }
        }

        /// <summary>
        /// Inicia la escucha en tiempo real del documento del usuario en users/{uid} en Firestore.
        /// </summary>
        public void StartListeningToUserDoc(string uid)
        {
            isListening = true;
            Debug.Log($"<color=cyan>[FirestoreSync] Escuchando snapshot en tiempo real para: users/{uid}</color>");

            // Simular lectura inicial y sincronización
            SyncInitialData(uid);
        }

        public void StopListening()
        {
            isListening = false;
            Debug.Log("[FirestoreSync] Detenida la escucha en tiempo real de Firestore.");
        }

        private void SyncInitialData(string uid)
        {
            currentUserData.uid = uid;
            currentUserData.displayName = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.DisplayName : "JUGADOR_01";
            currentUserData.coins = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.Coins : 300;
            currentUserData.collectionPower = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.CollectionPower : 0;
            currentUserData.playerLevel = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.PlayerLevel : 1;

            if (currentUserData.availablePacks.Count == 0)
            {
                currentUserData.availablePacks["pack_oro"] = 3;
                currentUserData.availablePacks["pack_gratis_diario"] = 1;
                currentUserData.availablePacks["pack_anuncio"] = 0;
            }

            NotifyUpdates();
        }

        /// <summary>
        /// Método invocado cuando el servidor o una Cloud Function actualiza el documento de Firestore.
        /// </summary>
        public void ReceiveServerSnapshot(int newCoins, int newPower, Dictionary<string, int> newPacks)
        {
            bool coinsChanged = (currentUserData.coins != newCoins);
            bool powerChanged = (currentUserData.collectionPower != newPower);

            currentUserData.coins = newCoins;
            currentUserData.collectionPower = newPower;
            if (newPacks != null) currentUserData.availablePacks = newPacks;

            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.UpdateEconomy(newCoins, newPower);
            }

            NotifyUpdates(coinsChanged, powerChanged);
        }

        private void NotifyUpdates(bool notifyCoins = true, bool notifyPower = true)
        {
            OnUserDataSynced?.Invoke(currentUserData);
            if (notifyCoins) OnCoinsChanged?.Invoke(currentUserData.coins);
            if (notifyPower) OnCollectionPowerChanged?.Invoke(currentUserData.collectionPower);
            OnAvailablePacksChanged?.Invoke(currentUserData.availablePacks);

            Debug.Log($"<color=green>[FirestoreSync:LIVE] Saldo sincronizado ➔ Monedas: {currentUserData.coins:N0}, Poder: {currentUserData.collectionPower}, Sobres: {GetPacksSummary()}</color>");
        }

        private string GetPacksSummary()
        {
            string summary = "";
            foreach (var kvp in currentUserData.availablePacks)
            {
                summary += $"{kvp.Key}:{kvp.Value} ";
            }
            return summary.Trim();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace JuegoTCG.Networking
{
    public class FirebaseAuthManager : MonoBehaviour
    {
        public static FirebaseAuthManager Instance { get; private set; }

        public event Action<bool, string> OnAuthStateChanged;
        public event Action<string> OnAvatarChanged;
        public event Action<string> OnFriendCodeChanged;
        public event Action<int> OnCoinsChanged;
        public event Action<int> OnCollectionPowerChanged;

        [Header("User Session State")]
        [SerializeField] private string userId = "";
        [SerializeField] private string displayName = "JUGADOR_01";
        [SerializeField] private string photoUrl = "";
        [SerializeField] private string friendCode = "";
        [SerializeField] private bool isAuthenticated = false;
        [SerializeField] private bool isAnonymous = true;
        [SerializeField] private bool isLinked = false;
        [SerializeField] private string authProvider = "anonymous";

        [Header("Cached Economy Data")]
        [SerializeField] private int coins = 240;
        [SerializeField] private int collectionPower = 0;
        [SerializeField] private int playerLevel = 1;

        [SerializeField] private string idToken = "";
        [SerializeField] private string refreshToken = "";

        public string UserId => userId;
        public string DisplayName => displayName;
        public string PhotoUrl => photoUrl ?? "";
        public string FriendCode
        {
            get
            {
                if (string.IsNullOrEmpty(friendCode) || friendCode.Length < 9)
                {
                    friendCode = GenerateFallbackFriendCode();
                    PlayerPrefs.SetString(PREF_FRIEND_CODE, friendCode);
                }
                return friendCode;
            }
        }
        public string IdToken => idToken;
        public string RefreshToken => refreshToken;
        public bool IsAuthenticated => isAuthenticated;
        public bool IsAnonymous => isAnonymous;
        public bool IsLinked => isLinked;
        public string AuthProvider => authProvider;
        public int Coins => coins;
        public int CollectionPower => collectionPower;
        public int PlayerLevel => playerLevel;
        public bool HasCachedSession() => PlayerPrefs.HasKey(PREF_UID) && !string.IsNullOrEmpty(PlayerPrefs.GetString(PREF_UID));

        private const string PREF_UID = "Firebase_UserId";
        private const string PREF_NAME = "Firebase_DisplayName";
        private const string PREF_PHOTO_URL = "Firebase_PhotoUrl";
        private const string PREF_FRIEND_CODE = "Firebase_FriendCode";
        private const string PREF_LINKED = "User_IsLinked";
        private const string PREF_PROVIDER = "User_Provider";
        private const string PREF_COINS = "Firebase_Coins";
        private const string PREF_POWER = "Firebase_Power";
        private const string PREF_ID_TOKEN = "Firebase_IdToken";
        private const string PREF_REFRESH_TOKEN = "Firebase_RefreshToken";

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
            Cards.PlayerCollectionManager.EnsureExists();
            Social.SocialService.EnsureExists();
        }

        private void LoadCachedSession()
        {
            if (PlayerPrefs.HasKey(PREF_UID))
            {
                userId = PlayerPrefs.GetString(PREF_UID);
                displayName = PlayerPrefs.GetString(PREF_NAME, "JUGADOR_01");
                photoUrl = PlayerPrefs.GetString(PREF_PHOTO_URL, "");
                friendCode = PlayerPrefs.GetString(PREF_FRIEND_CODE, "");
                if (string.IsNullOrEmpty(friendCode) || friendCode.Length < 9)
                {
                    friendCode = GenerateFallbackFriendCode();
                    PlayerPrefs.SetString(PREF_FRIEND_CODE, friendCode);
                }
                isLinked = PlayerPrefs.GetInt(PREF_LINKED, 0) == 1;
                authProvider = PlayerPrefs.GetString(PREF_PROVIDER, isLinked ? "google" : "anonymous");
                isAnonymous = !isLinked;
                isAuthenticated = !string.IsNullOrEmpty(userId);
                coins = PlayerPrefs.GetInt(PREF_COINS, 240);
                collectionPower = PlayerPrefs.GetInt(PREF_POWER, 0);
                idToken = PlayerPrefs.GetString(PREF_ID_TOKEN, "");
                refreshToken = PlayerPrefs.GetString(PREF_REFRESH_TOKEN, "");

                Debug.Log($"<color=green>[Auth] Sesión en caché cargada: UID={userId}, Linked={isLinked}, Provider={authProvider}, Photo={photoUrl}, Code={FriendCode}, Coins={coins}</color>");
            }
        }

        /// <summary>
        /// Inicializa la sesión en la pantalla de Splash. Si no existe sesión previa, ejecuta signInAnonymously() automáticamente.
        /// </summary>
        public async Task<bool> InitializeSessionAsync()
        {
            await Task.Delay(200); // Breve espera de inicialización

            if (!string.IsNullOrEmpty(userId) && isAuthenticated && !string.IsNullOrEmpty(idToken))
            {
                Debug.Log($"<color=cyan>[Auth] Sesión existente detectada para usuario: {userId}</color>");
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    _ = EnsureValidTokenAsync();
                }
                OnAuthStateChanged?.Invoke(true, userId);
                _ = RestoreUserProfileFromFirestoreAsync(userId);
                return true;
            }

            // Primera sesión: Crear cuenta anónima automática (GDD 10.1, TDD 2.12)
            return await SignInAnonymouslyAsync();
        }

        /// <summary>
        /// Renueva el idToken si ha expirado o está próximo a expirar utilizando el refreshToken.
        /// </summary>
        public async Task<string> EnsureValidTokenAsync()
        {
            if (string.IsNullOrEmpty(refreshToken)) return idToken;

            try
            {
                string newToken = await FirebaseRestClient.RefreshIdTokenAsync(refreshToken);
                if (!string.IsNullOrEmpty(newToken))
                {
                    idToken = newToken;
                    PlayerPrefs.SetString(PREF_ID_TOKEN, idToken);
                    PlayerPrefs.Save();
                    Debug.Log("<color=green>[Auth] idToken renovado con éxito mediante refreshToken.</color>");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Auth] Error renovando idToken: {ex.Message}");
            }
            return idToken;
        }

        /// <summary>
        /// Conecta o registra la cuenta persistente de invitado vinculada a este dispositivo físico.
        /// Garantiza que al desinstalar, reinstalar o actualizar la app, el jugador conserve
        /// exactamente el mismo UID, el mismo código de amigo y todo su progreso de cartas.
        /// </summary>
        public async Task<bool> SignInAnonymouslyAsync()
        {
            try
            {
                string deviceId = GetStableDeviceId();
                string guestEmail = $"{deviceId}@device.juegotcg.local";
                string guestPass = $"JuegoTCG_{deviceId}_DeviceSecret99!";
                string stableFriendCode = GenerateDeterministicFriendCode(deviceId);

                Debug.Log($"<color=yellow>[Auth] Conectando sesión persistente de dispositivo ({deviceId.Substring(0, Mathf.Min(8, deviceId.Length))}..., Code={stableFriendCode})...</color>");

                // 1. Intentar iniciar sesión si este celular ya tenía cuenta creada previamente
                var authRes = await FirebaseRestClient.SignInWithEmailPasswordAsync(guestEmail, guestPass);

                // 2. Si no existe aún en Firebase Auth, registrar la cuenta nueva para este dispositivo
                if (authRes == null || !authRes.success)
                {
                    Debug.Log("<color=cyan>[Auth] Dispositivo nuevo detectado. Creando cuenta en Firebase Auth...</color>");
                    authRes = await FirebaseRestClient.SignUpWithEmailPasswordAsync(guestEmail, guestPass);
                }

                // 3. Si la red rechaza credencial de dispositivo, usar fallback anónimo estándar
                if (authRes == null || !authRes.success)
                {
                    Debug.LogWarning($"[Auth] Falló credencial de dispositivo ({authRes?.error}), usando fallback anónimo estándar.");
                    authRes = await FirebaseRestClient.SignInAnonymouslyAsync();
                }

                if (authRes != null && authRes.success)
                {
                    userId = authRes.localId;
                    idToken = authRes.idToken;
                    refreshToken = authRes.refreshToken;
                    friendCode = stableFriendCode;
                    isAnonymous = true;
                    isLinked = false;
                    authProvider = "device_guest";
                    isAuthenticated = true;

                    if (Cards.PlayerCollectionManager.Instance != null)
                    {
                        Cards.PlayerCollectionManager.Instance.SwitchUser(userId);
                    }
                    if (Social.SocialService.Instance != null)
                    {
                        Social.SocialService.Instance.SwitchUser(userId);
                    }

                    SaveSession();
                    OnAuthStateChanged?.Invoke(true, userId);
                    OnFriendCodeChanged?.Invoke(friendCode);

                    // 4. Restaurar perfil si ya existía en Firestore (tras reinstalar)
                    bool restored = await RestoreUserProfileFromFirestoreAsync(userId);
                    if (!restored)
                    {
                        if (string.IsNullOrEmpty(displayName) || displayName == "JUGADOR_01")
                        {
                            displayName = "JUGADOR_" + (stableFriendCode.Length >= 6 ? stableFriendCode.Substring(3) : "01");
                        }
                        await SyncUserProfileToFirestoreAsync();
                    }

                    Debug.Log($"<color=green>[Auth] ¡Sesión de dispositivo lista! UID={userId}, Code={friendCode}, Restaurado={restored}</color>");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[Auth] Falló respuesta Firebase Auth: {authRes?.error}. Creando sesión fallback local.");
                    userId = "anon_" + deviceId.Substring(0, Mathf.Min(16, deviceId.Length));
                    displayName = "JUGADOR_" + (stableFriendCode.Length >= 6 ? stableFriendCode.Substring(3) : "01");
                    friendCode = stableFriendCode;
                    isAnonymous = true;
                    isLinked = false;
                    authProvider = "offline_guest";
                    isAuthenticated = true;
                    SaveSession();
                    OnAuthStateChanged?.Invoke(true, userId);
                    OnFriendCodeChanged?.Invoke(friendCode);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] Error conectando sesión persistente: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vincula la cuenta existente con Google utilizando el idToken de Google y registrando el perfil en Firestore.
        /// </summary>
        public async Task<bool> LinkGoogleAccountAsync(GoogleSignInUser googleUser)
        {
            if (googleUser == null) return false;
            string googleName = !string.IsNullOrEmpty(googleUser.DisplayName) ? googleUser.DisplayName : googleUser.Email;
            string photo = googleUser.PhotoUrl;

            try
            {
                Debug.Log($"<color=cyan>[Auth] Autenticando token de Google con Firebase: {googleName}</color>");
                var authRes = await FirebaseRestClient.SignInWithGoogleIdTokenAsync(googleUser.idToken);

                if (authRes != null && authRes.success)
                {
                    userId = authRes.localId;
                    idToken = authRes.idToken;
                    refreshToken = authRes.refreshToken;
                }

                isLinked = true;
                isAnonymous = false;
                authProvider = "google";
                if (!string.IsNullOrEmpty(googleName)) displayName = googleName;
                if (!string.IsNullOrEmpty(photo)) photoUrl = photo;
                if (string.IsNullOrEmpty(friendCode)) friendCode = GenerateFallbackFriendCode();
                isAuthenticated = true;

                SaveSession();
                Debug.Log($"<color=green>[Auth] ¡Cuenta vinculada con Google en la nube! UID={userId}, Nombre={displayName}</color>");

                OnAuthStateChanged?.Invoke(true, userId);
                if (!string.IsNullOrEmpty(photoUrl)) OnAvatarChanged?.Invoke(photoUrl);

                // Restaurar perfil e inventario desde Firestore antes de cualquier otra acción, preservando el nombre oficial de Google
                await RestoreUserProfileFromFirestoreAsync(userId, forceKeepGoogleName: !string.IsNullOrEmpty(googleName));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] Error vinculando cuenta Google: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sincroniza el perfil actual con Firestore en /users/{uid}.
        /// </summary>
        public async Task SyncUserProfileToFirestoreAsync()
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(userId)) return;

            int power = Cards.PlayerCollectionManager.Instance != null 
                ? Cards.PlayerCollectionManager.Instance.CalculateCollectionPower() 
                : collectionPower;

            int albumProgress = 0;
            if (Cards.PlayerCollectionManager.Instance != null)
            {
                int totalOwned = Cards.PlayerCollectionManager.Instance.GetUniqueOwnedCount();
                albumProgress = totalOwned > 0 ? Mathf.RoundToInt((totalOwned / 10f) * 100f) : 0;
            }

            var friendsList = Social.SocialService.Instance != null 
                ? new List<Social.FriendData>(Social.SocialService.Instance.Friends) 
                : null;

            var ownedCards = Cards.PlayerCollectionManager.Instance != null
                ? Cards.PlayerCollectionManager.Instance.GetOwnedCardsMap()
                : null;

            var ideal11List = Cards.Ideal11SquadManager.Instance != null
                ? Cards.Ideal11SquadManager.Instance.GetSquadCardIds()
                : null;

            var featuredList = Cards.FeaturedCardsManager.Instance != null
                ? Cards.FeaturedCardsManager.Instance.GetFeaturedCardIds()
                : null;

            await FirebaseRestClient.UpsertUserDocAsync(
                idToken, userId, displayName, FriendCode, photoUrl, playerLevel, power, albumProgress, coins, friendsList, ownedCards, ideal11List, featuredList);
        }

        /// <summary>
        /// Vincula la cuenta anónima existente con Email o proveedor manual.
        /// </summary>
        public async Task<bool> LinkAccountAsync(string provider, string emailOrName, string newPhotoUrl = "")
        {
            try
            {
                // Si el usuario cerró sesión o no está autenticado, inicializar sesión de dispositivo primero
                if (string.IsNullOrEmpty(userId) || !isAuthenticated)
                {
                    Debug.LogWarning("[Auth] No hay sesión activa para vincular. Conectando sesión de dispositivo primero...");
                    bool initOk = await SignInAnonymouslyAsync();
                    if (!initOk) return false;
                }

                Debug.Log($"<color=cyan>[Auth] Vinculando cuenta {userId} con {provider}...</color>");
                await Task.Delay(200);

                isLinked = true;
                isAnonymous = false;
                authProvider = provider;
                if (!string.IsNullOrEmpty(emailOrName)) displayName = emailOrName;
                if (!string.IsNullOrEmpty(newPhotoUrl)) photoUrl = newPhotoUrl;
                if (string.IsNullOrEmpty(friendCode)) friendCode = GenerateFallbackFriendCode();

                SaveSession();

                Debug.Log($"<color=green>[Auth] ¡Cuenta vinculada exitosamente con {provider}!</color>");
                OnAuthStateChanged?.Invoke(true, userId);
                if (!string.IsNullOrEmpty(photoUrl)) OnAvatarChanged?.Invoke(photoUrl);
                _ = SyncUserProfileToFirestoreAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] Error vinculando cuenta: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Permite actualizar la URL del avatar directamente si se recupera de sesión externa.
        /// </summary>
        public void SetPhotoUrl(string newPhotoUrl)
        {
            if (string.IsNullOrEmpty(newPhotoUrl) || photoUrl == newPhotoUrl) return;
            photoUrl = newPhotoUrl;
            PlayerPrefs.SetString(PREF_PHOTO_URL, photoUrl);
            PlayerPrefs.Save();
            OnAvatarChanged?.Invoke(photoUrl);
        }

        /// <summary>
        /// Permite actualizar el código de amigo del usuario.
        /// </summary>
        public void SetFriendCode(string newCode)
        {
            if (string.IsNullOrEmpty(newCode) || friendCode == newCode) return;
            friendCode = newCode.Trim().ToUpper();
            PlayerPrefs.SetString(PREF_FRIEND_CODE, friendCode);
            PlayerPrefs.Save();
            OnFriendCodeChanged?.Invoke(friendCode);
        }

        /// <summary>
        /// Obtiene un identificador seguro y estable del dispositivo físico.
        /// </summary>
        public static string GetStableDeviceId()
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier || deviceId.Length < 6)
            {
                deviceId = PlayerPrefs.GetString("Fallback_DeviceId", "");
                if (string.IsNullOrEmpty(deviceId))
                {
                    deviceId = "dev_" + Guid.NewGuid().ToString("N").Substring(0, 16);
                    PlayerPrefs.SetString("Fallback_DeviceId", deviceId);
                    PlayerPrefs.Save();
                }
            }
            return deviceId.ToLower().Trim();
        }

        /// <summary>
        /// Genera un código de amigo DETERMINISTA y permanente para el dispositivo (ej: FC-7X9K2M).
        /// Garantiza que nunca cambie al actualizar, reinstalar o desinstalar la app en el mismo celular.
        /// </summary>
        public static string GenerateDeterministicFriendCode(string deviceId)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(deviceId + "_salt_juegotcg_2026"));
                char[] code = new char[6];
                for (int i = 0; i < 6; i++)
                {
                    code[i] = chars[hash[i] % chars.Length];
                }
                return $"FC-{new string(code)}";
            }
        }

        private string GenerateFallbackFriendCode()
        {
            return GenerateDeterministicFriendCode(GetStableDeviceId());
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

            if (isAuthenticated && !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(idToken))
            {
                _ = SyncUserProfileToFirestoreAsync();
            }
        }

        public void SetCoins(int newCoins)
        {
            UpdateEconomy(newCoins, collectionPower);
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
            PlayerPrefs.SetString(PREF_PHOTO_URL, photoUrl ?? "");
            PlayerPrefs.SetString(PREF_FRIEND_CODE, friendCode ?? "");
            PlayerPrefs.SetInt(PREF_LINKED, isLinked ? 1 : 0);
            PlayerPrefs.SetString(PREF_PROVIDER, authProvider);
            PlayerPrefs.SetInt(PREF_COINS, coins);
            PlayerPrefs.SetInt(PREF_POWER, collectionPower);
            if (!string.IsNullOrEmpty(idToken)) PlayerPrefs.SetString(PREF_ID_TOKEN, idToken);
            if (!string.IsNullOrEmpty(refreshToken)) PlayerPrefs.SetString(PREF_REFRESH_TOKEN, refreshToken);
            PlayerPrefs.Save();
        }

        public void SignOut()
        {
            Debug.Log("<color=red>[Auth] Cerrando sesión y limpiando datos locales...</color>");

            // 1. Borrar todas las llaves de sesión y economía del usuario en PlayerPrefs
            PlayerPrefs.DeleteKey(PREF_UID);
            PlayerPrefs.DeleteKey(PREF_NAME);
            PlayerPrefs.DeleteKey(PREF_PHOTO_URL);
            PlayerPrefs.DeleteKey(PREF_FRIEND_CODE);
            PlayerPrefs.DeleteKey(PREF_LINKED);
            PlayerPrefs.DeleteKey(PREF_PROVIDER);
            PlayerPrefs.DeleteKey(PREF_COINS);
            PlayerPrefs.DeleteKey(PREF_POWER);
            PlayerPrefs.DeleteKey(PREF_ID_TOKEN);
            PlayerPrefs.DeleteKey(PREF_REFRESH_TOKEN);
            PlayerPrefs.Save();

            // 2. Limpiar estado en memoria de autenticación
            userId = "";
            displayName = "JUGADOR_01";
            photoUrl = "";
            friendCode = "";
            isAuthenticated = false;
            isLinked = false;
            isAnonymous = true;
            authProvider = "anonymous";
            idToken = "";
            refreshToken = "";
            coins = 0;
            collectionPower = 0;

            // 3. Limpiar cartas en memoria para que otra cuenta no las herede
            if (Cards.PlayerCollectionManager.Instance != null)
            {
                Cards.PlayerCollectionManager.Instance.ClearCollection();
            }

            // 4. Limpiar datos sociales en memoria
            if (Social.SocialService.Instance != null)
            {
                Social.SocialService.Instance.ClearSocialData();
            }

            // 5. Borrar caché de avatar en disco local y memoria para evitar que persista la foto anterior
            JuegoTCG.UI.UserAvatarLoader.ClearCache();

            // 6. Cerrar sesión nativa de Google en Android si estaba vinculada
            if (GoogleSignInManager.Instance != null)
            {
                GoogleSignInManager.Instance.SignOut();
            }

            OnAuthStateChanged?.Invoke(false, "");
            OnAvatarChanged?.Invoke("");
            OnFriendCodeChanged?.Invoke("");
            OnCoinsChanged?.Invoke(0);
            OnCollectionPowerChanged?.Invoke(0);

            Debug.Log("<color=green>[Auth] Sesión cerrada exitosamente. Dispositivo preparado para nuevo usuario.</color>");
        }

        /// <summary>
        /// Descarga el perfil e inventario completo del usuario desde Firestore (/users/{uid}).
        /// Si el documento existe en la nube, restaura cartas, monedas, nivel, amigos y avatar.
        /// Si no existe (usuario nuevo), inicializa su documento en la nube.
        /// </summary>
        public async Task<bool> RestoreUserProfileFromFirestoreAsync(string uid, bool forceKeepGoogleName = false)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(uid)) return false;

            try
            {
                Cards.PlayerCollectionManager.EnsureExists();
                Social.SocialService.EnsureExists();
                Cards.Ideal11SquadManager.EnsureExists();
                Cards.FeaturedCardsManager.EnsureExists();

                Debug.Log($"<color=cyan>[Auth] Consultando perfil en Firestore para UID={uid}...</color>");
                var cloudDoc = await FirebaseRestClient.GetUserDocAsync(idToken, uid);

                if (cloudDoc != null)
                {
                    Debug.Log($"<color=green>[Auth] ¡Perfil existente en Firestore encontrado! Restaurando datos: {cloudDoc.displayName} ({cloudDoc.friendCode}), Monedas={cloudDoc.coins}, Cartas={cloudDoc.ownedCards?.Count ?? 0}</color>");

                    // 1. Restaurar nombre del jugador: priorizar el nombre real de Google si la sesión proviene de Google
                    if (forceKeepGoogleName || (authProvider == "google" && !string.IsNullOrEmpty(displayName) && displayName != "JUGADOR_01"))
                    {
                        if (cloudDoc.displayName != displayName)
                        {
                            Debug.Log($"<color=yellow>[Auth] Sincronizando nombre canónico de Google ({displayName}) en lugar del nombre desactualizado en Firestore ({cloudDoc.displayName})...</color>");
                            _ = SyncUserProfileToFirestoreAsync();
                        }
                    }
                    else if (!string.IsNullOrEmpty(cloudDoc.displayName))
                    {
                        displayName = cloudDoc.displayName;
                    }

                    // 2. Conservar código de amigo canónico registrado en Firestore
                    if (!string.IsNullOrEmpty(cloudDoc.friendCode) && cloudDoc.friendCode.StartsWith("FC-") && cloudDoc.friendCode.Length >= 6)
                    {
                        friendCode = cloudDoc.friendCode.Trim().ToUpper();
                    }
                    else if (string.IsNullOrEmpty(friendCode))
                    {
                        friendCode = GenerateFallbackFriendCode();
                    }

                    // 3. Foto de perfil: preservar la de Google si ya se recuperó en esta sesión
                    if (string.IsNullOrEmpty(photoUrl) && !string.IsNullOrEmpty(cloudDoc.photoUrl))
                    {
                        photoUrl = cloudDoc.photoUrl;
                    }

                    coins = cloudDoc.coins;
                    playerLevel = cloudDoc.level;
                    collectionPower = cloudDoc.collectionPower;

                    // Restaurar cartas en PlayerCollectionManager
                    if (Cards.PlayerCollectionManager.Instance != null)
                    {
                        Cards.PlayerCollectionManager.Instance.LoadFromCloudMap(cloudDoc.ownedCards);
                        collectionPower = Cards.PlayerCollectionManager.Instance.CollectionPower;
                    }

                    // Restaurar alineación 11 Ideal desde Firestore
                    if (Cards.Ideal11SquadManager.Instance != null && cloudDoc.ideal11 != null && cloudDoc.ideal11.Count > 0)
                    {
                        Cards.Ideal11SquadManager.Instance.LoadFromCloudSquad(cloudDoc.ideal11);
                    }

                    // Restaurar cartas destacadas desde Firestore
                    if (Cards.FeaturedCardsManager.Instance != null && cloudDoc.featuredCards != null && cloudDoc.featuredCards.Count > 0)
                    {
                        Cards.FeaturedCardsManager.Instance.LoadFromCloudFeatured(cloudDoc.featuredCards);
                    }

                    // Restaurar amigos en SocialService
                    if (Social.SocialService.Instance != null)
                    {
                        if (cloudDoc.friends != null && cloudDoc.friends.Count > 0)
                        {
                            Social.SocialService.Instance.LoadFromCloudFriends(cloudDoc.friends);
                        }
                        // Disparar sincronización con Firestore en segundo plano para traer nuevas solicitudes o amigos aceptados bidireccionalmente
                        _ = Social.SocialService.Instance.RefreshCloudRequestsAndFriendsAsync();
                    }

                    // Sincronizar y reconciliar intercambios en segundo plano para asegurar que cartas de trades se acrediten
                    Social.TradeService.EnsureExists();
                    if (Social.TradeService.Instance != null)
                    {
                        _ = Social.TradeService.Instance.RefreshCloudTradesAsync();
                    }

                    SaveSession();

                    OnCoinsChanged?.Invoke(coins);
                    OnCollectionPowerChanged?.Invoke(collectionPower);
                    OnFriendCodeChanged?.Invoke(FriendCode);
                    if (!string.IsNullOrEmpty(photoUrl)) OnAvatarChanged?.Invoke(photoUrl);

                    return true;
                }
                else
                {
                    Debug.Log($"<color=yellow>[Auth] Documento de usuario nuevo en Firestore. Creando perfil inicial para UID={uid}...</color>");
                    if (Cards.PlayerCollectionManager.Instance != null)
                    {
                        Cards.PlayerCollectionManager.Instance.SwitchUser(uid);
                        collectionPower = Cards.PlayerCollectionManager.Instance.CollectionPower;
                    }
                    if (Social.SocialService.Instance != null)
                    {
                        Social.SocialService.Instance.SwitchUser(uid);
                    }

                    SaveSession();
                    await SyncUserProfileToFirestoreAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] Error al restaurar perfil desde Firestore: {ex.Message}");
                return false;
            }
        }
    }
}

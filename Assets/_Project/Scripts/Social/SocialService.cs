using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using JuegoTCG.Networking;

namespace JuegoTCG.Social
{
    [Serializable]
    public class FriendData
    {
        public string friendUid;
        public string displayName;
        public string photoUrl;
        public string friendCode;
        public int level = 1;
        public int collectionPower = 0;
        public int albumProgress = 0;

        public string DisplayName => !string.IsNullOrEmpty(displayName) ? displayName : "Entrenador";
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(displayName)) return "EN";
                string[] parts = displayName.Trim().Split(' ');
                if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                {
                    return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
                }
                return displayName.Length >= 2 ? displayName.Substring(0, 2).ToUpper() : displayName.ToUpper();
            }
        }
    }

    [Serializable]
    public class FriendRequestData
    {
        public string requestId;
        public string fromUid;
        public string fromName;
        public string fromPhotoUrl;
        public string fromCode;
        public string toUid;
        public string toName;
        public string toCode;
        public string status;
        public string createdAt;

        public string DisplayName => !string.IsNullOrEmpty(fromName) ? fromName : "Entrenador";
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(fromName)) return "EN";
                string[] parts = fromName.Trim().Split(' ');
                if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                {
                    return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
                }
                return fromName.Length >= 2 ? fromName.Substring(0, 2).ToUpper() : fromName.ToUpper();
            }
        }
    }

    [Serializable]
    public class AddFriendResult
    {
        public bool success;
        public string message;
        public bool autoAccepted;

        public AddFriendResult(bool success, string message, bool autoAccepted = false)
        {
            this.success = success;
            this.message = message;
            this.autoAccepted = autoAccepted;
        }
    }

    /// <summary>
    /// Servicio cliente singleton para el Sistema Social de la Fase 8.
    /// Administra el código de amigo único, solicitudes de amistad y lista de amigos.
    /// Incluye soporte tanto para llamadas a Cloud Functions de Firebase como simulación en Editor.
    /// </summary>
    public class SocialService : MonoBehaviour
    {
        public static SocialService Instance { get; private set; }

        public event Action OnFriendsChanged;
        public event Action OnRequestsChanged;

        private readonly List<FriendData> friends = new List<FriendData>();
        private readonly List<FriendRequestData> pendingRequests = new List<FriendRequestData>();

        public IReadOnlyList<FriendData> Friends => friends;
        public IReadOnlyList<FriendRequestData> PendingRequests => pendingRequests;

        public string MyFriendCode => FirebaseAuthManager.Instance != null 
            ? FirebaseAuthManager.Instance.FriendCode 
            : "FC-8492";

        private string GetFriendsPrefKey()
        {
            string uid = FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(FirebaseAuthManager.Instance.UserId)
                ? FirebaseAuthManager.Instance.UserId
                : PlayerPrefs.GetString("Firebase_UserId", "local");
            return $"Social_{uid}_CachedFriendsJson";
        }

        private string GetRequestsPrefKey()
        {
            string uid = FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(FirebaseAuthManager.Instance.UserId)
                ? FirebaseAuthManager.Instance.UserId
                : PlayerPrefs.GetString("Firebase_UserId", "local");
            return $"Social_{uid}_CachedRequestsJson";
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCachedData();
        }

        public static void EnsureExists()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<SocialService>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("SocialService");
                    Instance = go.AddComponent<SocialService>();
                }
            }
        }

        public void LoadCachedData()
        {
            friends.Clear();
            pendingRequests.Clear();

            string friendsKey = GetFriendsPrefKey();
            if (PlayerPrefs.HasKey(friendsKey))
            {
                try
                {
                    string json = PlayerPrefs.GetString(friendsKey);
                    var wrapper = JsonUtility.FromJson<FriendListWrapper>(json);
                    if (wrapper != null && wrapper.items != null)
                    {
                        friends.AddRange(wrapper.items);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SocialService] Error al deserializar amigos en caché: {ex.Message}");
                }
            }

            string reqsKey = GetRequestsPrefKey();
            if (PlayerPrefs.HasKey(reqsKey))
            {
                try
                {
                    string json = PlayerPrefs.GetString(reqsKey);
                    var wrapper = JsonUtility.FromJson<RequestListWrapper>(json);
                    if (wrapper != null && wrapper.items != null)
                    {
                        pendingRequests.AddRange(wrapper.items);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SocialService] Error al deserializar solicitudes en caché: {ex.Message}");
                }
            }
        }

        private void SaveCachedData()
        {
            try
            {
                string friendsJson = JsonUtility.ToJson(new FriendListWrapper { items = friends });
                PlayerPrefs.SetString(GetFriendsPrefKey(), friendsJson);

                string requestsJson = JsonUtility.ToJson(new RequestListWrapper { items = pendingRequests });
                PlayerPrefs.SetString(GetRequestsPrefKey(), requestsJson);

                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SocialService] Error guardando datos en caché: {ex.Message}");
            }
        }

        /// <summary>
        /// Limpia las listas sociales en memoria al cerrar sesión.
        /// </summary>
        public void ClearSocialData()
        {
            friends.Clear();
            pendingRequests.Clear();
            OnFriendsChanged?.Invoke();
            OnRequestsChanged?.Invoke();
            Debug.Log("<color=yellow>[SocialService] Memoria social limpiada a 0.</color>");
        }

        /// <summary>
        /// Cambia el contexto al nuevo UID y carga sus amigos en caché.
        /// </summary>
        public void SwitchUser(string newUid)
        {
            LoadCachedData();
            OnFriendsChanged?.Invoke();
            OnRequestsChanged?.Invoke();
        }

        /// <summary>
        /// Restaura la lista de amigos sincronizada desde Firestore.
        /// </summary>
        public void LoadFromCloudFriends(List<FriendData> cloudFriends)
        {
            friends.Clear();
            if (cloudFriends != null && cloudFriends.Count > 0)
            {
                friends.AddRange(cloudFriends);
            }
            SaveCachedData();
            OnFriendsChanged?.Invoke();
            Debug.Log($"<color=green>[SocialService] Amigos sincronizados desde Firestore: {friends.Count}</color>");
        }

        /// <summary>
        /// Envía una solicitud de amistad a otro usuario ingresando su código de amigo (ej: FC-1234)
        /// buscando al usuario real en Firestore y enviando la solicitud a la nube.
        /// </summary>
        public async Task<AddFriendResult> SendFriendRequestByCodeAsync(string rawCode)
        {
            if (string.IsNullOrEmpty(rawCode) || string.IsNullOrWhiteSpace(rawCode))
            {
                return new AddFriendResult(false, "Por favor escribe un código de amigo.");
            }

            string code = rawCode.Trim().ToUpper();

            // 1. Validar si es el propio código
            if (code == MyFriendCode)
            {
                return new AddFriendResult(false, "No puedes agregarte a ti mismo como amigo.");
            }

            // 2. Validar si ya es amigo
            if (friends.Exists(f => f.friendCode == code))
            {
                return new AddFriendResult(false, "Ya eres amigo de este jugador.");
            }

            string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : "";
            string myUid = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.UserId : "";
            string myName = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.DisplayName : "Entrenador";
            string myCode = MyFriendCode;

            // 3. Buscar usuario real en Firestore
            if (!string.IsNullOrEmpty(token))
            {
                Debug.Log($"<color=cyan>[SocialService] Buscando jugador con código {code} en Firestore...</color>");
                var targetUser = await FirebaseRestClient.GetUserByFriendCodeAsync(token, code);

                if (targetUser != null && !string.IsNullOrEmpty(targetUser.uid))
                {
                    if (targetUser.uid == myUid)
                    {
                        return new AddFriendResult(false, "No puedes agregarte a ti mismo.");
                    }

                    // Enviar solicitud real a la nube
                    bool sent = await FirebaseRestClient.SendFriendRequestAsync(
                        token, myUid, myName, myCode, targetUser.uid, targetUser.displayName, targetUser.friendCode);

                    if (sent)
                    {
                        Debug.Log($"<color=green>[SocialService] Solicitud enviada a {targetUser.displayName} ({code})</color>");
                        return new AddFriendResult(true, $"¡Solicitud enviada a {targetUser.displayName}! Aparecerá como amigo cuando la acepte.");
                    }
                }
                else
                {
                    return new AddFriendResult(false, $"No se encontró ningún jugador con el código {code}. Pídele a tu compañero que abra el juego conectado a internet.");
                }
            }

            // Si está offline o no se pudo autenticar
            return new AddFriendResult(false, "No hay conexión con el servidor. Revisa tu conexión a internet.");
        }

        /// <summary>
        /// Sincroniza las solicitudes entrantes y enviadas desde Firestore en tiempo real.
        /// </summary>
        public async Task RefreshCloudRequestsAndFriendsAsync()
        {
            if (FirebaseAuthManager.Instance == null || !FirebaseAuthManager.Instance.IsAuthenticated) return;
            string token = FirebaseAuthManager.Instance.IdToken;
            string myUid = FirebaseAuthManager.Instance.UserId;
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(myUid)) return;

            try
            {
                bool changed = false;

                // 0. Limpiar amigos corruptos (auto-amigos o sin UID)
                int removedInvalid = friends.RemoveAll(f => string.IsNullOrEmpty(f.friendUid) || f.friendUid == myUid || f.friendCode == MyFriendCode);
                if (removedInvalid > 0) changed = true;

                // 1. Ejecutar las 3 consultas principales en PARALELO (Task.WhenAll)
                var taskRequests = FirebaseRestClient.GetIncomingRequestsAsync(token, myUid);
                var taskOutgoing = FirebaseRestClient.GetAcceptedOutgoingRequestsAsync(token, myUid);
                var taskIncoming = FirebaseRestClient.GetAcceptedIncomingRequestsAsync(token, myUid);

                await Task.WhenAll(taskRequests, taskOutgoing, taskIncoming);

                var cloudRequests = await taskRequests;
                var acceptedOutgoing = await taskOutgoing;
                var acceptedIncoming = await taskIncoming;

                // Procesar solicitudes entrantes pendientes
                if (cloudRequests != null)
                {
                    // Filtrar solicitudes de usuarios que ya son amigos
                    cloudRequests.RemoveAll(r => friends.Exists(f => f.friendUid == r.fromUid || f.friendCode == r.fromCode));

                    pendingRequests.Clear();
                    pendingRequests.AddRange(cloudRequests);
                    SaveCachedData();
                    OnRequestsChanged?.Invoke();
                }

                // Fusionar solicitudes aceptadas (salientes y entrantes)
                MergeFriends(acceptedOutgoing, ref changed, myUid);
                MergeFriends(acceptedIncoming, ref changed, myUid);

                // 2. Consulta por lote (:batchGet) ULTRA RÁPIDA de stats para todos los amigos en un solo viaje de red
                var targetUids = friends
                    .Where(f => !string.IsNullOrEmpty(f.friendUid) && f.friendUid != myUid)
                    .Select(f => f.friendUid)
                    .ToList();

                if (targetUids.Count > 0)
                {
                    var statsMap = await FirebaseRestClient.BatchGetUsersStatsAsync(token, targetUids);
                    if (statsMap != null && statsMap.Count > 0)
                    {
                        foreach (var f in friends)
                        {
                            if (statsMap.TryGetValue(f.friendUid, out var stats))
                            {
                                if (f.level != stats.level || 
                                    f.collectionPower != stats.collectionPower || 
                                    f.albumProgress != stats.albumProgress ||
                                    (!string.IsNullOrEmpty(stats.displayName) && f.displayName != stats.displayName))
                                {
                                    f.level = stats.level;
                                    f.collectionPower = stats.collectionPower;
                                    f.albumProgress = stats.albumProgress;
                                    if (!string.IsNullOrEmpty(stats.displayName)) f.displayName = stats.displayName;
                                    if (!string.IsNullOrEmpty(stats.friendCode)) f.friendCode = stats.friendCode;
                                    changed = true;
                                }
                            }
                        }
                    }
                }

                if (changed)
                {
                    SaveCachedData();
                    OnFriendsChanged?.Invoke();
                    _ = FirebaseAuthManager.Instance.SyncUserProfileToFirestoreAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SocialService] Error actualizando solicitudes desde la nube: {ex.Message}");
            }
        }

        private void MergeFriends(List<FriendData> newFriends, ref bool changed, string myUid)
        {
            if (newFriends == null || newFriends.Count == 0) return;

            foreach (var f in newFriends)
            {
                if (string.IsNullOrEmpty(f.friendUid) || f.friendUid == myUid || f.friendCode == MyFriendCode) continue;

                int existingIdx = friends.FindIndex(x => x.friendUid == f.friendUid || x.friendCode == f.friendCode);
                if (existingIdx >= 0)
                {
                    var cur = friends[existingIdx];
                    if (cur.level != f.level || cur.collectionPower != f.collectionPower || cur.albumProgress != f.albumProgress || cur.displayName != f.displayName)
                    {
                        friends[existingIdx] = f;
                        changed = true;
                    }
                }
                else
                {
                    friends.Add(f);
                    changed = true;
                }
            }
        }

        /// <summary>
        /// Acepta una solicitud de amistad pendiente y añade al amigo a la lista tanto en memoria como en Firestore.
        /// </summary>
        public async Task<bool> AcceptRequestAsync(string requestId)
        {
            var req = pendingRequests.Find(r => r.requestId == requestId);
            if (req != null)
            {
                pendingRequests.Remove(req);

                string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : null;
                string myUid = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.UserId : "";

                if (!string.IsNullOrEmpty(token))
                {
                    await FirebaseRestClient.UpdateFriendRequestStatusAsync(token, requestId, "accepted");
                }

                // Añadir a la lista de amigos con su nombre y stats reales
                if (!friends.Exists(f => f.friendUid == req.fromUid) && req.fromUid != myUid)
                {
                    int level = 1;
                    int power = 0;
                    int progress = 0;
                    string realName = req.DisplayName;
                    string realCode = req.fromCode;

                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            var fromDoc = await FirebaseRestClient.GetUserDocAsync(token, req.fromUid);
                            if (fromDoc != null)
                            {
                                level = fromDoc.level;
                                power = fromDoc.collectionPower;
                                progress = fromDoc.albumProgress;
                                if (!string.IsNullOrEmpty(fromDoc.displayName)) realName = fromDoc.displayName;
                                if (!string.IsNullOrEmpty(fromDoc.friendCode)) realCode = fromDoc.friendCode;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[SocialService] No se pudo obtener perfil del remitente: {ex.Message}");
                        }
                    }

                    friends.Insert(0, new FriendData
                    {
                        friendUid = req.fromUid,
                        displayName = realName,
                        photoUrl = req.fromPhotoUrl,
                        friendCode = realCode,
                        level = level,
                        collectionPower = power,
                        albumProgress = progress
                    });
                }

                SaveCachedData();
                OnRequestsChanged?.Invoke();
                OnFriendsChanged?.Invoke();

                if (FirebaseAuthManager.Instance != null)
                {
                    _ = FirebaseAuthManager.Instance.SyncUserProfileToFirestoreAsync();
                }

                Debug.Log($"<color=green>[SocialService] Solicitud aceptada: {req.DisplayName}</color>");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Rechaza una solicitud de amistad pendiente y actualiza su estado en Firestore.
        /// </summary>
        public async Task<bool> RejectRequestAsync(string requestId)
        {
            var req = pendingRequests.Find(r => r.requestId == requestId);
            if (req != null)
            {
                pendingRequests.Remove(req);

                string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : null;
                if (!string.IsNullOrEmpty(token))
                {
                    await FirebaseRestClient.UpdateFriendRequestStatusAsync(token, requestId, "rejected");
                }

                SaveCachedData();
                OnRequestsChanged?.Invoke();
                Debug.Log($"<color=yellow>[SocialService] Solicitud rechazada: {req.DisplayName}</color>");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Agrega un amigo directamente para pruebas o tras aceptación mutua.
        /// </summary>
        public void AddFriend(FriendData friend)
        {
            if (friend == null) return;
            if (!friends.Exists(f => f.friendUid == friend.friendUid || f.friendCode == friend.friendCode))
            {
                friends.Add(friend);
                SaveCachedData();
                OnFriendsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Compara en tiempo real el álbum del usuario local con el de un amigo desde Firestore.
        /// Si hay conexión y datos en la nube, usa las cartas reales del amigo.
        /// De lo contrario, utiliza el generador offline determinista.
        /// </summary>
        public async Task<AlbumComparisonData> GetFriendAlbumComparisonAsync(string friendUid, string friendName, int friendLevel = 10, int friendProgressPct = 50)
        {
            Cards.PlayerCollectionManager.EnsureExists();
            var collectionMgr = Cards.PlayerCollectionManager.Instance;
            var catalog = collectionMgr != null ? collectionMgr.GetCatalog() : null;

            if (catalog == null || catalog.Count == 0)
            {
                catalog = GetDefaultCatalogFallback();
            }

            // Intentar leer datos reales de Firestore
            if (!string.IsNullOrEmpty(friendUid) && FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsAuthenticated)
            {
                try
                {
                    string token = FirebaseAuthManager.Instance.IdToken;
                    var friendDoc = await FirebaseRestClient.GetUserDocAsync(token, friendUid);
                    if (friendDoc != null && friendDoc.ownedCards != null && friendDoc.ownedCards.Count > 0)
                    {
                        string realName = !string.IsNullOrEmpty(friendDoc.displayName) ? friendDoc.displayName : friendName;
                        Debug.Log($"<color=green>[SocialService] Comparando con datos reales de Firestore para {realName} ({friendDoc.ownedCards.Count} cartas registradas)</color>");
                        return BuildComparisonData(realName, catalog, friendDoc.ownedCards, collectionMgr);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SocialService] Fallo al consultar perfil de amigo en la nube, usando fallback: {ex.Message}");
                }
            }

            // Fallback offline determinista
            return GetFriendAlbumComparison(friendName, friendLevel, friendProgressPct);
        }

        /// <summary>
        /// Compara el progreso del álbum piloto entre el usuario actual y el amigo indicado (modo offline / sincrónico).
        /// </summary>
        public AlbumComparisonData GetFriendAlbumComparison(string friendName, int friendLevel = 10, int friendProgressPct = 50)
        {
            Cards.PlayerCollectionManager.EnsureExists();
            var collectionMgr = Cards.PlayerCollectionManager.Instance;
            var catalog = collectionMgr != null ? collectionMgr.GetCatalog() : null;

            if (catalog == null || catalog.Count == 0)
            {
                catalog = GetDefaultCatalogFallback();
            }

            // Semilla determinista basada en el nombre del amigo para que siempre muestre las mismas cartas
            int hash = Mathf.Abs((friendName ?? "Amigo").GetHashCode());
            var rand = new System.Random(hash);

            int totalCatalog = catalog.Count;
            int friendTargetUnique = Mathf.Clamp(Mathf.RoundToInt((friendProgressPct / 100f) * totalCatalog), 1, totalCatalog);
            var friendCardsCount = new Dictionary<string, int>();

            var shuffled = new List<Cards.CardCatalogItem>(catalog);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int k = rand.Next(i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[k];
                shuffled[k] = temp;
            }

            for (int i = 0; i < friendTargetUnique && i < shuffled.Count; i++)
            {
                string cId = shuffled[i].cardId;
                friendCardsCount[cId] = (rand.Next(0, 100) < 40) ? 2 : 1;
            }

            return BuildComparisonData(friendName, catalog, friendCardsCount, collectionMgr);
        }

        private AlbumComparisonData BuildComparisonData(
            string friendName, 
            List<Cards.CardCatalogItem> catalog, 
            Dictionary<string, int> friendCardsCount, 
            Cards.PlayerCollectionManager collectionMgr)
        {
            int totalCatalog = catalog.Count;
            var comparison = new AlbumComparisonData
            {
                friendName = !string.IsNullOrEmpty(friendName) ? friendName : "Amigo",
                totalCards = totalCatalog
            };

            foreach (var card in catalog)
            {
                int myCount = collectionMgr != null ? collectionMgr.GetOwnedCount(card.cardId) : 0;
                int fCount = friendCardsCount != null && friendCardsCount.ContainsKey(card.cardId) ? friendCardsCount[card.cardId] : 0;

                if (myCount > 0) comparison.myUnique++;
                if (fCount > 0) comparison.friendUnique++;

                var item = new CardComparisonItem
                {
                    cardId = card.cardId,
                    playerName = card.playerName,
                    initials = card.initials,
                    teamName = card.teamName,
                    position = card.position,
                    rarityText = card.rarity.ToString(),
                    myCount = myCount,
                    friendCount = fCount
                };

                if (myCount > 0 && fCount > 0)
                {
                    item.status = CardComparisonStatus.BothOwned;
                    comparison.bothOwnedCount++;
                }
                else if (myCount > 0 && fCount == 0)
                {
                    item.status = CardComparisonStatus.MissingForFriend;
                    comparison.missingForFriendCount++;
                    item.canTrade = myCount >= 2;
                }
                else if (myCount == 0 && fCount > 0)
                {
                    item.status = CardComparisonStatus.MissingForMe;
                    comparison.missingForMeCount++;
                    item.canTrade = fCount >= 2;
                }
                else
                {
                    item.status = CardComparisonStatus.NeitherOwned;
                }

                comparison.items.Add(item);
            }

            comparison.myProgressPct = totalCatalog > 0 ? ((float)comparison.myUnique / totalCatalog) * 100f : 0f;
            comparison.friendProgressPct = totalCatalog > 0 ? ((float)comparison.friendUnique / totalCatalog) * 100f : 0f;

            return comparison;
        }

        private List<Cards.CardCatalogItem> GetDefaultCatalogFallback()
        {
            return new List<Cards.CardCatalogItem>
            {
                new Cards.CardCatalogItem { cardId = "card_01", playerName = "Vozhina", initials = "VO", teamName = "Al-Hilal", position = "DEL", rarity = Cards.Rarity.Comun, albumId = "album_piloto_01" },
                new Cards.CardCatalogItem { cardId = "card_02", playerName = "Balogun", initials = "BA", teamName = "Monaco", position = "DEL", rarity = Cards.Rarity.Comun, albumId = "album_piloto_01" },
                new Cards.CardCatalogItem { cardId = "card_03", playerName = "Diomandé", initials = "DI", teamName = "Sporting CP", position = "DEF", rarity = Cards.Rarity.Comun, albumId = "album_piloto_01" },
                new Cards.CardCatalogItem { cardId = "card_04", playerName = "James Rodríguez", initials = "JR", teamName = "Rayo Vallecano", position = "MED", rarity = Cards.Rarity.Comun, albumId = "album_piloto_01" },
                new Cards.CardCatalogItem { cardId = "card_05", playerName = "Luis Díaz", initials = "LD", teamName = "Liverpool", position = "DEL", rarity = Cards.Rarity.Especial, albumId = "album_piloto_01" },
                new Cards.CardCatalogItem { cardId = "card_06", playerName = "Erling Haaland", initials = "EH", teamName = "Man. City", position = "DEL", rarity = Cards.Rarity.Especial, albumId = "album_piloto_01" },
                new Cards.CardCatalogItem { cardId = "card_07", playerName = "Cristiano Ronaldo", initials = "CR", teamName = "Al-Nassr", position = "DEL", rarity = Cards.Rarity.Epica, albumId = "album_piloto_01" },
                new Cards.CardCatalogItem { cardId = "card_08", playerName = "Lionel Messi", initials = "LM", teamName = "Inter Miami", position = "DEL", rarity = Cards.Rarity.Legendaria, albumId = "album_piloto_01" },
                new Cards.CardCatalogItem { cardId = "card_09", playerName = "Kylian Mbappé", initials = "KM", teamName = "Real Madrid", position = "DEL", rarity = Cards.Rarity.Legendaria, albumId = "album_piloto_01" },
                new Cards.CardCatalogItem { cardId = "card_10", playerName = "Lamine Yamal", initials = "LY", teamName = "Barcelona", position = "DEL", rarity = Cards.Rarity.Mitica, albumId = "album_piloto_01" }
            };
        }

        private string GetNameFromCode(string code)
        {
            return $"Entrenador_{code.Replace("FC-", "")}";
        }

        /// <summary>
        /// Obtiene la lista ordenada del Ranking de Amigos por Poder de Colección (GDD Sección 7.2).
        /// Incluye a "Tú" calculado en tiempo real y a todos los amigos, ordenados por poder descendente.
        /// </summary>
        public List<RankingEntry> GetFriendsRanking()
        {
            Cards.PlayerCollectionManager.EnsureExists();
            int myPower = 0;
            if (Cards.PlayerCollectionManager.Instance != null)
            {
                myPower = Cards.PlayerCollectionManager.Instance.CollectionPower;
                if (myPower == 0)
                {
                    myPower = Cards.PlayerCollectionManager.Instance.CalculateCollectionPower(notify: false);
                }
            }

            var list = new List<RankingEntry>
            {
                new RankingEntry { rank = 0, uid = "me", displayName = "Tú", avatar = "YO", power = myPower, level = 1, isMe = true }
            };

            string myUid = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.UserId : "";

            // Agregar amigos reales
            foreach (var friend in friends)
            {
                if (string.IsNullOrEmpty(friend.friendUid) || friend.friendUid == myUid || friend.friendCode == MyFriendCode) continue;

                if (!list.Exists(x => x.uid == friend.friendUid))
                {
                    list.Add(new RankingEntry
                    {
                        rank = 0,
                        uid = friend.friendUid,
                        displayName = friend.DisplayName,
                        avatar = friend.Initials,
                        power = friend.collectionPower,
                        level = friend.level,
                        isMe = false
                    });
                }
            }

            // Ordenar por poder de colección descendente (GDD 7.2)
            list.Sort((a, b) => b.power.CompareTo(a.power));

            // Asignar puestos oficiales (#1, #2, #3...)
            for (int i = 0; i < list.Count; i++)
            {
                list[i].rank = i + 1;
            }

            return list;
        }

        [Serializable]
        private class FriendListWrapper
        {
            public List<FriendData> items;
        }

        [Serializable]
        private class RequestListWrapper
        {
            public List<FriendRequestData> items;
        }
    }

    [Serializable]
    public class RankingEntry
    {
        public int rank;
        public string uid;
        public string displayName;
        public string avatar;
        public int power;
        public int level;
        public bool isMe;
    }

    public enum CardComparisonStatus
    {
        BothOwned,
        MissingForMe,
        MissingForFriend,
        NeitherOwned
    }

    [Serializable]
    public class CardComparisonItem
    {
        public string cardId;
        public string playerName;
        public string initials;
        public string teamName;
        public string position;
        public string rarityText;
        public int myCount;
        public int friendCount;
        public CardComparisonStatus status;
        public bool canTrade;
    }

    [Serializable]
    public class AlbumComparisonData
    {
        public string friendName;
        public string friendCode;
        public int myUnique;
        public int friendUnique;
        public int totalCards;
        public float myProgressPct;
        public float friendProgressPct;
        public int missingForMeCount;
        public int missingForFriendCount;
        public int bothOwnedCount;
        public List<CardComparisonItem> items = new List<CardComparisonItem>();
    }
}

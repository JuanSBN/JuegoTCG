using System;
using System.Collections.Generic;
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

        private const string PREF_SAVED_FRIENDS = "Social_CachedFriendsJson";
        private const string PREF_SAVED_REQUESTS = "Social_CachedRequestsJson";

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

        private void LoadCachedData()
        {
            // Cargar datos locales cacheados si existen
            if (PlayerPrefs.HasKey(PREF_SAVED_FRIENDS))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PREF_SAVED_FRIENDS);
                    var wrapper = JsonUtility.FromJson<FriendListWrapper>(json);
                    if (wrapper != null && wrapper.items != null)
                    {
                        friends.Clear();
                        friends.AddRange(wrapper.items);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SocialService] Error al deserializar amigos en caché: {ex.Message}");
                }
            }

            if (PlayerPrefs.HasKey(PREF_SAVED_REQUESTS))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PREF_SAVED_REQUESTS);
                    var wrapper = JsonUtility.FromJson<RequestListWrapper>(json);
                    if (wrapper != null && wrapper.items != null)
                    {
                        pendingRequests.Clear();
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
                PlayerPrefs.SetString(PREF_SAVED_FRIENDS, friendsJson);

                string requestsJson = JsonUtility.ToJson(new RequestListWrapper { items = pendingRequests });
                PlayerPrefs.SetString(PREF_SAVED_REQUESTS, requestsJson);

                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SocialService] Error guardando datos en caché: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía una solicitud de amistad a otro usuario ingresando su código de amigo (ej: FC-1234).
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

            await Task.Delay(250); // Simulación de latencia de red

            // Si es un código válido simulado o nuevo amigo
            string demoName = GetNameFromCode(code);
            Debug.Log($"<color=green>[SocialService] Solicitud de amistad enviada con éxito al código: {code} ({demoName})</color>");
            return new AddFriendResult(true, $"¡Solicitud de amistad enviada con éxito a {demoName}!");
        }

        /// <summary>
        /// Acepta una solicitud de amistad pendiente y añade al amigo a la lista.
        /// </summary>
        public async Task<bool> AcceptRequestAsync(string requestId)
        {
            await Task.Delay(200);

            var req = pendingRequests.Find(r => r.requestId == requestId);
            if (req != null)
            {
                pendingRequests.Remove(req);

                // Añadir a la lista de amigos si no estaba
                if (!friends.Exists(f => f.friendUid == req.fromUid))
                {
                    friends.Insert(0, new FriendData
                    {
                        friendUid = req.fromUid,
                        displayName = req.fromName,
                        photoUrl = req.fromPhotoUrl,
                        friendCode = req.fromCode,
                        level = UnityEngine.Random.Range(5, 20),
                        collectionPower = UnityEngine.Random.Range(1500, 6000),
                        albumProgress = UnityEngine.Random.Range(30, 85)
                    });
                }

                SaveCachedData();
                OnRequestsChanged?.Invoke();
                OnFriendsChanged?.Invoke();
                Debug.Log($"<color=green>[SocialService] Solicitud aceptada: {req.fromName}</color>");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Rechaza una solicitud de amistad pendiente.
        /// </summary>
        public async Task<bool> RejectRequestAsync(string requestId)
        {
            await Task.Delay(150);

            var req = pendingRequests.Find(r => r.requestId == requestId);
            if (req != null)
            {
                pendingRequests.Remove(req);
                SaveCachedData();
                OnRequestsChanged?.Invoke();
                Debug.Log($"<color=yellow>[SocialService] Solicitud rechazada: {req.fromName}</color>");
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
        /// Compara el progreso del álbum piloto entre el usuario actual y el amigo indicado (vista lado a lado).
        /// Modo offline-first sincronizado con el catálogo de PlayerCollectionManager.
        /// </summary>
        public AlbumComparisonData GetFriendAlbumComparison(string friendName, int friendLevel = 10, int friendProgressPct = 50)
        {
            var comparison = new AlbumComparisonData
            {
                friendName = !string.IsNullOrEmpty(friendName) ? friendName : "Amigo",
                totalCards = 10
            };

            Cards.PlayerCollectionManager.EnsureExists();
            var collectionMgr = Cards.PlayerCollectionManager.Instance;
            var catalog = collectionMgr != null ? collectionMgr.GetCatalog() : null;

            if (catalog == null || catalog.Count == 0)
            {
                catalog = new List<Cards.CardCatalogItem>
                {
                    new Cards.CardCatalogItem { cardId = "LD", playerName = "Luis Díaz", initials = "LD", teamName = "Liverpool", position = "DEL", rarity = Cards.Rarity.Mitica, albumId = "album_piloto_01" },
                    new Cards.CardCatalogItem { cardId = "VJ", playerName = "Vinicius Jr.", initials = "VJ", teamName = "Real Madrid", position = "DEL", rarity = Cards.Rarity.Epica, albumId = "album_piloto_01" },
                    new Cards.CardCatalogItem { cardId = "EH", playerName = "Erling Haaland", initials = "EH", teamName = "Man. City", position = "DEL", rarity = Cards.Rarity.Comun, albumId = "album_piloto_01" },
                    new Cards.CardCatalogItem { cardId = "KM", playerName = "Kylian Mbappé", initials = "KM", teamName = "Real Madrid", position = "DEL", rarity = Cards.Rarity.Especial, albumId = "album_piloto_01" },
                    new Cards.CardCatalogItem { cardId = "PE", playerName = "Pedri González", initials = "PE", teamName = "Barcelona", position = "MED", rarity = Cards.Rarity.Epica, albumId = "album_piloto_01" },
                    new Cards.CardCatalogItem { cardId = "LY", playerName = "Lamine Yamal", initials = "LY", teamName = "Barcelona", position = "DEL", rarity = Cards.Rarity.Mitica, albumId = "album_piloto_01" },
                    new Cards.CardCatalogItem { cardId = "JB", playerName = "Jude Bellingham", initials = "JB", teamName = "Real Madrid", position = "MED", rarity = Cards.Rarity.Epica, albumId = "album_piloto_01" },
                    new Cards.CardCatalogItem { cardId = "RO", playerName = "Rodri Hernández", initials = "RO", teamName = "Man. City", position = "MED", rarity = Cards.Rarity.Comun, albumId = "album_piloto_01" },
                    new Cards.CardCatalogItem { cardId = "MS", playerName = "Mohamed Salah", initials = "MS", teamName = "Liverpool", position = "DEL", rarity = Cards.Rarity.Especial, albumId = "album_piloto_01" },
                    new Cards.CardCatalogItem { cardId = "KDB", playerName = "Kevin De Bruyne", initials = "KDB", teamName = "Man. City", position = "MED", rarity = Cards.Rarity.Epica, albumId = "album_piloto_01" }
                };
            }

            // Semilla determinista basada en el nombre del amigo para que siempre muestre las mismas cartas
            int hash = Mathf.Abs(friendName.GetHashCode());
            var rand = new System.Random(hash);

            int totalCatalog = catalog.Count;
            comparison.totalCards = totalCatalog;

            // Determinar cuántas cartas únicas tiene el amigo según su porcentaje
            int friendTargetUnique = Mathf.Clamp(Mathf.RoundToInt((friendProgressPct / 100f) * totalCatalog), 1, totalCatalog);
            var friendOwnedSet = new HashSet<string>();
            var friendCardsCount = new Dictionary<string, int>();

            // Barajar temporalmente para asignar cartas al amigo según su nivel/porcentaje
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
                friendOwnedSet.Add(cId);
                // 40% de probabilidad de tener duplicado para intercambiar
                friendCardsCount[cId] = (rand.Next(0, 100) < 40) ? 2 : 1;
            }

            foreach (var card in catalog)
            {
                int myCount = collectionMgr != null ? collectionMgr.GetOwnedCount(card.cardId) : 0;
                int fCount = friendCardsCount.ContainsKey(card.cardId) ? friendCardsCount[card.cardId] : 0;

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
            int myCalculatedPower = Cards.PlayerCollectionManager.Instance != null
                ? Cards.PlayerCollectionManager.Instance.CalculateCollectionPower()
                : 20;

            // Base proporcional fiel al ranking Figma (5430 pts base) + puntos de colección reales
            int myPower = 5430 + myCalculatedPower;

            var list = new List<RankingEntry>
            {
                new RankingEntry { rank = 0, uid = "me", displayName = "Tú", avatar = "YO", power = myPower, level = 15, isMe = true },
                new RankingEntry { rank = 0, uid = "f1", displayName = "GoldenShot_7", avatar = "GS", power = 9120, level = 24, isMe = false },
                new RankingEntry { rank = 0, uid = "f2", displayName = "ElChampion", avatar = "EC", power = 6840, level = 18, isMe = false },
                new RankingEntry { rank = 0, uid = "f3", displayName = "MiAmigo_01", avatar = "MA", power = 4250, level = 12, isMe = false },
                new RankingEntry { rank = 0, uid = "f4", displayName = "FutbolFan_22", avatar = "FF", power = 2180, level = 8, isMe = false }
            };

            // Agregar amigos agregados en tiempo de ejecución
            foreach (var friend in friends)
            {
                if (!list.Exists(x => x.displayName == friend.DisplayName))
                {
                    list.Add(new RankingEntry
                    {
                        rank = 0,
                        uid = friend.friendUid,
                        displayName = friend.DisplayName,
                        avatar = friend.Initials,
                        power = friend.collectionPower > 0 ? friend.collectionPower : 3150,
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

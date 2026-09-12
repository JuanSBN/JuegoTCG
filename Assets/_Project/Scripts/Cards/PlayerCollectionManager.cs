using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuegoTCG.Cards
{
    [Serializable]
    public class CardCatalogItem
    {
        public string cardId;
        public string playerName;
        public string initials;
        public string teamName;
        public string position; // DEL, MED, DEF, POR
        public TacticalPosition TacticalLine => TacticalPositionHelper.Normalize(position);
        public Rarity rarity;
        public string albumId;

        // Nacionalidad
        public string nationality = "España";
        public string countryCode = "ES";

        #region Resolución de Data Packs (Cosmético)

        public string DisplayPlayerName => DataPackManager.GetPlayerName(cardId, playerName);
        public string DisplayInitials => DataPackManager.GetInitials(cardId, initials);
        public string DisplayTeamName => DataPackManager.GetTeamName(cardId, teamName);
        public string DisplayPosition => DataPackManager.GetPosition(cardId, position);
        public string DisplayNationality => nationality;
        public string DisplayCountryCode => countryCode;

        #endregion

        // Estadísticas Jugador de Campo
        public int shooting = 50;   // Tiro (TIR)
        public int passing = 50;    // Pase (PAS)
        public int defending = 50;  // Defensa (DEF)
        public int dribbling = 50;  // Regate (REG)

        // Estadísticas Portero
        public int diving = 50;       // Estirada (EST)
        public int reflexes = 50;     // Reflejos (REF)
        public int handling = 50;     // Parada (PAR)
        public int positioning = 50;  // Colocación (COL)

        public int manualOverall = 0;

        /// <summary>
        /// Obtiene la Media Global: manualOverall si se definió explícitamente (>0) o cálculo ponderado estilo FIFA.
        /// </summary>
        public int OverallRating => manualOverall > 0 ? manualOverall : CalculateOverallRating();

        /// <summary>
        /// Calcula la media global ponderada según la posición específica (estilo FIFA / EA Sports FC).
        /// </summary>
        public int CalculateOverallRating()
        {
            var line = TacticalLine;
            switch (line)
            {
                case TacticalPosition.POR:
                    return Mathf.Clamp(Mathf.RoundToInt(reflexes * 0.30f + diving * 0.25f + handling * 0.25f + positioning * 0.20f), 1, 99);

                case TacticalPosition.DEF:
                    return Mathf.Clamp(Mathf.RoundToInt(defending * 0.50f + passing * 0.25f + dribbling * 0.20f + shooting * 0.05f), 1, 99);

                case TacticalPosition.MED:
                    return Mathf.Clamp(Mathf.RoundToInt(passing * 0.35f + dribbling * 0.30f + shooting * 0.20f + defending * 0.15f), 1, 99);

                case TacticalPosition.DEL:
                default:
                    return Mathf.Clamp(Mathf.RoundToInt(shooting * 0.45f + dribbling * 0.30f + passing * 0.20f + defending * 0.05f), 1, 99);
            }
        }

        public CardStatsSummary GetDisplayStats()
        {
            if (TacticalLine == TacticalPosition.POR)
            {
                return new CardStatsSummary
                {
                    stat1Name = "EST", stat1Value = diving,
                    stat2Name = "REF", stat2Value = reflexes,
                    stat3Name = "PAR", stat3Value = handling,
                    stat4Name = "COL", stat4Value = positioning
                };
            }
            else
            {
                return new CardStatsSummary
                {
                    stat1Name = "TIR", stat1Value = shooting,
                    stat2Name = "PAS", stat2Value = passing,
                    stat3Name = "DEF", stat3Value = defending,
                    stat4Name = "REG", stat4Value = dribbling
                };
            }
        }
    }

    public class PlayerCollectionManager : MonoBehaviour
    {
        public static PlayerCollectionManager Instance { get; private set; }

        public event Action OnCollectionUpdated;
        public event Action<int> OnCollectionPowerUpdated;

        public int CollectionPower { get; private set; }

        [Header("Owned Cards (CardId -> Count)")]
        private Dictionary<string, int> ownedCards = new Dictionary<string, int>();

        [Header("Pilot Album Catalog (Fallback)")]
        [SerializeField] private List<CardCatalogItem> pilotAlbumCatalog = new List<CardCatalogItem>();

        [Header("Multi-Album Management")]
        private Dictionary<string, AlbumData> loadedAlbums = new Dictionary<string, AlbumData>();
        private Dictionary<string, CardCatalogItem> allCardsCatalog = new Dictionary<string, CardCatalogItem>();

        private string GetPrefKey(string cardId)
        {
            string uid = Networking.FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(Networking.FirebaseAuthManager.Instance.UserId)
                ? Networking.FirebaseAuthManager.Instance.UserId
                : PlayerPrefs.GetString("Firebase_UserId", "local");
            return $"Collection_{uid}_Card_{cardId}";
        }

        private string GetTotalUniquePrefKey()
        {
            string uid = Networking.FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(Networking.FirebaseAuthManager.Instance.UserId)
                ? Networking.FirebaseAuthManager.Instance.UserId
                : PlayerPrefs.GetString("Firebase_UserId", "local");
            return $"Collection_{uid}_TotalUnique";
        }

        private string GetOwnedIdsListKey()
        {
            string uid = Networking.FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(Networking.FirebaseAuthManager.Instance.UserId)
                ? Networking.FirebaseAuthManager.Instance.UserId
                : PlayerPrefs.GetString("Firebase_UserId", "local");
            return $"Collection_{uid}_OwnedIdsList";
        }

        public static void EnsureExists()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<PlayerCollectionManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("PlayerCollectionManager");
                    Instance = go.AddComponent<PlayerCollectionManager>();
                }
            }
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
            InitializePilotCatalog();
            LoadAllAlbums();
            LoadCollection();
        }

        private void InitializePilotCatalog()
        {
            pilotAlbumCatalog = new List<CardCatalogItem>
            {
                new CardCatalogItem { 
                    cardId = "card_01", playerName = "Vozhina", initials = "VO", teamName = "FC Piloto", position = "POR", rarity = Rarity.Comun, albumId = "album_piloto_liga",
                    nationality = "Rusia", countryCode = "RU", diving = 70, reflexes = 74, handling = 68, positioning = 71
                },
                new CardCatalogItem { 
                    cardId = "card_02", playerName = "Balogun", initials = "FB", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Comun, albumId = "album_piloto_liga",
                    nationality = "Estados Unidos", countryCode = "US", shooting = 78, dribbling = 76, passing = 66, defending = 32
                },
                new CardCatalogItem { 
                    cardId = "card_03", playerName = "Diomandé", initials = "OD", teamName = "FC Piloto", position = "DEF", rarity = Rarity.Comun, albumId = "album_piloto_liga",
                    nationality = "Costa de Marfil", countryCode = "CI", defending = 79, passing = 64, dribbling = 66, shooting = 35
                },
                new CardCatalogItem { 
                    cardId = "card_04", playerName = "James Rodríguez", initials = "JR", teamName = "FC Piloto", position = "MED", rarity = Rarity.Comun, albumId = "album_piloto_liga",
                    nationality = "Colombia", countryCode = "CO", passing = 86, dribbling = 83, shooting = 82, defending = 44
                },
                new CardCatalogItem { 
                    cardId = "card_05", playerName = "Luis Díaz", initials = "LD", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Especial, albumId = "album_piloto_liga",
                    nationality = "Colombia", countryCode = "CO", dribbling = 87, shooting = 82, passing = 78, defending = 40
                },
                new CardCatalogItem { 
                    cardId = "card_06", playerName = "Erling Haaland", initials = "EH", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Especial, albumId = "album_piloto_liga",
                    nationality = "Noruega", countryCode = "NO", shooting = 93, dribbling = 82, passing = 70, defending = 45, manualOverall = 88
                },
                new CardCatalogItem { 
                    cardId = "card_07", playerName = "Cristiano Ronaldo", initials = "CR", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Epica, albumId = "album_piloto_liga",
                    nationality = "Portugal", countryCode = "PT", shooting = 90, dribbling = 83, passing = 76, defending = 35, manualOverall = 86
                },
                new CardCatalogItem { 
                    cardId = "card_08", playerName = "Lionel Messi", initials = "LM", teamName = "FC Piloto", position = "MED", rarity = Rarity.Legendaria, albumId = "album_piloto_liga",
                    nationality = "Argentina", countryCode = "AR", dribbling = 94, passing = 91, shooting = 88, defending = 34, manualOverall = 91
                },
                new CardCatalogItem { 
                    cardId = "card_09", playerName = "Kylian Mbappé", initials = "KM", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Legendaria, albumId = "album_piloto_liga",
                    nationality = "Francia", countryCode = "FR", shooting = 91, dribbling = 92, passing = 81, defending = 36, manualOverall = 91
                },
                new CardCatalogItem { 
                    cardId = "card_10", playerName = "Lamine Yamal", initials = "LY", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Mitica, albumId = "album_piloto_liga",
                    nationality = "España", countryCode = "ES", dribbling = 89, passing = 84, shooting = 83, defending = 38, manualOverall = 85
                }
            };

            foreach (var card in pilotAlbumCatalog)
            {
                if (!allCardsCatalog.ContainsKey(card.cardId))
                {
                    allCardsCatalog[card.cardId] = card;
                }
            }
        }

        /// <summary>
        /// Carga automáticamente todos los AlbumData ubicados en Resources/Albums/ (y subcarpetas).
        /// </summary>
        public void LoadAllAlbums()
        {
            AlbumData[] resourceAlbums = Resources.LoadAll<AlbumData>("Albums");
            if (resourceAlbums != null)
            {
                foreach (var album in resourceAlbums)
                {
                    RegisterAlbum(album);
                }
            }

            // También comprobar si Album_Piloto existe en ScriptableObjects (Unity Editor)
#if UNITY_EDITOR
            string[] albumGuids = UnityEditor.AssetDatabase.FindAssets("t:AlbumData");
            foreach (var guid in albumGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                AlbumData album = UnityEditor.AssetDatabase.LoadAssetAtPath<AlbumData>(path);
                if (album != null)
                {
                    RegisterAlbum(album);
                }
            }
#endif
        }

        /// <summary>
        /// Registra un AlbumData dinámico y sus cartas asociadas en el catálogo global.
        /// </summary>
        public void RegisterAlbum(AlbumData album)
        {
            if (album == null || string.IsNullOrEmpty(album.albumId)) return;

            loadedAlbums[album.albumId] = album;

            if (album.cards != null)
            {
                foreach (var card in album.cards)
                {
                    if (card == null || string.IsNullOrEmpty(card.cardId)) continue;

                    string inits = "TC";
                    if (!string.IsNullOrEmpty(card.playerName))
                    {
                        var parts = card.playerName.Trim().Split(' ');
                        inits = parts.Length >= 2
                            ? $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper()
                            : (card.playerName.Length >= 2 ? card.playerName.Substring(0, 2).ToUpper() : card.playerName.ToUpper());
                    }

                    allCardsCatalog[card.cardId] = new CardCatalogItem
                    {
                        cardId = card.cardId,
                        playerName = card.playerName,
                        initials = inits,
                        teamName = card.teamName,
                        position = card.position,
                        rarity = card.rarity,
                        albumId = album.albumId,
                        nationality = card.nationality,
                        countryCode = card.countryCode,
                        shooting = card.shooting,
                        passing = card.passing,
                        defending = card.defending,
                        dribbling = card.dribbling,
                        diving = card.diving,
                        reflexes = card.reflexes,
                        handling = card.handling,
                        positioning = card.positioning,
                        manualOverall = card.manualOverall
                    };
                }
            }
        }

        public void LoadCollection()
        {
            ownedCards.Clear();

            // 1. Cargar desde la lista persistente de IDs de cartas obtenidas
            string idListStr = PlayerPrefs.GetString(GetOwnedIdsListKey(), "");
            if (!string.IsNullOrEmpty(idListStr))
            {
                string[] ids = idListStr.Split(',');
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    int count = PlayerPrefs.GetInt(GetPrefKey(id), 0);
                    if (count > 0)
                    {
                        ownedCards[id] = count;
                    }
                }
            }

            // 2. Por compatibilidad y redundancia, verificar también todas las cartas del catálogo
            foreach (var card in allCardsCatalog.Values)
            {
                if (card == null || string.IsNullOrEmpty(card.cardId)) continue;
                if (ownedCards.ContainsKey(card.cardId)) continue;

                string key = GetPrefKey(card.cardId);
                int count = PlayerPrefs.GetInt(key, 0);

                if (count > 0)
                {
                    ownedCards[card.cardId] = count;
                }
            }

            CalculateCollectionPower();
            Debug.Log($"<color=green>[Collection] Colección cargada: {ownedCards.Count}/{allCardsCatalog.Count} cartas únicas desbloqueadas en {loadedAlbums.Count} álbumes. Poder: {CollectionPower}</color>");
        }

        /// <summary>
        /// Limpia completamente la colección en memoria y notifica a la UI. Usado al cerrar sesión.
        /// </summary>
        public void ClearCollection()
        {
            ownedCards.Clear();
            CollectionPower = 0;
            PlayerPrefs.DeleteKey(GetOwnedIdsListKey());
            PlayerPrefs.SetInt("Player_CollectionPower", 0);
            PlayerPrefs.Save();

            OnCollectionUpdated?.Invoke();
            OnCollectionPowerUpdated?.Invoke(0);
            Debug.Log("<color=yellow>[Collection] Memoria de colección limpiada a 0.</color>");
        }

        /// <summary>
        /// Cambia el contexto de usuario y carga su inventario específico desde PlayerPrefs.
        /// </summary>
        public void SwitchUser(string newUid)
        {
            ownedCards.Clear();
            LoadCollection();
            OnCollectionUpdated?.Invoke();
            OnCollectionPowerUpdated?.Invoke(CollectionPower);
            Debug.Log($"<color=cyan>[Collection] Usuario cambiado a {newUid}. Cartas: {ownedCards.Count}, Poder: {CollectionPower}</color>");
        }

        /// <summary>
        /// Restaura el inventario de cartas descargado desde Firestore y lo persiste localmente para el usuario.
        /// </summary>
        public void LoadFromCloudMap(Dictionary<string, int> cloudCards)
        {
            ownedCards.Clear();
            if (cloudCards != null)
            {
                foreach (var kvp in cloudCards)
                {
                    if (kvp.Value > 0)
                    {
                        ownedCards[kvp.Key] = kvp.Value;
                        PlayerPrefs.SetInt(GetPrefKey(kvp.Key), kvp.Value);
                    }
                }
            }
            var idList = new List<string>(ownedCards.Keys);
            PlayerPrefs.SetString(GetOwnedIdsListKey(), string.Join(",", idList));
            PlayerPrefs.SetInt(GetTotalUniquePrefKey(), ownedCards.Count);
            PlayerPrefs.Save();

            CalculateCollectionPower();
            OnCollectionUpdated?.Invoke();
            OnCollectionPowerUpdated?.Invoke(CollectionPower);
            Debug.Log($"<color=green>[Collection] Colección restaurada desde la nube: {ownedCards.Count} cartas únicas. Poder: {CollectionPower}</color>");
        }

        /// <summary>
        /// Resetea la colección a 0 y restaura las monedas al balance inicial de prueba.
        /// </summary>
        public void ResetAlphaAccountData(int initialCoins = 400)
        {
            InitializePilotCatalog();
            ownedCards.Clear();
            foreach (var card in pilotAlbumCatalog)
            {
                PlayerPrefs.DeleteKey(GetPrefKey(card.cardId));
                PlayerPrefs.DeleteKey("Collection_Card_" + card.cardId);
            }
            // También limpiar viejas keys por si acaso
            string[] legacyKeys = { "EH", "RO", "LD", "VJ", "KM", "PE", "LY", "JB", "MS", "KDB" };
            foreach (var k in legacyKeys) PlayerPrefs.DeleteKey("Collection_Card_" + k);

            PlayerPrefs.DeleteKey(GetTotalUniquePrefKey());
            PlayerPrefs.DeleteKey("Collection_TotalUnique");
            PlayerPrefs.SetInt("Player_CollectionPower", 0);
            PlayerPrefs.SetInt("Firebase_Coins", initialCoins);
            PlayerPrefs.Save();

            CollectionPower = 0;
            if (JuegoTCG.Networking.FirebaseAuthManager.Instance != null)
            {
                JuegoTCG.Networking.FirebaseAuthManager.Instance.SetCoins(initialCoins);
            }

            OnCollectionUpdated?.Invoke();
            OnCollectionPowerUpdated?.Invoke(0);
            Debug.Log($"<color=yellow>[Alpha Reset] ¡Cuenta reiniciada a 0! Cartas: 0/{pilotAlbumCatalog.Count}, Monedas: {initialCoins}</color>");
        }

        public void SaveCollection()
        {
            var idList = new List<string>(ownedCards.Keys);
            PlayerPrefs.SetString(GetOwnedIdsListKey(), string.Join(",", idList));

            foreach (var kvp in ownedCards)
            {
                PlayerPrefs.SetInt(GetPrefKey(kvp.Key), kvp.Value);
            }
            PlayerPrefs.SetInt(GetTotalUniquePrefKey(), ownedCards.Count);
            PlayerPrefs.Save();

            CalculateCollectionPower();
            OnCollectionUpdated?.Invoke();
        }

        /// <summary>
        /// Calcula el poder de colección oficial según la fórmula del GDD Sección 7.2:
        /// Suma de puntos fijos por rareza multiplicados por cartas ÚNICAS obtenidas (duplicados no suman).
        /// Comun: 1, Especial: 2, Epica: 4, Legendaria: 8, Mitica: 15, FullArt: 25.
        /// </summary>
        public int CalculateCollectionPower(bool notify = true)
        {
            int totalPower = 0;
            foreach (var card in allCardsCatalog.Values)
            {
                if (IsCardOwned(card.cardId))
                {
                    totalPower += GetRarityPowerPoints(card.rarity);
                }
            }

            CollectionPower = totalPower;
            PlayerPrefs.SetInt("Player_CollectionPower", CollectionPower);
            PlayerPrefs.Save();

            if (notify)
            {
                OnCollectionPowerUpdated?.Invoke(CollectionPower);
            }
            return CollectionPower;
        }

        public static int GetRarityPowerPoints(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Comun: return 1;
                case Rarity.Especial: return 2;
                case Rarity.Epica: return 4;
                case Rarity.Legendaria: return 8;
                case Rarity.Mitica: return 15;
                case Rarity.FullArt: return 25;
                default: return 1;
            }
        }

        public void AddCard(string cardId, int qty = 1)
        {
            if (string.IsNullOrEmpty(cardId)) return;

            if (ownedCards.ContainsKey(cardId))
            {
                ownedCards[cardId] += qty;
            }
            else
            {
                ownedCards[cardId] = qty;
            }

            SaveCollection();
            CalculateCollectionPower();
            if (Networking.FirebaseAuthManager.Instance != null)
            {
                _ = Networking.FirebaseAuthManager.Instance.SyncUserProfileToFirestoreAsync();
            }
            Debug.Log($"<color=cyan>[Collection] Carta añadida: {cardId} (+{qty}). Total en inventario: {ownedCards[cardId]}</color>");
        }

        public bool RemoveCard(string cardId, int qty = 1)
        {
            if (string.IsNullOrEmpty(cardId)) return false;
            if (!ownedCards.ContainsKey(cardId) || ownedCards[cardId] < qty) return false;

            ownedCards[cardId] -= qty;
            if (ownedCards[cardId] <= 0)
            {
                ownedCards.Remove(cardId);
                PlayerPrefs.DeleteKey(GetPrefKey(cardId));
            }

            SaveCollection();
            CalculateCollectionPower();
            if (Networking.FirebaseAuthManager.Instance != null)
            {
                _ = Networking.FirebaseAuthManager.Instance.SyncUserProfileToFirestoreAsync();
            }

            Debug.Log($"<color=orange>[Collection] Carta removida: {cardId} (-{qty}). Restantes: {(ownedCards.ContainsKey(cardId) ? ownedCards[cardId] : 0)}</color>");
            return true;
        }

        public void AddCards(List<CardData> newCards)
        {
            if (newCards == null || newCards.Count == 0) return;
            bool addedAny = false;
            foreach (var card in newCards)
            {
                if (card != null && !string.IsNullOrEmpty(card.cardId))
                {
                    if (ownedCards.ContainsKey(card.cardId))
                    {
                        ownedCards[card.cardId]++;
                    }
                    else
                    {
                        ownedCards[card.cardId] = 1;
                    }
                    addedAny = true;
                }
            }

            if (addedAny)
            {
                SaveCollection();
                CalculateCollectionPower();
                if (Networking.FirebaseAuthManager.Instance != null)
                {
                    _ = Networking.FirebaseAuthManager.Instance.SyncUserProfileToFirestoreAsync();
                }
                Debug.Log($"<color=cyan>[Collection] Lote de sobres añadido ({newCards.Count} cartas). Poder actualizado: {CollectionPower}</color>");
            }
        }

        public bool IsCardOwned(string cardId)
        {
            return ownedCards.ContainsKey(cardId) && ownedCards[cardId] > 0;
        }

        public int GetOwnedCount(string cardId)
        {
            return ownedCards.ContainsKey(cardId) ? ownedCards[cardId] : 0;
        }

        public int GetCardCount(string cardId)
        {
            return GetOwnedCount(cardId);
        }

        public int GetUniqueOwnedCount()
        {
            return ownedCards.Count;
        }

        public int GetTotalUniqueCardsOwned()
        {
            return GetUniqueOwnedCount();
        }

        public int GetTotalCardsCount()
        {
            int sum = 0;
            foreach (var count in ownedCards.Values) sum += count;
            return sum;
        }

        public Dictionary<string, int> GetOwnedCardsMap()
        {
            return new Dictionary<string, int>(ownedCards);
        }

        public List<CardCatalogItem> GetCatalog()
        {
            return new List<CardCatalogItem>(allCardsCatalog.Values);
        }

        public CardCatalogItem GetCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            if (allCardsCatalog.TryGetValue(cardId, out var item)) return item;
            return pilotAlbumCatalog.Find(c => c.cardId == cardId);
        }

        public List<AlbumData> GetAllAlbums()
        {
            return new List<AlbumData>(loadedAlbums.Values);
        }

        public AlbumData GetAlbum(string albumId)
        {
            if (string.IsNullOrEmpty(albumId)) return null;
            loadedAlbums.TryGetValue(albumId, out var album);
            return album;
        }

        /// <summary>
        /// Obtiene el progreso de un álbum específico en base a su albumId.
        /// </summary>
        public void GetAlbumProgress(string albumId, out int ownedUnique, out int totalCards, out float percentage)
        {
            ownedUnique = 0;
            totalCards = 0;
            percentage = 0f;

            if (string.IsNullOrEmpty(albumId)) return;

            // Si el álbum está registrado en loadedAlbums y tiene cartas
            if (loadedAlbums.TryGetValue(albumId, out var album) && album.cards != null && album.cards.Count > 0)
            {
                totalCards = album.cards.Count;
                foreach (var c in album.cards)
                {
                    if (c != null && IsCardOwned(c.cardId)) ownedUnique++;
                }
            }
            else
            {
                // Buscar en allCardsCatalog por albumId
                foreach (var c in allCardsCatalog.Values)
                {
                    if (c.albumId == albumId)
                    {
                        totalCards++;
                        if (IsCardOwned(c.cardId)) ownedUnique++;
                    }
                }
            }

            percentage = totalCards > 0 ? (float)ownedUnique / totalCards : 0f;
        }

        /// <summary>
        /// Comprueba si un álbum ha sido completado al 100% (todas las cartas únicas obtenidas).
        /// </summary>
        public bool IsAlbumCompleted(string albumId)
        {
            GetAlbumProgress(albumId, out int ownedUnique, out int totalCards, out float pct);
            return totalCards > 0 && ownedUnique >= totalCards;
        }

        /// <summary>
        /// Sobrecarga heredada para compatibilidad con código existente (evalúa el álbum piloto).
        /// </summary>
        public void GetAlbumProgress(out int ownedUnique, out int totalCards, out float percentage)
        {
            GetAlbumProgress("album_piloto_liga", out ownedUnique, out totalCards, out percentage);
            if (totalCards == 0)
            {
                totalCards = pilotAlbumCatalog.Count;
                ownedUnique = 0;
                foreach (var card in pilotAlbumCatalog)
                {
                    if (IsCardOwned(card.cardId)) ownedUnique++;
                }
                percentage = totalCards > 0 ? (float)ownedUnique / totalCards : 0f;
            }
        }
    }
}

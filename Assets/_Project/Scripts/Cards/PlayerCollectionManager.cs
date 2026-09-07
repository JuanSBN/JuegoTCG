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
        public Rarity rarity;
        public string albumId;
    }

    public class PlayerCollectionManager : MonoBehaviour
    {
        public static PlayerCollectionManager Instance { get; private set; }

        public event Action OnCollectionUpdated;
        public event Action<int> OnCollectionPowerUpdated;

        public int CollectionPower { get; private set; }

        [Header("Owned Cards (CardId -> Count)")]
        private Dictionary<string, int> ownedCards = new Dictionary<string, int>();

        [Header("Pilot Album Catalog")]
        [SerializeField] private List<CardCatalogItem> pilotAlbumCatalog = new List<CardCatalogItem>();

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
            LoadCollection();
        }

        private void InitializePilotCatalog()
        {
            pilotAlbumCatalog = new List<CardCatalogItem>
            {
                new CardCatalogItem { cardId = "card_01", playerName = "Vozhina", initials = "VO", teamName = "FC Piloto", position = "DEF", rarity = Rarity.Comun, albumId = "album_piloto_liga" },
                new CardCatalogItem { cardId = "card_02", playerName = "Balogun", initials = "FB", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Comun, albumId = "album_piloto_liga" },
                new CardCatalogItem { cardId = "card_03", playerName = "Diomandé", initials = "OD", teamName = "FC Piloto", position = "DEF", rarity = Rarity.Comun, albumId = "album_piloto_liga" },
                new CardCatalogItem { cardId = "card_04", playerName = "James Rodríguez", initials = "JR", teamName = "FC Piloto", position = "MED", rarity = Rarity.Comun, albumId = "album_piloto_liga" },
                new CardCatalogItem { cardId = "card_05", playerName = "Luis Díaz", initials = "LD", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Especial, albumId = "album_piloto_liga" },
                new CardCatalogItem { cardId = "card_06", playerName = "Erling Haaland", initials = "EH", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Especial, albumId = "album_piloto_liga" },
                new CardCatalogItem { cardId = "card_07", playerName = "Cristiano Ronaldo", initials = "CR", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Epica, albumId = "album_piloto_liga" },
                new CardCatalogItem { cardId = "card_08", playerName = "Lionel Messi", initials = "LM", teamName = "FC Piloto", position = "MED", rarity = Rarity.Legendaria, albumId = "album_piloto_liga" },
                new CardCatalogItem { cardId = "card_09", playerName = "Kylian Mbappé", initials = "KM", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Legendaria, albumId = "album_piloto_liga" },
                new CardCatalogItem { cardId = "card_10", playerName = "Lamine Yamal", initials = "LY", teamName = "FC Piloto", position = "DEL", rarity = Rarity.Mitica, albumId = "album_piloto_liga" }
            };
        }

        public void LoadCollection()
        {
            ownedCards.Clear();
            foreach (var card in pilotAlbumCatalog)
            {
                string key = GetPrefKey(card.cardId);
                int count = PlayerPrefs.GetInt(key, 0);

                if (count > 0)
                {
                    ownedCards[card.cardId] = count;
                }
            }

            CalculateCollectionPower();
            Debug.Log($"<color=green>[Collection] Colección cargada: {ownedCards.Count}/{pilotAlbumCatalog.Count} cartas únicas desbloqueadas. Poder: {CollectionPower}</color>");
        }

        /// <summary>
        /// Limpia completamente la colección en memoria y notifica a la UI. Usado al cerrar sesión.
        /// </summary>
        public void ClearCollection()
        {
            ownedCards.Clear();
            CollectionPower = 0;
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
            foreach (var card in pilotAlbumCatalog)
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
            return pilotAlbumCatalog;
        }

        public void GetAlbumProgress(out int ownedUnique, out int totalCards, out float percentage)
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

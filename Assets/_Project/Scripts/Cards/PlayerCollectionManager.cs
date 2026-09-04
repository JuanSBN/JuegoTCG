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

        private const string PREF_COLLECTION_PREFIX = "Collection_Card_";
        private const string PREF_TOTAL_UNIQUE = "Collection_TotalUnique";

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
            if (pilotAlbumCatalog.Count > 0) return;

            pilotAlbumCatalog = new List<CardCatalogItem>
            {
                new CardCatalogItem { cardId = "LD", playerName = "Luis Díaz", initials = "LD", teamName = "Liverpool", position = "DEL", rarity = Rarity.Mitica, albumId = "album_piloto_01" },
                new CardCatalogItem { cardId = "VJ", playerName = "Vinicius Jr.", initials = "VJ", teamName = "Real Madrid", position = "DEL", rarity = Rarity.Epica, albumId = "album_piloto_01" },
                new CardCatalogItem { cardId = "EH", playerName = "Erling Haaland", initials = "EH", teamName = "Man. City", position = "DEL", rarity = Rarity.Comun, albumId = "album_piloto_01" },
                new CardCatalogItem { cardId = "KM", playerName = "Kylian Mbappé", initials = "KM", teamName = "Real Madrid", position = "DEL", rarity = Rarity.Especial, albumId = "album_piloto_01" },
                new CardCatalogItem { cardId = "PE", playerName = "Pedri González", initials = "PE", teamName = "Barcelona", position = "MED", rarity = Rarity.Epica, albumId = "album_piloto_01" },
                new CardCatalogItem { cardId = "LY", playerName = "Lamine Yamal", initials = "LY", teamName = "Barcelona", position = "DEL", rarity = Rarity.Mitica, albumId = "album_piloto_01" },
                new CardCatalogItem { cardId = "JB", playerName = "Jude Bellingham", initials = "JB", teamName = "Real Madrid", position = "MED", rarity = Rarity.Epica, albumId = "album_piloto_01" },
                new CardCatalogItem { cardId = "RO", playerName = "Rodri Hernández", initials = "RO", teamName = "Man. City", position = "MED", rarity = Rarity.Comun, albumId = "album_piloto_01" },
                new CardCatalogItem { cardId = "MS", playerName = "Mohamed Salah", initials = "MS", teamName = "Liverpool", position = "DEL", rarity = Rarity.Especial, albumId = "album_piloto_01" },
                new CardCatalogItem { cardId = "KDB", playerName = "Kevin De Bruyne", initials = "KDB", teamName = "Man. City", position = "MED", rarity = Rarity.Epica, albumId = "album_piloto_01" }
            };
        }

        private void LoadCollection()
        {
            ownedCards.Clear();
            foreach (var card in pilotAlbumCatalog)
            {
                int count = PlayerPrefs.GetInt(PREF_COLLECTION_PREFIX + card.cardId, 0);
                if (count > 0)
                {
                    ownedCards[card.cardId] = count;
                }
            }

            // Si es la primera vez, dar 2 cartas iniciales desbloqueadas
            if (ownedCards.Count == 0)
            {
                ownedCards["EH"] = 2; // Haaland x2
                ownedCards["RO"] = 1; // Rodri x1
                SaveCollection();
            }

            CalculateCollectionPower();
            Debug.Log($"<color=green>[Collection] Colección cargada: {ownedCards.Count}/{pilotAlbumCatalog.Count} cartas únicas desbloqueadas. Poder: {CollectionPower}</color>");
        }

        public void SaveCollection()
        {
            foreach (var kvp in ownedCards)
            {
                PlayerPrefs.SetInt(PREF_COLLECTION_PREFIX + kvp.Key, kvp.Value);
            }
            PlayerPrefs.SetInt(PREF_TOTAL_UNIQUE, ownedCards.Count);
            PlayerPrefs.Save();

            CalculateCollectionPower();
            OnCollectionUpdated?.Invoke();
        }

        /// <summary>
        /// Calcula el poder de colección oficial según la fórmula del GDD Sección 7.2:
        /// Suma de puntos fijos por rareza multiplicados por cartas ÚNICAS obtenidas (duplicados no suman).
        /// Comun: 1, Especial: 2, Epica: 4, Legendaria: 8, Mitica: 15, FullArt: 25.
        /// </summary>
        public int CalculateCollectionPower()
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

            OnCollectionPowerUpdated?.Invoke(CollectionPower);
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
            Debug.Log($"<color=cyan>[Collection] Carta añadida: {cardId} (+{qty}). Total en inventario: {ownedCards[cardId]}</color>");
        }

        public void AddCards(List<CardData> newCards)
        {
            if (newCards == null) return;
            foreach (var card in newCards)
            {
                if (card != null && !string.IsNullOrEmpty(card.cardId))
                {
                    AddCard(card.cardId, 1);
                }
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

        public int GetTotalCardsCount()
        {
            int sum = 0;
            foreach (var count in ownedCards.Values) sum += count;
            return sum;
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

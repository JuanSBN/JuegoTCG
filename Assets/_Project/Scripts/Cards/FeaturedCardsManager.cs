using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuegoTCG.Cards
{
    public class FeaturedCardsManager : MonoBehaviour
    {
        public static FeaturedCardsManager Instance { get; private set; }

        public event Action OnFeaturedUpdated;

        public const int SlotCount = 3;

        [Header("Featured Cards (3 Slots)")]
        [SerializeField] private string[] featuredCardIds = new string[SlotCount] { "", "", "" };

        private const string PREF_FEATURED_PREFIX = "Profile_Featured_Slot_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFeaturedCards();
        }

        public static void EnsureExists()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("FeaturedCardsManager");
                Instance = go.AddComponent<FeaturedCardsManager>();
                DontDestroyOnLoad(go);
            }
        }

        public void LoadFeaturedCards()
        {
            if (featuredCardIds == null || featuredCardIds.Length != SlotCount)
            {
                featuredCardIds = new string[SlotCount] { "", "", "" };
            }

            for (int i = 0; i < SlotCount; i++)
            {
                string savedId = PlayerPrefs.GetString(PREF_FEATURED_PREFIX + i, "");
                // Solo conservar la carta si el jugador realmente la posee
                if (!string.IsNullOrEmpty(savedId) && PlayerCollectionManager.Instance != null && !PlayerCollectionManager.Instance.IsCardOwned(savedId))
                {
                    savedId = "";
                }
                featuredCardIds[i] = savedId;
            }
            Debug.Log($"<color=green>[FeaturedCards] Cartas destacadas cargadas: [{string.Join(", ", featuredCardIds)}]</color>");
        }

        public void ValidateOwnedCards()
        {
            if (PlayerCollectionManager.Instance == null) return;
            bool changed = false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (!string.IsNullOrEmpty(featuredCardIds[i]) &&
                    !PlayerCollectionManager.Instance.IsCardOwned(featuredCardIds[i]))
                {
                    featuredCardIds[i] = "";
                    PlayerPrefs.SetString(PREF_FEATURED_PREFIX + i, "");
                    changed = true;
                }
            }

            if (changed)
            {
                PlayerPrefs.Save();
                OnFeaturedUpdated?.Invoke();
            }
        }

        public string GetSlotCardId(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return "";
            return featuredCardIds[slotIndex] ?? "";
        }

        public bool IsCardFeatured(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (featuredCardIds[i] == cardId) return true;
            }
            return false;
        }

        public int GetCardFeaturedSlotIndex(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return -1;
            for (int i = 0; i < SlotCount; i++)
            {
                if (featuredCardIds[i] == cardId) return i;
            }
            return -1;
        }

        public void AssignCardToSlot(int slotIndex, string cardId)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return;

            // Regla anti-duplicados: si la carta ya estaba destacada en otro slot, desasignarla de allí
            for (int i = 0; i < SlotCount; i++)
            {
                if (i != slotIndex && featuredCardIds[i] == cardId)
                {
                    featuredCardIds[i] = "";
                    PlayerPrefs.SetString(PREF_FEATURED_PREFIX + i, "");
                }
            }

            featuredCardIds[slotIndex] = cardId ?? "";
            PlayerPrefs.SetString(PREF_FEATURED_PREFIX + slotIndex, featuredCardIds[slotIndex]);
            PlayerPrefs.Save();

            Debug.Log($"<color=gold>[FeaturedCards] Carta '{cardId}' asignada a slot destacado #{slotIndex}.</color>");
            OnFeaturedUpdated?.Invoke();
        }

        public void RemoveCardFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return;

            string oldId = featuredCardIds[slotIndex];
            featuredCardIds[slotIndex] = "";
            PlayerPrefs.SetString(PREF_FEATURED_PREFIX + slotIndex, "");
            PlayerPrefs.Save();

            Debug.Log($"<color=orange>[FeaturedCards] Carta '{oldId}' removida del slot #{slotIndex}.</color>");
            OnFeaturedUpdated?.Invoke();
        }

        public int GetFilledSlotsCount()
        {
            int count = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (!string.IsNullOrEmpty(featuredCardIds[i])) count++;
            }
            return count;
        }
    }
}

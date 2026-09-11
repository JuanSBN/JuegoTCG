using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuegoTCG.Cards
{
    [Serializable]
    public class PitchSlotData
    {
        public int slotIndex;
        public string positionCode; // "POR", "DEF_LI", "DEF_C1", "DEF_C2", "DEF_LD", "MED_I", "MED_C", "MED_D", "DEL_EI", "DEL_DC", "DEL_ED"
        public string positionName; // "Portero", "Lateral Izq.", "Defensa Central", etc.
        public string assignedCardId; // "LD", "EH", etc. o vacio
    }

    public class Ideal11SquadManager : MonoBehaviour
    {
        public static Ideal11SquadManager Instance { get; private set; }

        public event Action OnSquadUpdated;

        [Header("Formation 4-3-3 (11 Slots)")]
        [SerializeField] private List<PitchSlotData> formationSlots = new List<PitchSlotData>();

        private const string PREF_SLOT_PREFIX = "Ideal11_Slot_";

        private string GetPrefKey(int slotIndex)
        {
            string uid = Networking.FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(Networking.FirebaseAuthManager.Instance.UserId)
                ? Networking.FirebaseAuthManager.Instance.UserId
                : PlayerPrefs.GetString("Firebase_UserId", "local");
            return $"Ideal11_{uid}_Slot_{slotIndex}";
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
            InitializeFormation();
            LoadSquad();
        }

        public void InitializeFormation()
        {
            if (formationSlots != null && formationSlots.Count == 11) return;

            formationSlots = new List<PitchSlotData>
            {
                // Delantera (3)
                new PitchSlotData { slotIndex = 0, positionCode = "DEL_EI", positionName = "Extremo Izq.", assignedCardId = "" },
                new PitchSlotData { slotIndex = 1, positionCode = "DEL_DC", positionName = "Delantero Centro", assignedCardId = "" },
                new PitchSlotData { slotIndex = 2, positionCode = "DEL_ED", positionName = "Extremo Der.", assignedCardId = "" },

                // Mediocampo (3)
                new PitchSlotData { slotIndex = 3, positionCode = "MED_I", positionName = "Interior Izq.", assignedCardId = "" },
                new PitchSlotData { slotIndex = 4, positionCode = "MED_C", positionName = "Pivote", assignedCardId = "" },
                new PitchSlotData { slotIndex = 5, positionCode = "MED_D", positionName = "Interior Der.", assignedCardId = "" },

                // Defensa (4)
                new PitchSlotData { slotIndex = 6, positionCode = "DEF_LI", positionName = "Lateral Izq.", assignedCardId = "" },
                new PitchSlotData { slotIndex = 7, positionCode = "DEF_C1", positionName = "Central 1", assignedCardId = "" },
                new PitchSlotData { slotIndex = 8, positionCode = "DEF_C2", positionName = "Central 2", assignedCardId = "" },
                new PitchSlotData { slotIndex = 9, positionCode = "DEF_LD", positionName = "Lateral Der.", assignedCardId = "" },

                // Portero (1)
                new PitchSlotData { slotIndex = 10, positionCode = "POR", positionName = "Portero", assignedCardId = "" }
            };
        }

        public void LoadSquad()
        {
            bool collectionReady = PlayerCollectionManager.Instance != null && PlayerCollectionManager.Instance.GetUniqueOwnedCount() > 0;

            for (int i = 0; i < formationSlots.Count; i++)
            {
                string key = GetPrefKey(i);
                string savedId = "";

                if (PlayerPrefs.HasKey(key))
                {
                    savedId = PlayerPrefs.GetString(key, "");
                }
                else if (PlayerPrefs.HasKey(PREF_SLOT_PREFIX + i))
                {
                    savedId = PlayerPrefs.GetString(PREF_SLOT_PREFIX + i, "");
                }

                // Solo limpiar la carta si la colección ya cargó y el jugador no la posee
                if (!string.IsNullOrEmpty(savedId) && collectionReady && !PlayerCollectionManager.Instance.IsCardOwned(savedId))
                {
                    savedId = "";
                }

                formationSlots[i].assignedCardId = savedId;
            }
            Debug.Log($"<color=green>[Ideal11] Alineación cargada: {GetFilledSlotsCount()}/11 jugadores posicionados.</color>");
        }

        public void ValidateOwnedCards()
        {
            if (PlayerCollectionManager.Instance == null) return;
            bool changed = false;
            for (int i = 0; i < formationSlots.Count; i++)
            {
                if (!string.IsNullOrEmpty(formationSlots[i].assignedCardId) &&
                    !PlayerCollectionManager.Instance.IsCardOwned(formationSlots[i].assignedCardId))
                {
                    formationSlots[i].assignedCardId = "";
                    changed = true;
                }
            }
            if (changed) SaveSquad();
        }

        public void SaveSquad()
        {
            for (int i = 0; i < formationSlots.Count; i++)
            {
                string cardId = formationSlots[i].assignedCardId ?? "";
                PlayerPrefs.SetString(GetPrefKey(i), cardId);
                PlayerPrefs.SetString(PREF_SLOT_PREFIX + i, cardId);
            }
            PlayerPrefs.Save();
            OnSquadUpdated?.Invoke();
            Debug.Log("<color=cyan>[Ideal11] Alineación guardada con éxito.</color>");

            if (Networking.FirebaseAuthManager.Instance != null && Networking.FirebaseAuthManager.Instance.IsAuthenticated)
            {
                _ = Networking.FirebaseAuthManager.Instance.SyncUserProfileToFirestoreAsync();
            }
        }

        public List<string> GetSquadCardIds()
        {
            var list = new List<string>();
            if (formationSlots == null) return list;
            for (int i = 0; i < formationSlots.Count; i++)
            {
                list.Add(formationSlots[i].assignedCardId ?? "");
            }
            return list;
        }

        public void LoadFromCloudSquad(List<string> cloudSlots)
        {
            if (formationSlots == null || formationSlots.Count != 11)
            {
                InitializeFormation();
            }

            if (cloudSlots != null && cloudSlots.Count > 0)
            {
                for (int i = 0; i < formationSlots.Count && i < cloudSlots.Count; i++)
                {
                    string cardId = cloudSlots[i] ?? "";
                    formationSlots[i].assignedCardId = cardId;
                    PlayerPrefs.SetString(GetPrefKey(i), cardId);
                    PlayerPrefs.SetString(PREF_SLOT_PREFIX + i, cardId);
                }
                PlayerPrefs.Save();
                OnSquadUpdated?.Invoke();
                Debug.Log($"<color=green>[Ideal11] Alineación 11 Ideal restaurada desde Firestore: {GetFilledSlotsCount()}/11 cartas.</color>");
            }
        }

        public static void EnsureExists()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<Ideal11SquadManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("Ideal11SquadManager");
                    Instance = go.AddComponent<Ideal11SquadManager>();
                }
            }
        }

        public void AssignCardToSlot(int slotIndex, string cardId)
        {
            if (slotIndex >= 0 && slotIndex < formationSlots.Count)
            {
                // Regla anti-duplicados: Si la carta ya está asignada en otro slot, desasignarla de allí
                if (!string.IsNullOrEmpty(cardId))
                {
                    for (int i = 0; i < formationSlots.Count; i++)
                    {
                        if (i != slotIndex && formationSlots[i].assignedCardId == cardId)
                        {
                            formationSlots[i].assignedCardId = "";
                        }
                    }
                }

                formationSlots[slotIndex].assignedCardId = cardId;
                SaveSquad();
            }
        }

        public void RemoveCardFromSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < formationSlots.Count)
            {
                formationSlots[slotIndex].assignedCardId = "";
                SaveSquad();
            }
        }

        public PitchSlotData GetSlot(int slotIndex)
        {
            if (formationSlots == null || formationSlots.Count < 11)
            {
                InitializeFormation();
                LoadSquad();
            }
            if (formationSlots != null && slotIndex >= 0 && slotIndex < formationSlots.Count)
            {
                return formationSlots[slotIndex];
            }
            return null;
        }

        public bool IsCardInSquad(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;
            foreach (var s in formationSlots)
            {
                if (s.assignedCardId == cardId) return true;
            }
            return false;
        }

        public int GetFilledSlotsCount()
        {
            int count = 0;
            foreach (var slot in formationSlots)
            {
                if (!string.IsNullOrEmpty(slot.assignedCardId)) count++;
            }
            return count;
        }

        public int CalculateSquadPower()
        {
            int power = 0;
            foreach (var slot in formationSlots)
            {
                if (!string.IsNullOrEmpty(slot.assignedCardId))
                {
                    // Puntos fijos según rareza de la carta (GDD 7.2)
                    if (slot.assignedCardId == "LD" || slot.assignedCardId == "LY") power += 35; // Míticas
                    else if (slot.assignedCardId == "VJ" || slot.assignedCardId == "PE" || slot.assignedCardId == "JB" || slot.assignedCardId == "KDB") power += 20; // Épicas
                    else if (slot.assignedCardId == "KM" || slot.assignedCardId == "MS") power += 12; // Especiales
                    else power += 6; // Comunes
                }
            }
            return power;
        }

        public List<PitchSlotData> GetSlots()
        {
            return formationSlots;
        }
    }
}

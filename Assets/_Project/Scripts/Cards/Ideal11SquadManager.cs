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

        private void InitializeFormation()
        {
            if (formationSlots.Count > 0) return;

            formationSlots = new List<PitchSlotData>
            {
                // Delantera (3)
                new PitchSlotData { slotIndex = 0, positionCode = "DEL_EI", positionName = "Extremo Izq.", assignedCardId = "LD" }, // Luis Díaz
                new PitchSlotData { slotIndex = 1, positionCode = "DEL_DC", positionName = "Delantero Centro", assignedCardId = "EH" }, // Haaland
                new PitchSlotData { slotIndex = 2, positionCode = "DEL_ED", positionName = "Extremo Der.", assignedCardId = "VJ" }, // Vinicius

                // Mediocampo (3)
                new PitchSlotData { slotIndex = 3, positionCode = "MED_I", positionName = "Interior Izq.", assignedCardId = "PE" }, // Pedri
                new PitchSlotData { slotIndex = 4, positionCode = "MED_C", positionName = "Pivote", assignedCardId = "RO" }, // Rodri
                new PitchSlotData { slotIndex = 5, positionCode = "MED_D", positionName = "Interior Der.", assignedCardId = "JB" }, // Bellingham

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
            for (int i = 0; i < formationSlots.Count; i++)
            {
                if (PlayerPrefs.HasKey(PREF_SLOT_PREFIX + i))
                {
                    formationSlots[i].assignedCardId = PlayerPrefs.GetString(PREF_SLOT_PREFIX + i);
                }
            }
            Debug.Log($"<color=green>[Ideal11] Alineación cargada: {GetFilledSlotsCount()}/11 jugadores posicionados.</color>");
        }

        public void SaveSquad()
        {
            for (int i = 0; i < formationSlots.Count; i++)
            {
                PlayerPrefs.SetString(PREF_SLOT_PREFIX + i, formationSlots[i].assignedCardId);
            }
            PlayerPrefs.Save();
            OnSquadUpdated?.Invoke();
            Debug.Log("<color=cyan>[Ideal11] Alineación guardada con éxito.</color>");
        }

        public void AssignCardToSlot(int slotIndex, string cardId)
        {
            if (slotIndex >= 0 && slotIndex < formationSlots.Count)
            {
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

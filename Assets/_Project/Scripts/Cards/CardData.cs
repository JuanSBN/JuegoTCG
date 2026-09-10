using UnityEngine;

namespace JuegoTCG.Cards
{
    [CreateAssetMenu(fileName = "NewCardData", menuName = "JuegoTCG/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Información Básica")]
        public string cardId;
        public string playerName;
        public string teamName;
        public string position; // Delantero, Mediocampista, Defensor, Portero

        /// <summary>
        /// Línea táctica normalizada (POR, DEF, MED, DEL) calculada automáticamente.
        /// </summary>
        public TacticalPosition TacticalLine => TacticalPositionHelper.Normalize(position);

        [Header("Rareza y Colección")]
        public Rarity rarity;
        public string albumId;

        [Header("Arte Visual")]
        public Sprite defaultArt;
    }
}

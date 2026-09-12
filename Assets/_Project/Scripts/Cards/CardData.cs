using System;
using UnityEngine;

namespace JuegoTCG.Cards
{
    [Serializable]
    public struct CardStatsSummary
    {
        public string stat1Name;
        public int stat1Value;
        public string stat2Name;
        public int stat2Value;
        public string stat3Name;
        public int stat3Value;
        public string stat4Name;
        public int stat4Value;
    }

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

        [Header("Nacionalidad")]
        public string nationality = "España";
        public string countryCode = "ES"; // AR, ES, CO, FR, NO, PT, US, CI, RU, etc.

        [Header("Rareza y Colección")]
        public Rarity rarity;
        public string albumId;

        [Header("Arte Visual")]
        public Sprite defaultArt;

        #region Resolución de Data Packs (Cosmético)

        /// <summary>
        /// Nombre del jugador respetando Data Pack activo (o playerName por defecto).
        /// </summary>
        public string DisplayPlayerName => DataPackManager.GetPlayerName(cardId, playerName);

        /// <summary>
        /// Nombre del equipo respetando Data Pack activo (o teamName por defecto).
        /// </summary>
        public string DisplayTeamName => DataPackManager.GetTeamName(cardId, teamName);

        /// <summary>
        /// Posición textual respetando Data Pack activo (o position por defecto).
        /// </summary>
        public string DisplayPosition => DataPackManager.GetPosition(cardId, position);

        /// <summary>
        /// Nacionalidad inmutable (no modificable por Data Packs para mantener consistencia de filtros).
        /// </summary>
        public string DisplayNationality => nationality;

        /// <summary>
        /// Código ISO de país inmutable (no modificable por Data Packs para mantener consistencia de filtros).
        /// </summary>
        public string DisplayCountryCode => countryCode;

        /// <summary>
        /// Arte/Foto respetando Data Pack activo (o defaultArt por defecto).
        /// </summary>
        public Sprite DisplayArt => DataPackManager.GetCardArt(cardId, defaultArt);

        /// <summary>
        /// Iniciales del jugador (para avatars cuando no hay foto disponible).
        /// </summary>
        public string DisplayInitials
        {
            get
            {
                string name = DisplayPlayerName;
                if (string.IsNullOrEmpty(name)) return "FC";
                string[] parts = name.Trim().Split(' ');
                if (parts.Length == 1) return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
                return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
            }
        }

        #endregion

        [Header("Estadísticas de Jugador de Campo")]
        [Range(1, 99)] public int shooting = 50;   // Tiro (TIR)
        [Range(1, 99)] public int passing = 50;    // Pase (PAS)
        [Range(1, 99)] public int defending = 50;  // Defensa (DEF)
        [Range(1, 99)] public int dribbling = 50;  // Regate (REG)

        [Header("Estadísticas de Portero")]
        [Range(1, 99)] public int diving = 50;       // Estirada (EST)
        [Range(1, 99)] public int reflexes = 50;     // Reflejos (REF)
        [Range(1, 99)] public int handling = 50;     // Parada (PAR)
        [Range(1, 99)] public int positioning = 50;  // Colocación (COL)

        [Header("Media Global (OVR)")]
        [Tooltip("Si es 0, se calcula automáticamente mediante promedio ponderado según posición (estilo FIFA).")]
        [Range(0, 99)] public int manualOverall = 0;

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
                    // Portero: Reflejos 30%, Estirada 25%, Parada 25%, Colocación 20%
                    return Mathf.Clamp(Mathf.RoundToInt(reflexes * 0.30f + diving * 0.25f + handling * 0.25f + positioning * 0.20f), 1, 99);

                case TacticalPosition.DEF:
                    // Defensor: Defensa 50%, Pase 25%, Regate 20%, Tiro 5%
                    return Mathf.Clamp(Mathf.RoundToInt(defending * 0.50f + passing * 0.25f + dribbling * 0.20f + shooting * 0.05f), 1, 99);

                case TacticalPosition.MED:
                    // Mediocampista: Pase 35%, Regate 30%, Tiro 20%, Defensa 15%
                    return Mathf.Clamp(Mathf.RoundToInt(passing * 0.35f + dribbling * 0.30f + shooting * 0.20f + defending * 0.15f), 1, 99);

                case TacticalPosition.DEL:
                default:
                    // Delantero: Tiro 45%, Regate 30%, Pase 20%, Defensa 5%
                    return Mathf.Clamp(Mathf.RoundToInt(shooting * 0.45f + dribbling * 0.30f + passing * 0.20f + defending * 0.05f), 1, 99);
            }
        }

        /// <summary>
        /// Devuelve un resumen formateado de las 4 estadísticas correspondientes para renderizado ágil en UI.
        /// </summary>
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
}

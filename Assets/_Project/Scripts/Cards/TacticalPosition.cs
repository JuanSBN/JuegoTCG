using System;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Representa las 4 líneas maestras tácticas utilizadas en "MI 11 IDEAL".
    /// </summary>
    public enum TacticalPosition
    {
        POR, // Portero / Arquero
        DEF, // Defensa (Centrales, Laterales, Carrileros)
        MED, // Mediocentro (Pivotes, Interiores, Mediapuntas, Volantes)
        DEL  // Delantero (Delanteros Centro, Extremos, Segundas Puntas)
    }

    /// <summary>
    /// Helper para normalizar y mapear posiciones descriptivas o abreviadas a las 4 líneas tácticas maestras.
    /// </summary>
    public static class TacticalPositionHelper
    {
        /// <summary>
        /// Convierte cualquier texto descriptivo de posición ("Extremo Derecho", "Lateral", "Pivote", "GK")
        /// a su correspondiente línea maestra TacticalPosition (DEL, DEF, MED, POR).
        /// </summary>
        public static TacticalPosition Normalize(string rawPosition)
        {
            if (string.IsNullOrWhiteSpace(rawPosition)) return TacticalPosition.DEL;

            string p = rawPosition.Trim().ToUpperInvariant();

            // 1. Portero / Arquero / Goalkeeper
            if (p.Contains("POR") || p.Contains("ARQ") || p.Contains("GK") || p.Contains("GOAL") || p == "PO")
            {
                return TacticalPosition.POR;
            }

            // 2. Defensa (Central, Lateral, Zaguero, Carrilero, etc.)
            if (p.Contains("DEF") || p.Contains("LATERAL") || p.Contains("CENTRAL") || p.Contains("ZAGUERO") || 
                p.Contains("CARRILERO") || p == "DF" || p == "CB" || p == "LB" || p == "RB" || p == "LTD" || p == "LTI")
            {
                return TacticalPosition.DEF;
            }

            // 3. Mediocentro (Mediocampista, Volante, Pivote, Interior, Mediapunta, etc.)
            if (p.Contains("MED") || p.Contains("VOLANTE") || p.Contains("PIVOTE") || p.Contains("INTERIOR") || 
                p.Contains("ORGANIZADOR") || p == "MC" || p == "MCD" || p == "MCO" || p == "MF" || p == "CM" || p == "CAM" || p == "CDM")
            {
                return TacticalPosition.MED;
            }

            // 4. Delantero (Extremo Izquierdo, Extremo Derecho, Delantero Centro, Punta, etc.)
            if (p.Contains("DEL") || p.Contains("EXTREMO") || p.Contains("PUNTA") || p.Contains("ATACANTE") || 
                p.Contains("FORWARD") || p.Contains("STRIKER") || p.Contains("WINGER") ||
                p == "EI" || p == "ED" || p == "DC" || p == "SD" || p == "LW" || p == "RW" || p == "ST" || p == "CF")
            {
                return TacticalPosition.DEL;
            }

            // Por defecto, si no coincide con las anteriores, se asigna como Delantero
            return TacticalPosition.DEL;
        }

        /// <summary>
        /// Extrae la línea táctica a partir del código de slot de la cancha ("DEL_EI", "MED_C", "DEF_LI", "POR", etc.).
        /// </summary>
        public static TacticalPosition FromSlotCode(string slotCode)
        {
            if (string.IsNullOrWhiteSpace(slotCode)) return TacticalPosition.DEL;

            string code = slotCode.Trim().ToUpperInvariant();
            if (code.StartsWith("POR") || code.Contains("G1") || code.Contains("GK")) return TacticalPosition.POR;
            if (code.StartsWith("DEF") || code.Contains("D1") || code.Contains("D2") || code.Contains("D3") || code.Contains("D4")) return TacticalPosition.DEF;
            if (code.StartsWith("MED") || code.Contains("M1") || code.Contains("M2") || code.Contains("M3")) return TacticalPosition.MED;
            return TacticalPosition.DEL;
        }

        /// <summary>
        /// Código corto estándar de 3 letras ("POR", "DEF", "MED", "DEL").
        /// </summary>
        public static string GetCode(TacticalPosition pos)
        {
            return pos.ToString();
        }

        /// <summary>
        /// Nombre amigable de la posición ("Portero", "Defensa", "Mediocentro", "Delantero").
        /// </summary>
        public static string GetDisplayName(TacticalPosition pos)
        {
            switch (pos)
            {
                case TacticalPosition.POR: return "Portero";
                case TacticalPosition.DEF: return "Defensa";
                case TacticalPosition.MED: return "Mediocentro";
                case TacticalPosition.DEL: return "Delantero";
                default: return "Jugador";
            }
        }

        /// <summary>
        /// Título exacto para el modal de selección en Figma ("ELEGIR PORTERO", "ELEGIR DELANTERO", etc.).
        /// </summary>
        public static string GetModalTitle(TacticalPosition pos)
        {
            switch (pos)
            {
                case TacticalPosition.POR: return "ELEGIR PORTERO";
                case TacticalPosition.DEF: return "ELEGIR DEFENSA";
                case TacticalPosition.MED: return "ELEGIR MEDIOCENTRO";
                case TacticalPosition.DEL: return "ELEGIR DELANTERO";
                default: return "ELEGIR JUGADOR";
            }
        }

        /// <summary>
        /// Verifica si una carta pertenece a la línea táctica solicitada.
        /// </summary>
        public static bool Matches(TacticalPosition cardLine, TacticalPosition targetLine)
        {
            return cardLine == targetLine;
        }

        /// <summary>
        /// Verifica si una carta pertenece a la línea táctica solicitada (por texto descriptivo).
        /// </summary>
        public static bool Matches(string cardPosition, TacticalPosition targetLine)
        {
            return Normalize(cardPosition) == targetLine;
        }
    }
}

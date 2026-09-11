using System.Collections.Generic;
using UnityEngine;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Servicio centralizado y de alto rendimiento para la carga y caché de banderas nacionales.
    /// Busca los sprites en "Resources/Flags/{COUNTRY_CODE}".
    /// </summary>
    public static class CountryFlagService
    {
        private static readonly Dictionary<string, Sprite> flagCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Obtiene el sprite de la bandera correspondiente a un código ISO (ej: "AR", "ES", "CO").
        /// Utiliza caché en memoria para 0 consumo de CPU en listas y pantallas.
        /// </summary>
        public static Sprite GetFlag(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode)) return null;

            string code = countryCode.Trim().ToUpperInvariant();

            if (flagCache.TryGetValue(code, out var cachedSprite))
            {
                return cachedSprite;
            }

            Sprite loaded = Resources.Load<Sprite>($"Flags/{code}");
            flagCache[code] = loaded;

            return loaded;
        }

        /// <summary>
        /// Comprueba si existe la bandera en Resources/Flags/ para el código dado.
        /// </summary>
        public static bool HasFlag(string countryCode)
        {
            return GetFlag(countryCode) != null;
        }

        /// <summary>
        /// Limpia la caché en memoria (por ejemplo al cambiar de escena si se desea liberar memoria).
        /// </summary>
        public static void ClearCache()
        {
            flagCache.Clear();
        }
    }
}

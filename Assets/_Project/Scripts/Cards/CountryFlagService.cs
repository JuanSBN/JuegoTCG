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

        static CountryFlagService()
        {
            DataPackManager.OnDataPackReloaded += ClearCache;
        }

        /// <summary>
        /// Obtiene el sprite de la bandera correspondiente a un código ISO (ej: "AR", "ES", "CO").
        /// Utiliza caché en memoria para 0 consumo de CPU en listas y pantallas.
        /// Prioriza banderas personalizadas provistas por Data Packs activos.
        /// </summary>
        public static Sprite GetFlag(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode)) return null;

            string code = countryCode.Trim().ToUpperInvariant();

            if (flagCache.TryGetValue(code, out var cachedSprite))
            {
                return cachedSprite;
            }

            // 1. Intentar obtener bandera custom del Data Pack activo
            Sprite loaded = DataPackManager.GetCustomFlag(code);

            // 2. Si no hay bandera en el pack, cargar de Resources
            if (loaded == null)
            {
                loaded = Resources.Load<Sprite>($"Flags/{code}");
            }

            if (loaded != null)
            {
                flagCache[code] = loaded;
            }

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

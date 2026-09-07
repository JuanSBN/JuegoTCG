using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Administrador centralizado de Data Packs e imágenes de cartas.
    /// Resuelve las imágenes de jugadores consultando primero si hay un Data Pack instalado localmente,
    /// luego CardData.defaultArt, y retorna null si la carta no dispone de imagen (activando el diseño fallback de iniciales).
    /// </summary>
    public static class DataPackManager
    {
        private static readonly Dictionary<string, Sprite> runtimeArtCache = new Dictionary<string, Sprite>();
        private static string dataPackPhotosDir;

        public static string DataPackPhotosDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(dataPackPhotosDir))
                {
                    dataPackPhotosDir = Path.Combine(Application.persistentDataPath, "DataPacks", "Photos");
                }
                return dataPackPhotosDir;
            }
        }

        /// <summary>
        /// Obtiene el Sprite de arte para una carta según su cardId.
        /// Orden de resolución:
        /// 1. Caché en memoria.
        /// 2. Archivo en directorio de Data Packs del dispositivo (ej: card_10.png o card_10.jpg).
        /// 3. defaultArt del CardData (si existe).
        /// 4. null (fallback a iniciales con diseño estándar).
        /// </summary>
        public static Sprite GetCardArt(string cardId, Sprite defaultArt = null)
        {
            if (string.IsNullOrEmpty(cardId)) return defaultArt;

            // 1. Caché en memoria
            if (runtimeArtCache.TryGetValue(cardId, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            // 2. Data Pack local en almacenamiento persistente
            Sprite dataPackSprite = TryLoadSpriteFromDisk(cardId);
            if (dataPackSprite != null)
            {
                runtimeArtCache[cardId] = dataPackSprite;
                return dataPackSprite;
            }

            // 3. defaultArt predeterminado de ScriptableObject
            if (defaultArt != null)
            {
                runtimeArtCache[cardId] = defaultArt;
                return defaultArt;
            }

            return null;
        }

        /// <summary>
        /// Comprueba si la carta cuenta con un arte disponible (Data Pack o defaultArt).
        /// </summary>
        public static bool HasCardArt(string cardId, Sprite defaultArt = null)
        {
            return GetCardArt(cardId, defaultArt) != null;
        }

        /// <summary>
        /// Permite registrar o inyectar un Sprite en caliente para una carta (usado al descargar Data Packs).
        /// </summary>
        public static void RegisterRuntimeArt(string cardId, Sprite sprite)
        {
            if (string.IsNullOrEmpty(cardId) || sprite == null) return;
            runtimeArtCache[cardId] = sprite;
        }

        /// <summary>
        /// Limpia la caché en memoria (por ejemplo al cambiar de usuario o desinstalar un Data Pack).
        /// </summary>
        public static void ClearCache()
        {
            runtimeArtCache.Clear();
        }

        private static Sprite TryLoadSpriteFromDisk(string cardId)
        {
            try
            {
                string dir = DataPackPhotosDirectory;
                if (!Directory.Exists(dir)) return null;

                // Buscar por cardId con .png o .jpg
                string pngPath = Path.Combine(dir, $"{cardId}.png");
                string jpgPath = Path.Combine(dir, $"{cardId}.jpg");
                string targetPath = File.Exists(pngPath) ? pngPath : (File.Exists(jpgPath) ? jpgPath : null);

                if (string.IsNullOrEmpty(targetPath)) return null;

                byte[] fileData = File.ReadAllBytes(targetPath);
                if (fileData == null || fileData.Length == 0) return null;

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(fileData))
                {
                    texture.name = $"DataPack_Photo_{cardId}";
                    return Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DataPackManager] Error al cargar arte para '{cardId}' desde disco: {ex.Message}");
            }

            return null;
        }
    }
}

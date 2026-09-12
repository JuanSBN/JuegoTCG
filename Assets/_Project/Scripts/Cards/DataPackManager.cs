using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Administrador centralizado de Data Packs de la comunidad.
    /// Resuelve de forma no invasiva las sustituciones cosméticas (nombres reales, clubes,
    /// nacionalidades y fotos HD) sin alterar la integridad competitiva (estadísticas y OVR inmutables).
    /// </summary>
    public static class DataPackManager
    {
        private static readonly Dictionary<string, DataPackCardEntry> cardOverrides = new Dictionary<string, DataPackCardEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> runtimeArtCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> runtimeFlagCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private static DataPackManifest activeManifest = null;
        private static bool isInitialized = false;

        public static event Action OnDataPackReloaded;

        #region Directorios

        public static string RootDirectory => Path.Combine(Application.persistentDataPath, "DataPacks");
        public static string ActiveDirectory => Path.Combine(RootDirectory, "Active");
        public static string TempDirectory => Path.Combine(RootDirectory, "Temp");
        public static string ManifestFilePath => Path.Combine(ActiveDirectory, "manifest.json");
        public static string DatabaseFilePath => Path.Combine(ActiveDirectory, "database.json");

        public static string PhotosDirectory
        {
            get
            {
                string pLower = Path.Combine(ActiveDirectory, "photos");
                if (Directory.Exists(pLower)) return pLower;
                string pUpper = Path.Combine(ActiveDirectory, "Photos");
                if (Directory.Exists(pUpper)) return pUpper;
                return pLower;
            }
        }

        public static string FlagsDirectory
        {
            get
            {
                string fLower = Path.Combine(ActiveDirectory, "flags");
                if (Directory.Exists(fLower)) return fLower;
                string fUpper = Path.Combine(ActiveDirectory, "Flags");
                if (Directory.Exists(fUpper)) return fUpper;
                return fLower;
            }
        }

        #endregion

        #region Estado

        public static bool IsDataPackActive
        {
            get
            {
                EnsureInitialized();
                return activeManifest != null;
            }
        }

        public static DataPackManifest ActiveManifest
        {
            get
            {
                EnsureInitialized();
                return activeManifest;
            }
        }

        #endregion

        #region Inicialización y Carga

        public static void EnsureInitialized()
        {
            if (isInitialized) return;
            ReloadActiveDataPack();
        }

        public static void ReloadActiveDataPack()
        {
            ClearCache();
            cardOverrides.Clear();
            activeManifest = null;
            isInitialized = true;

            try
            {
                if (File.Exists(ManifestFilePath))
                {
                    string manifestJson = File.ReadAllText(ManifestFilePath);
                    activeManifest = JsonUtility.FromJson<DataPackManifest>(manifestJson);
                }

                if (File.Exists(DatabaseFilePath))
                {
                    string dbJson = File.ReadAllText(DatabaseFilePath);
                    DataPackDatabase db = JsonUtility.FromJson<DataPackDatabase>(dbJson);
                    if (db != null && db.cards != null)
                    {
                        foreach (var card in db.cards)
                        {
                            if (!string.IsNullOrWhiteSpace(card.cardId))
                            {
                                cardOverrides[card.cardId.Trim()] = card;
                            }
                        }
                    }
                }

                if (activeManifest != null)
                {
                    Debug.Log($"<color=green>[DataPackManager] Data Pack activo: '{activeManifest.title}' v{activeManifest.version} con {cardOverrides.Count} sustituciones.</color>");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DataPackManager] Error al cargar Data Pack activo: {ex.Message}");
                activeManifest = null;
                cardOverrides.Clear();
            }

            OnDataPackReloaded?.Invoke();
        }

        public static void ClearActiveDataPack()
        {
            try
            {
                if (Directory.Exists(ActiveDirectory))
                {
                    Directory.Delete(ActiveDirectory, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DataPackManager] Error al limpiar directorio Active: {ex.Message}");
            }

            ReloadActiveDataPack();
        }

        public static void ClearCache()
        {
            runtimeArtCache.Clear();
            runtimeFlagCache.Clear();
        }

        #endregion

        #region Resolución de Metadatos Cosméticos (Nombres, Equipos, Banderas)

        public static string GetPlayerName(string cardId, string defaultName)
        {
            if (string.IsNullOrEmpty(cardId)) return defaultName;
            EnsureInitialized();

            if (cardOverrides.TryGetValue(cardId, out var entry) && !string.IsNullOrWhiteSpace(entry.playerName))
            {
                return entry.playerName;
            }
            return defaultName;
        }

        public static string GetInitials(string cardId, string defaultInitials)
        {
            if (string.IsNullOrEmpty(cardId)) return defaultInitials;
            EnsureInitialized();

            if (cardOverrides.TryGetValue(cardId, out var entry) && !string.IsNullOrWhiteSpace(entry.initials))
            {
                return entry.initials;
            }
            return defaultInitials;
        }

        public static string GetTeamName(string cardId, string defaultTeam)
        {
            if (string.IsNullOrEmpty(cardId)) return defaultTeam;
            EnsureInitialized();

            if (cardOverrides.TryGetValue(cardId, out var entry) && !string.IsNullOrWhiteSpace(entry.teamName))
            {
                return entry.teamName;
            }
            return defaultTeam;
        }

        public static string GetPosition(string cardId, string defaultPos)
        {
            if (string.IsNullOrEmpty(cardId)) return defaultPos;
            EnsureInitialized();

            if (cardOverrides.TryGetValue(cardId, out var entry) && !string.IsNullOrWhiteSpace(entry.position))
            {
                return entry.position;
            }
            return defaultPos;
        }

        /// <summary>
        /// Las nacionalidades y códigos de país son inmutables por Data Packs
        /// para garantizar que los filtros por nación del álbum nunca se descalibren.
        /// </summary>
        public static string GetNationality(string cardId, string defaultNat)
        {
            return defaultNat;
        }

        /// <summary>
        /// El código de país es inmutable por Data Packs para mantener la coherencia
        /// de las búsquedas, colecciones y banderas asignadas a cada carta.
        /// </summary>
        public static string GetCountryCode(string cardId, string defaultCode)
        {
            return defaultCode;
        }

        #endregion

        #region Resolución de Arte (Fotos y Banderas)

        public static Sprite GetCardArt(string cardId, Sprite defaultArt = null)
        {
            if (string.IsNullOrEmpty(cardId)) return defaultArt;
            EnsureInitialized();

            if (runtimeArtCache.TryGetValue(cardId, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            Sprite dataPackSprite = TryLoadPhotoFromDisk(cardId);
            if (dataPackSprite != null)
            {
                runtimeArtCache[cardId] = dataPackSprite;
                return dataPackSprite;
            }

            if (defaultArt != null)
            {
                runtimeArtCache[cardId] = defaultArt;
                return defaultArt;
            }

            return null;
        }

        public static bool HasCardArt(string cardId, Sprite defaultArt = null)
        {
            return GetCardArt(cardId, defaultArt) != null;
        }

        public static Sprite GetCustomFlag(string countryCode, Sprite defaultFlag = null)
        {
            if (string.IsNullOrWhiteSpace(countryCode)) return defaultFlag;
            EnsureInitialized();

            string code = countryCode.Trim().ToUpperInvariant();
            if (runtimeFlagCache.TryGetValue(code, out Sprite cachedFlag) && cachedFlag != null)
            {
                return cachedFlag;
            }

            Sprite customFlag = TryLoadFlagFromDisk(code);
            if (customFlag != null)
            {
                runtimeFlagCache[code] = customFlag;
                return customFlag;
            }

            return defaultFlag;
        }

        public static void RegisterRuntimeArt(string cardId, Sprite sprite)
        {
            if (string.IsNullOrEmpty(cardId) || sprite == null) return;
            runtimeArtCache[cardId] = sprite;
        }

        private static Sprite TryLoadPhotoFromDisk(string cardId)
        {
            try
            {
                string dir = PhotosDirectory;
                if (!Directory.Exists(dir)) return null;

                string[] extensions = new string[] { ".png", ".jpg", ".jpeg", ".webp" };
                foreach (var ext in extensions)
                {
                    string path = Path.Combine(dir, $"{cardId}{ext}");
                    if (File.Exists(path))
                    {
                        byte[] fileData = File.ReadAllBytes(path);
                        if (fileData != null && fileData.Length > 0)
                        {
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
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DataPackManager] Error cargando foto '{cardId}': {ex.Message}");
            }

            return null;
        }

        private static Sprite TryLoadFlagFromDisk(string countryCode)
        {
            try
            {
                string dir = FlagsDirectory;
                if (!Directory.Exists(dir)) return null;

                string path = Path.Combine(dir, $"{countryCode}.png");
                if (File.Exists(path))
                {
                    byte[] fileData = File.ReadAllBytes(path);
                    if (fileData != null && fileData.Length > 0)
                    {
                        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (texture.LoadImage(fileData))
                        {
                            texture.name = $"DataPack_Flag_{countryCode}";
                            return Sprite.Create(
                                texture,
                                new Rect(0, 0, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f),
                                100f
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DataPackManager] Error cargando bandera '{countryCode}': {ex.Message}");
            }

            return null;
        }

        #endregion
    }
}

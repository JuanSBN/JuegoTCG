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
        private static readonly Dictionary<string, Sprite> runtimeBgCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

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

        public static string BackgroundsDirectory
        {
            get
            {
                string bLower = Path.Combine(ActiveDirectory, "backgrounds");
                if (Directory.Exists(bLower)) return bLower;
                string bUpper = Path.Combine(ActiveDirectory, "Backgrounds");
                if (Directory.Exists(bUpper)) return bUpper;
                return bLower;
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

        /// <summary>
        /// Recarga el data pack activo desde disco. Alias para ReloadActiveDataPack.
        /// </summary>
        public static void ReloadFromDisk() => ReloadActiveDataPack();

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
            runtimeBgCache.Clear();
        }

        #endregion

        #region Resolución de Metadatos Cosméticos (Nombres, Equipos, Banderas)

        public static string GetPlayerName(string cardId, string defaultName = "")
        {
            if (string.IsNullOrEmpty(cardId)) return defaultName;
            EnsureInitialized();

            if (cardOverrides.TryGetValue(cardId, out var entry) && !string.IsNullOrWhiteSpace(entry.playerName))
            {
                return entry.playerName;
            }
            return defaultName;
        }

        public static string GetInitials(string cardId, string defaultInitials = "")
        {
            if (string.IsNullOrEmpty(cardId)) return defaultInitials;
            EnsureInitialized();

            if (cardOverrides.TryGetValue(cardId, out var entry) && !string.IsNullOrWhiteSpace(entry.initials))
            {
                return entry.initials;
            }
            return defaultInitials;
        }

        /// <summary>
        /// Por seguridad legal y protección de marcas de clubes, los Data Packs NO modifican equipos.
        /// Devuelve siempre el valor por defecto configurado en el juego.
        /// </summary>
        public static string GetTeamName(string cardId, string defaultTeam = "")
        {
            return defaultTeam;
        }

        public static string GetPosition(string cardId, string defaultPos = "")
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
        public static string GetNationality(string cardId, string defaultNat = "")
        {
            return defaultNat;
        }

        /// <summary>
        /// El código de país es inmutable por Data Packs para mantener la coherencia
        /// de las búsquedas, colecciones y banderas asignadas a cada carta.
        /// </summary>
        public static string GetCountryCode(string cardId, string defaultCode = "")
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

        #region Resolución de Fondos (Backgrounds)

        /// <summary>
        /// Obtiene el fondo de la carta provisto por el Data Pack (ej. fondo Champions, estadio o torneo).
        /// Busca primero un fondo individual específico ('backgrounds/{cardId}.png') y luego el fondo global del pack ('background.png' o 'backgrounds/default.png').
        /// </summary>
        public static Sprite GetCardBackground(string cardId, Sprite defaultBackground = null)
        {
            string key = !string.IsNullOrEmpty(cardId) ? cardId : "default";
            EnsureInitialized();

            if (runtimeBgCache.TryGetValue(key, out Sprite cachedBg) && cachedBg != null)
            {
                return cachedBg;
            }

            // 1. Fondo específico de la carta: backgrounds/{cardId}.*
            Sprite specificBg = TryLoadBackgroundFromDisk(key);
            if (specificBg != null)
            {
                runtimeBgCache[key] = specificBg;
                return specificBg;
            }

            // 2. Fondo global del paquete en caché
            if (runtimeBgCache.TryGetValue("__global__", out Sprite cachedGlobal) && cachedGlobal != null)
            {
                return cachedGlobal;
            }

            // 3. Cargar fondo global del paquete desde disco: backgrounds/default.* o Active/background.*
            Sprite globalBg = TryLoadGlobalBackgroundFromDisk();
            if (globalBg != null)
            {
                runtimeBgCache["__global__"] = globalBg;
                runtimeBgCache[key] = globalBg;
                return globalBg;
            }

            if (defaultBackground != null)
            {
                runtimeBgCache[key] = defaultBackground;
                return defaultBackground;
            }

            return null;
        }

        public static bool HasCardBackground(string cardId)
        {
            return GetCardBackground(cardId) != null;
        }

        private static Sprite TryLoadBackgroundFromDisk(string cardId)
        {
            try
            {
                string dir = BackgroundsDirectory;
                if (!Directory.Exists(dir)) return null;

                string[] extensions = new string[] { ".png", ".jpg", ".jpeg", ".webp" };
                foreach (var ext in extensions)
                {
                    string path = Path.Combine(dir, $"{cardId}{ext}");
                    if (File.Exists(path))
                    {
                        return LoadSpriteFromFile(path, $"DataPack_Bg_{cardId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DataPackManager] Error cargando fondo '{cardId}': {ex.Message}");
            }

            return null;
        }

        private static Sprite TryLoadGlobalBackgroundFromDisk()
        {
            try
            {
                string[] extensions = new string[] { ".png", ".jpg", ".jpeg", ".webp" };

                // A. Buscar en la raíz de Active/ (background.png, Background.png, bg.png)
                foreach (var ext in extensions)
                {
                    string rootPathLower = Path.Combine(ActiveDirectory, $"background{ext}");
                    if (File.Exists(rootPathLower)) return LoadSpriteFromFile(rootPathLower, "DataPack_GlobalBg");

                    string rootPathUpper = Path.Combine(ActiveDirectory, $"Background{ext}");
                    if (File.Exists(rootPathUpper)) return LoadSpriteFromFile(rootPathUpper, "DataPack_GlobalBg");

                    string rootPathBg = Path.Combine(ActiveDirectory, $"bg{ext}");
                    if (File.Exists(rootPathBg)) return LoadSpriteFromFile(rootPathBg, "DataPack_GlobalBg");
                }

                // B. Buscar dentro de backgrounds/ (default.*, bg.*, background.*)
                string dir = BackgroundsDirectory;
                if (Directory.Exists(dir))
                {
                    string[] globalNames = new string[] { "default", "Default", "bg", "Bg", "background", "Background" };
                    foreach (var name in globalNames)
                    {
                        foreach (var ext in extensions)
                        {
                            string path = Path.Combine(dir, $"{name}{ext}");
                            if (File.Exists(path))
                            {
                                return LoadSpriteFromFile(path, "DataPack_GlobalBg");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DataPackManager] Error cargando fondo global: {ex.Message}");
            }

            return null;
        }

        private static Sprite LoadSpriteFromFile(string filePath, string spriteName)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                if (fileData != null && fileData.Length > 0)
                {
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (texture.LoadImage(fileData))
                    {
                        texture.name = spriteName;
                        return Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f),
                            100f
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DataPackManager] Error creando Sprite desde '{filePath}': {ex.Message}");
            }
            return null;
        }

        #endregion
    }
}

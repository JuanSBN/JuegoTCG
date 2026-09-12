using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Servicio de consulta y sincronización del catálogo remoto de Data Packs de la comunidad.
    /// Consulta la colección 'datapacks_catalog' en Cloud Firestore (estilo World Soccer Champs)
    /// permitiendo gestionar los packs recomendados desde la consola de Firebase sin tocar el código.
    /// </summary>
    public static class DataPackIndexService
    {
        public static string FirestoreCatalogUrl => 
            $"https://firestore.googleapis.com/v1/projects/{Networking.FirebaseRestClient.ProjectId}/databases/(default)/documents/datapacks_catalog";

        public static string CachedIndexPath => Path.Combine(DataPackManager.RootDirectory, "datapacks_index.json");

        private static List<DataPackIndexItem> cachedItems = null;

        public static event Action<List<DataPackIndexItem>> OnIndexLoaded;

        /// <summary>
        /// Obtiene la lista actual en memoria o desde la caché en disco si existe.
        /// </summary>
        public static List<DataPackIndexItem> GetCachedOrFallbackList()
        {
            if (cachedItems != null && cachedItems.Count > 0)
            {
                return cachedItems;
            }

            // 1. Intentar leer desde caché en disco
            if (File.Exists(CachedIndexPath))
            {
                try
                {
                    string json = File.ReadAllText(CachedIndexPath);
                    DataPackIndexRoot root = JsonUtility.FromJson<DataPackIndexRoot>(json);
                    if (root != null && root.packs != null && root.packs.Count > 0)
                    {
                        cachedItems = root.packs;
                        return cachedItems;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DataPackIndexService] Error leyendo caché local de catálogo: {ex.Message}");
                }
            }

            // 2. Fallback predeterminado si no hay conexión ni caché
            cachedItems = GetDefaultFallbackList();
            return cachedItems;
        }

        /// <summary>
        /// Corutina para descargar el catálogo remoto desde Firestore o URL personalizada con fallback automático.
        /// </summary>
        public static IEnumerator FetchIndexRoutine(Action<List<DataPackIndexItem>> onSuccess, Action<string> onError, string customUrl = null)
        {
            string url = !string.IsNullOrWhiteSpace(customUrl) ? customUrl : FirestoreCatalogUrl;

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 8;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string responseText = req.downloadHandler.text;
                        List<DataPackIndexItem> parsedItems = null;

                        // 1. Intentar parsear como respuesta Firestore REST
                        if (responseText.Contains("\"document\"") || responseText.Contains("\"documents\""))
                        {
                            parsedItems = ParseFirestoreCatalog(responseText);
                        }

                        // 2. Si no era Firestore o no trajo items, intentar formato JSON plano (DataPackIndexRoot)
                        if (parsedItems == null || parsedItems.Count == 0)
                        {
                            DataPackIndexRoot root = JsonUtility.FromJson<DataPackIndexRoot>(responseText);
                            if (root != null && root.packs != null && root.packs.Count > 0)
                            {
                                parsedItems = root.packs;
                            }
                        }

                        if (parsedItems != null && parsedItems.Count > 0)
                        {
                            cachedItems = parsedItems;

                            // Guardar en disco para modo offline
                            try
                            {
                                if (!Directory.Exists(DataPackManager.RootDirectory))
                                {
                                    Directory.CreateDirectory(DataPackManager.RootDirectory);
                                }

                                DataPackIndexRoot saveRoot = new DataPackIndexRoot { packs = cachedItems };
                                File.WriteAllText(CachedIndexPath, JsonUtility.ToJson(saveRoot, true));
                            }
                            catch (Exception saveEx)
                            {
                                Debug.LogWarning($"[DataPackIndexService] No se pudo guardar caché local: {saveEx.Message}");
                            }

                            OnIndexLoaded?.Invoke(cachedItems);
                            onSuccess?.Invoke(cachedItems);
                            yield break;
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Debug.LogWarning($"[DataPackIndexService] Error deserializando catálogo remoto: {parseEx.Message}");
                    }
                }
                else
                {
                    Debug.Log($"[DataPackIndexService] Catálogo remoto no disponible ({req.error}). Usando catálogo local/fallback.");
                }
            }

            // Fallback en caso de error de red, colección vacía en Firestore o modo offline
            List<DataPackIndexItem> fallback = GetCachedOrFallbackList();
            onSuccess?.Invoke(fallback);
            onError?.Invoke("Catálogo local cargado correctamente.");
        }

        private static List<DataPackIndexItem> ParseFirestoreCatalog(string json)
        {
            var list = new List<DataPackIndexItem>();
            if (string.IsNullOrEmpty(json)) return list;

            var docIndices = new List<int>();
            int idx = json.IndexOf("\"name\":");
            while (idx >= 0)
            {
                docIndices.Add(idx);
                idx = json.IndexOf("\"name\":", idx + 7);
            }

            for (int i = 0; i < docIndices.Count; i++)
            {
                int start = docIndices[i];
                int end = (i + 1 < docIndices.Count) ? docIndices[i + 1] : json.Length;
                string block = json.Substring(start, end - start);

                string title = ExtractFieldString(block, "title");
                string downloadUrl = ExtractFieldString(block, "downloadUrl");
                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(downloadUrl)) continue;

                list.Add(new DataPackIndexItem
                {
                    id = ExtractFieldString(block, "id"),
                    title = title,
                    author = ExtractFieldString(block, "author"),
                    version = ExtractFieldString(block, "version"),
                    description = ExtractFieldString(block, "description"),
                    downloadUrl = downloadUrl,
                    sizeMb = ExtractFieldString(block, "sizeMb"),
                    cardsCount = ExtractFieldInt(block, "cardsCount"),
                    isRecommended = ExtractFieldBool(block, "isRecommended"),
                    bannerColor = ExtractFieldString(block, "bannerColor")
                });
            }

            return list;
        }

        private static string ExtractFieldString(string block, string fieldName)
        {
            var m = System.Text.RegularExpressions.Regex.Match(block, $"\"{fieldName}\"\\s*:\\s*\\{{\\s*\"stringValue\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : "";
        }

        private static int ExtractFieldInt(string block, string fieldName)
        {
            var m = System.Text.RegularExpressions.Regex.Match(block, $"\"{fieldName}\"\\s*:\\s*\\{{\\s*\"integerValue\"\\s*:\\s*\"?([0-9]+)\"?");
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        private static bool ExtractFieldBool(string block, string fieldName)
        {
            var m = System.Text.RegularExpressions.Regex.Match(block, $"\"{fieldName}\"\\s*:\\s*\\{{\\s*\"booleanValue\"\\s*:\\s*(true|false)");
            return m.Success && bool.TryParse(m.Groups[1].Value, out bool b) && b;
        }

        /// <summary>
        /// Catálogo por defecto integrado para cuando no haya conexión de red disponible.
        /// </summary>
        private static List<DataPackIndexItem> GetDefaultFallbackList()
        {
            return new List<DataPackIndexItem>
            {
                new DataPackIndexItem
                {
                    id = "official_real_names_pack",
                    title = "Real Names & Photos Community Pack",
                    author = "Comunidad TCG",
                    version = "1.0.0",
                    description = "Sustituye los nombres ficticios por futbolistas reales, añade fotos HD y banderas nacionales oficiales.",
                    downloadUrl = "https://raw.githubusercontent.com/JuanSBN/JuegoTCG/main/DataPacks/datapack_real_names_v1.zip",
                    sizeMb = "12.4 MB",
                    cardsCount = 18,
                    isRecommended = true,
                    bannerColor = "#FFB300"
                },
                new DataPackIndexItem
                {
                    id = "legends_retro_pack",
                    title = "Legends & Icons Visual Edition",
                    author = "RetroTCG Fans",
                    version = "0.9.0",
                    description = "Versión estética de leyendas mundiales y fotos clásicas de época.",
                    downloadUrl = "https://raw.githubusercontent.com/JuanSBN/JuegoTCG/main/DataPacks/datapack_legends_v1.zip",
                    sizeMb = "8.1 MB",
                    cardsCount = 12,
                    isRecommended = false,
                    bannerColor = "#00B4D8"
                }
            };
        }
    }
}

using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Gestor de descarga, descompresión y despliegue atómico de Data Packs.
    /// Garantiza que nunca quede una instalación corrupta mediante swap atómico en disco.
    /// </summary>
    public class DataPackInstaller : MonoBehaviour
    {
        private static DataPackInstaller instance;

        public static DataPackInstaller Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = FindFirstObjectByType<DataPackInstaller>();
                    if (existing != null)
                    {
                        instance = existing;
                    }
                    else
                    {
                        GameObject go = new GameObject("DataPackInstaller");
                        instance = go.AddComponent<DataPackInstaller>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public bool IsBusy { get; private set; } = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Descarga un archivo .zip de un Data Pack desde una URL y lo instala atómicamente.
        /// </summary>
        public void InstallFromUrl(string url, Action<float, string> onProgress, Action<bool, string> onComplete)
        {
            if (IsBusy)
            {
                onComplete?.Invoke(false, "Hay otra operación de Data Pack en progreso.");
                return;
            }

            StartCoroutine(DownloadAndInstallRoutine(url, onProgress, onComplete));
        }

        /// <summary>
        /// Instala un Data Pack desde un archivo .zip local en el almacenamiento del dispositivo.
        /// </summary>
        public void InstallFromLocalZip(string zipFilePath, Action<float, string> onProgress, Action<bool, string> onComplete)
        {
            if (IsBusy)
            {
                onComplete?.Invoke(false, "Hay otra operación de Data Pack en progreso.");
                return;
            }

            StartCoroutine(ExtractAndApplyRoutine(zipFilePath, onProgress, onComplete, isTempZip: false));
        }

        /// <summary>
        /// Elimina el Data Pack activo y restaura las cartas originales por defecto.
        /// </summary>
        public void UninstallActivePack(Action<bool, string> onComplete)
        {
            try
            {
                DataPackManager.ClearActiveDataPack();
                onComplete?.Invoke(true, null);
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(false, ex.Message);
            }
        }

        private IEnumerator DownloadAndInstallRoutine(string url, Action<float, string> onProgress, Action<bool, string> onComplete)
        {
            IsBusy = true;
            onProgress?.Invoke(0f, "Conectando al servidor...");

            string tempDir = Path.Combine(DataPackManager.TempDirectory, "download");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            string tempZipPath = Path.Combine(tempDir, "pack_temp.zip");
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.downloadHandler = new DownloadHandlerFile(tempZipPath);
                var op = req.SendWebRequest();

                while (!op.isDone)
                {
                    float p = Mathf.Clamp01(req.downloadProgress * 0.7f); // 0% a 70% para la descarga
                    onProgress?.Invoke(p, $"Descargando Data Pack... {Mathf.RoundToInt(p * 100)}%");
                    yield return null;
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    IsBusy = false;
                    if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                    onComplete?.Invoke(false, $"Error de descarga: {req.error}");
                    yield break;
                }
            }

            // Proceder con descompresión e instalación
            yield return ExtractAndApplyRoutine(tempZipPath, onProgress, onComplete, isTempZip: true);
        }

        private IEnumerator ExtractAndApplyRoutine(string zipFilePath, Action<float, string> onProgress, Action<bool, string> onComplete, bool isTempZip)
        {
            IsBusy = true;
            string extractDir = Path.Combine(DataPackManager.TempDirectory, "extracted");

            try
            {
                if (!File.Exists(zipFilePath))
                {
                    throw new FileNotFoundException("Archivo .zip no encontrado en la ruta especificada.", zipFilePath);
                }

                onProgress?.Invoke(0.72f, "Preparando descompresión...");

                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }
                Directory.CreateDirectory(extractDir);

                // Descompresión segura con validación anti-ZipSlip
                using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
                {
                    int totalEntries = archive.Entries.Count;
                    int processed = 0;

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destPath = Path.GetFullPath(Path.Combine(extractDir, entry.FullName));
                        string fullExtractDir = Path.GetFullPath(extractDir);

                        if (!destPath.StartsWith(fullExtractDir, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("Entrada ZIP inválida detectada.");
                        }

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destPath);
                        }
                        else
                        {
                            string dirName = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                            {
                                Directory.CreateDirectory(dirName);
                            }
                            entry.ExtractToFile(destPath, true);
                        }

                        processed++;
                        if (processed % 10 == 0 || processed == totalEntries)
                        {
                            float p = 0.72f + (0.18f * ((float)processed / totalEntries));
                            onProgress?.Invoke(p, $"Extrayendo archivos ({processed}/{totalEntries})...");
                        }
                    }
                }

                onProgress?.Invoke(0.92f, "Verificando estructura del paquete...");

                // Normalización de carpetas si el zip venía empaquetado dentro de una subcarpeta raíz
                string resolvedRootDir = extractDir;
                if (!File.Exists(Path.Combine(extractDir, "manifest.json")))
                {
                    string[] subdirs = Directory.GetDirectories(extractDir);
                    if (subdirs.Length == 1 && File.Exists(Path.Combine(subdirs[0], "manifest.json")))
                    {
                        resolvedRootDir = subdirs[0];
                    }
                }

                string manifestFile = Path.Combine(resolvedRootDir, "manifest.json");
                string databaseFile = Path.Combine(resolvedRootDir, "database.json");

                if (!File.Exists(manifestFile))
                {
                    throw new Exception("El paquete no contiene un 'manifest.json' válido.");
                }

                if (!File.Exists(databaseFile))
                {
                    throw new Exception("El paquete no contiene un 'database.json' válido.");
                }

                // Validar JSON sintáctico
                string manifestJson = File.ReadAllText(manifestFile);
                DataPackManifest manifest = JsonUtility.FromJson<DataPackManifest>(manifestJson);
                if (manifest == null || string.IsNullOrWhiteSpace(manifest.title))
                {
                    throw new Exception("El 'manifest.json' del paquete está corrupto o mal formado.");
                }

                string dbJson = File.ReadAllText(databaseFile);
                DataPackDatabase db = JsonUtility.FromJson<DataPackDatabase>(dbJson);
                if (db == null)
                {
                    throw new Exception("El 'database.json' del paquete está corrupto o mal formado.");
                }

                onProgress?.Invoke(0.96f, "Aplicando cambios atómicamente...");

                // Swap atómico
                string activeDir = DataPackManager.ActiveDirectory;
                if (Directory.Exists(activeDir))
                {
                    Directory.Delete(activeDir, true);
                }

                if (resolvedRootDir == extractDir)
                {
                    Directory.Move(extractDir, activeDir);
                }
                else
                {
                    Directory.Move(resolvedRootDir, activeDir);
                    if (Directory.Exists(extractDir))
                    {
                        Directory.Delete(extractDir, true);
                    }
                }

                // Limpieza del archivo ZIP temporal si se descargó de internet
                if (isTempZip && File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath);
                }

                // Recargar DataPackManager
                DataPackManager.ReloadActiveDataPack();

                onProgress?.Invoke(1.0f, "¡Data Pack instalado con éxito!");
                onComplete?.Invoke(true, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataPackInstaller] Error durante la instalación: {ex.Message}");

                // Limpieza de seguridad
                try
                {
                    if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                    if (isTempZip && File.Exists(zipFilePath)) File.Delete(zipFilePath);
                }
                catch { }

                onComplete?.Invoke(false, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }

            yield return null;
        }
    }
}

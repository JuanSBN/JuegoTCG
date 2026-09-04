using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Utilidad centralizada para descargar, almacenar en caché y aplicar el avatar del usuario
    /// tanto en memoria como en disco local (Application.persistentDataPath).
    /// Se integra a la perfección con UI Toolkit (VisualElement y StyleBackground).
    /// </summary>
    public static class UserAvatarLoader
    {
        private static Texture2D cachedAvatarTexture = null;
        private static string cachedForUrl = null;
        private static bool isDownloading = false;

        public static event Action<Texture2D> OnAvatarLoaded;

        private static string LocalDiskPath => Path.Combine(Application.persistentDataPath, "user_avatar_cache.png");

        /// <summary>
        /// Aplica de forma síncrona o asíncrona la foto de perfil en el contenedor circular del avatar.
        /// Si no hay avatar disponible, restaura el ícono de silueta por defecto.
        /// </summary>
        public static void LoadAvatar(MonoBehaviour runner, VisualElement avatarCircle, VisualElement avatarIcon = null)
        {
            if (avatarCircle == null) return;

            string photoUrl = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.PhotoUrl : "";

            // 1. Si no hay URL de foto (anónimo o sin foto), mostrar silueta por defecto
            if (string.IsNullOrEmpty(photoUrl))
            {
                ShowDefaultIcon(avatarCircle, avatarIcon);
                return;
            }

            // 2. Si ya está cargado en caché de memoria y coincide la URL
            if (cachedAvatarTexture != null && cachedForUrl == photoUrl)
            {
                ApplyTexture(avatarCircle, avatarIcon, cachedAvatarTexture);
                return;
            }

            // 3. Revisar caché en disco local (Application.persistentDataPath)
            if (File.Exists(LocalDiskPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(LocalDiskPath);
                    if (bytes != null && bytes.Length > 0)
                    {
                        Texture2D diskTexture = new Texture2D(2, 2);
                        if (diskTexture.LoadImage(bytes))
                        {
                            cachedAvatarTexture = diskTexture;
                            cachedForUrl = photoUrl;
                            ApplyTexture(avatarCircle, avatarIcon, diskTexture);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UserAvatarLoader] Error leyendo caché local de avatar: {ex.Message}");
                }
            }

            // 4. Descargar vía red asíncrona
            if (runner != null && runner.isActiveAndEnabled && !isDownloading)
            {
                runner.StartCoroutine(DownloadAvatarRoutine(photoUrl, avatarCircle, avatarIcon));
            }
        }

        private static IEnumerator DownloadAvatarRoutine(string url, VisualElement avatarCircle, VisualElement avatarIcon)
        {
            isDownloading = true;
            Debug.Log($"<color=cyan>[UserAvatarLoader] Descargando foto de perfil desde: {url}</color>");

            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(req);
                    if (downloadedTexture != null)
                    {
                        cachedAvatarTexture = downloadedTexture;
                        cachedForUrl = url;

                        // Guardar en disco para cargas instantáneas posteriores / offline
                        try
                        {
                            byte[] pngBytes = downloadedTexture.EncodeToPNG();
                            if (pngBytes != null)
                            {
                                File.WriteAllBytes(LocalDiskPath, pngBytes);
                                Debug.Log($"<color=green>[UserAvatarLoader] Avatar guardado en caché local: {LocalDiskPath}</color>");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[UserAvatarLoader] Error al guardar caché de avatar en disco: {ex.Message}");
                        }

                        ApplyTexture(avatarCircle, avatarIcon, downloadedTexture);
                        OnAvatarLoaded?.Invoke(downloadedTexture);
                    }
                }
                else
                {
                    Debug.LogWarning($"[UserAvatarLoader] Error al descargar foto de perfil ({req.error}). Mostrando ícono por defecto.");
                    ShowDefaultIcon(avatarCircle, avatarIcon);
                }
            }

            isDownloading = false;
        }

        public static void ApplyTexture(VisualElement avatarCircle, VisualElement avatarIcon, Texture2D texture)
        {
            if (avatarCircle == null || texture == null) return;

            avatarCircle.style.backgroundImage = new StyleBackground(Background.FromTexture2D(texture));
            avatarCircle.style.unityBackgroundImageTintColor = Color.white;

            if (avatarIcon != null)
            {
                avatarIcon.style.display = DisplayStyle.None;
            }
        }

        public static void ShowDefaultIcon(VisualElement avatarCircle, VisualElement avatarIcon)
        {
            if (avatarCircle != null)
            {
                avatarCircle.style.backgroundImage = StyleKeyword.Null;
            }

            if (avatarIcon != null)
            {
                avatarIcon.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Borra la caché de avatar en memoria y disco (útil al cerrar sesión).
        /// </summary>
        public static void ClearCache()
        {
            cachedAvatarTexture = null;
            cachedForUrl = null;
            try
            {
                if (File.Exists(LocalDiskPath))
                {
                    File.Delete(LocalDiskPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UserAvatarLoader] Error borrando archivo de caché de avatar: {ex.Message}");
            }
        }
    }
}

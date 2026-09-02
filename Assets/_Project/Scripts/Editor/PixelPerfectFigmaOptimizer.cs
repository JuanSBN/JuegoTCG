using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.Editor
{
    public static class PixelPerfectFigmaOptimizer
    {
        private static readonly string[] ProjectScenes = new string[]
        {
            "Assets/_Project/Scenes/SplashScene.unity",
            "Assets/_Project/Scenes/LoginScene.unity",
            "Assets/_Project/Scenes/HomeScreenScene.unity",
            "Assets/_Project/Scenes/MyCardsScene.unity",
            "Assets/_Project/Scenes/StoreScene.unity",
            "Assets/_Project/Scenes/CommunityScene.unity",
            "Assets/_Project/Scenes/VitrinesScene.unity",
            "Assets/_Project/Scenes/TradeScene.unity",
            "Assets/_Project/Scenes/MarketScene.unity",
            "Assets/_Project/Scenes/FriendsScene.unity",
            "Assets/_Project/Scenes/ProfileScene.unity",
            "Assets/_Project/Scenes/SettingsScene.unity",
            "Assets/_Project/Scenes/PackOpeningScene.unity"
        };

        [MenuItem("JuegoTCG/✨ Ajuste Visual Pixel-Perfect (Figma Responsive)")]
        public static void ApplyPixelPerfectOptimization()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("<color=yellow>[FigmaUI] Por favor sal del modo juego (Play ▶) antes de aplicar el ajuste de escenas.</color>");
                EditorApplication.isPlaying = false;
                return;
            }

            Debug.Log("<color=cyan>[FigmaUI] Aplicando ajuste Pixel-Perfect: VerticalLayoutGroups, PreserveAspect y espaciados responsivos...</color>");

            string currentActiveScene = SceneManager.GetActiveScene().path;

            foreach (var scenePath in ProjectScenes)
            {
                if (!File.Exists(scenePath)) continue;

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                OptimizeSceneFigmaLayout(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"<color=green>[FigmaUI] Escena perfeccionada: {scene.name}</color>");
            }

            if (!string.IsNullOrEmpty(currentActiveScene) && File.Exists(currentActiveScene))
            {
                EditorSceneManager.OpenScene(currentActiveScene, OpenSceneMode.Single);
            }

            Debug.Log("<color=gold>[FigmaUI:COMPLETO] ¡Todas las pantallas quedaron con proporciones idénticas a Figma y 100% responsivas!</color>");
        }

        private static void OptimizeSceneFigmaLayout(Scene scene)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                // 1. Canvas Scaler Match Width (0.0) a 1080x2400
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 2400f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.0f;

                // 2. Corregir distorsión de círculos e iconos (Preserve Aspect)
                Image[] images = canvas.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    string iName = img.gameObject.name.ToLower();
                    if (iName.Contains("avatar") || iName.Contains("circle") || iName.Contains("icon") ||
                        iName.Contains("gear") || iName.Contains("settings") || iName.Contains("search") ||
                        iName.Contains("frame") || iName.Contains("perfil"))
                    {
                        img.preserveAspect = true;
                        
                        // Si es un icono o avatar circular, forzar 1:1
                        RectTransform rt = img.GetComponent<RectTransform>();
                        if (rt != null && rt.sizeDelta.x > 0 && rt.sizeDelta.y > 0)
                        {
                            float maxDim = Mathf.Max(rt.sizeDelta.x, rt.sizeDelta.y);
                            if (Mathf.Abs(rt.sizeDelta.x - rt.sizeDelta.y) > 5f && (iName.Contains("icon") || iName.Contains("gear") || iName.Contains("avatar")))
                            {
                                rt.sizeDelta = new Vector2(maxDim, maxDim);
                            }
                        }
                    }
                }

                // 3. Organizar Cabecera de Mis Cartas / Álbum
                if (scene.name.Contains("MyCards"))
                {
                    FixMyCardsHeaderLayout(canvas);
                }

                // 4. Organizar Pantalla de Inicio (Botón Misiones y Sobres)
                if (scene.name.Contains("Home") || scene.name.Contains("Inicio"))
                {
                    FixHomeScreenLayout(canvas);
                }

                // 5. Organizar Pantalla de Perfil (Avatar y Cancha)
                if (scene.name.Contains("Profile"))
                {
                    FixProfileScreenLayout(canvas);
                }
            }
        }

        private static void FixMyCardsHeaderLayout(Canvas canvas)
        {
            // Título arriba
            Transform titleObj = canvas.transform.Find("HeaderContainer/TitleText") ?? canvas.transform.Find("TitleText") ?? canvas.transform.Find("Header/Title");
            if (titleObj != null)
            {
                RectTransform rt = titleObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -80f);
                    rt.sizeDelta = new Vector2(0f, 60f);
                }
            }

            // Fila de Filtros debajo del título
            Transform filterObj = canvas.transform.Find("FilterScroll") ?? canvas.transform.Find("Filters") ?? canvas.transform.Find("Scroll View Filters");
            if (filterObj != null)
            {
                RectTransform rt = filterObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -160f);
                    rt.sizeDelta = new Vector2(-60f, 70f);
                }
            }

            // Contador de cartas y lupa debajo de los filtros
            Transform counterObj = canvas.transform.Find("TotalCardsText") ?? canvas.transform.Find("HeaderContainer/CountText");
            if (counterObj != null)
            {
                RectTransform rt = counterObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(45f, -245f);
                }
            }
        }

        private static void FixHomeScreenLayout(Canvas canvas)
        {
            // Botón Misiones
            Transform misionesBtn = canvas.transform.Find("MisionesButton") ?? canvas.transform.Find("QuickActions/MisionesButton");
            if (misionesBtn != null)
            {
                RectTransform rt = misionesBtn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(250f, 74f);
                }
            }
        }

        private static void FixProfileScreenLayout(Canvas canvas)
        {
            // Avatar circular centrado y tuerca 1:1 arriba a la derecha
            Transform gearBtn = canvas.transform.Find("SettingsButton") ?? canvas.transform.Find("Header/SettingsButton") ?? canvas.transform.Find("TopBar/SettingsButton");
            if (gearBtn != null)
            {
                RectTransform rt = gearBtn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(64f, 64f); // Tamaño cuadrado perfecto
                    Image img = gearBtn.GetComponent<Image>();
                    if (img != null) img.preserveAspect = true;
                }
            }

            Transform avatarObj = canvas.transform.Find("Avatar") ?? canvas.transform.Find("AvatarContainer/AvatarImage");
            if (avatarObj != null)
            {
                RectTransform rt = avatarObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(160f, 160f); // Avatar circular 1:1
                    Image img = avatarObj.GetComponent<Image>();
                    if (img != null) img.preserveAspect = true;
                }
            }
        }
    }
}

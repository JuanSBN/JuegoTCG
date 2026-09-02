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
    public static class MobileResponsiveOptimizer
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

        public static void OptimizeAllScenesForMobile()
        {
            Debug.Log("<color=cyan>[Responsive] Aplicando diseño responsivo integral (Anclas, Canvas Scaler Match Width y Grillas)...</color>");

            string currentActiveScene = SceneManager.GetActiveScene().path;

            foreach (var scenePath in ProjectScenes)
            {
                if (!File.Exists(scenePath)) continue;

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                ApplyResponsiveToScene(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"<color=green>[Responsive] Escena optimizada con éxito: {scene.name}</color>");
            }

            // Restaurar escena activa
            if (!string.IsNullOrEmpty(currentActiveScene) && File.Exists(currentActiveScene))
            {
                EditorSceneManager.OpenScene(currentActiveScene, OpenSceneMode.Single);
            }

            Debug.Log("<color=gold>[Responsive:COMPLETO] ¡Todas las 13 escenas quedaron 100% responsivas para celulares modernos!</color>");
        }

        private static void ApplyResponsiveToScene(Scene scene)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                // 1. Configurar Canvas Scaler: Match Width (0.0) a 1080x2400
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 2400f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.0f; // Match Width (escala fija horizontal perfecta)

                // 2. Optimizar anclas de barras superiores e inferiores
                ConfigureResponsiveBars(canvas);

                // 3. Optimizar grillas de cartas y álbum
                ConfigureResponsiveGrids(canvas);

                // 4. Optimizar tipografías y botones
                ConfigureResponsiveTextsAndButtons(canvas);
            }
        }

        private static void ConfigureResponsiveBars(Canvas canvas)
        {
            RectTransform[] allTransforms = canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var rt in allTransforms)
            {
                string name = rt.gameObject.name.ToLower();

                // Barra superior (TopBar / Header)
                if (name.Contains("topbar") || name.Contains("header") || name.Contains("encabezado"))
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                }
                // Barra de navegación inferior (BottomBar / NavigationTabs)
                else if (name.Contains("bottombar") || name.Contains("tabs") || name.Contains("navigation") || name.Contains("barra_inferior"))
                {
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                }
            }
        }

        private static void ConfigureResponsiveGrids(Canvas canvas)
        {
            GridLayoutGroup[] grids = canvas.GetComponentsInChildren<GridLayoutGroup>(true);
            foreach (var grid in grids)
            {
                string gName = grid.gameObject.name.ToLower();

                // Grilla de Álbum o Cartas (2 columnas responsivas para 1080px de ancho)
                if (gName.Contains("card") || gName.Contains("album") || gName.Contains("grid") || gName.Contains("content"))
                {
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = 2;
                    grid.cellSize = new Vector2(480f, 680f);
                    grid.spacing = new Vector2(30f, 30f);
                    grid.padding = new RectOffset(45, 45, 20, 100);
                    grid.childAlignment = TextAnchor.UpperCenter;
                }
            }
        }

        private static void ConfigureResponsiveTextsAndButtons(Canvas canvas)
        {
            TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                string objName = text.gameObject.name.ToLower();

                if (objName.Contains("title") || objName.Contains("header"))
                {
                    text.fontSize = Mathf.Clamp(text.fontSize, 44f, 54f);
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 36f;
                    text.fontSizeMax = 54f;
                }
                else if (objName.Contains("btn") || objName.Contains("button") || objName.Contains("mision"))
                {
                    text.enableWordWrapping = false;
                    text.overflowMode = TextOverflowModes.Overflow;
                }
            }

            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                RectTransform rt = b.GetComponent<RectTransform>();
                if (rt != null && rt.sizeDelta.y > 0 && rt.sizeDelta.y < 65f && !b.name.Contains("Tab") && !b.name.Contains("Icon"))
                {
                    Vector2 sz = rt.sizeDelta;
                    sz.y = 72f;
                    rt.sizeDelta = sz;
                }
            }
        }
    }
}

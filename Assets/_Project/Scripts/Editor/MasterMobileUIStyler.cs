using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JuegoTCG.UI;

namespace JuegoTCG.Editor
{
    public static class MasterMobileUIStyler
    {
        // Design Tokens de Alta Fidelidad (Figma)
        private static readonly Color Gold = new Color(0.910f, 0.659f, 0.125f);
        private static readonly Color DarkBg = new Color(0.043f, 0.082f, 0.059f);
        private static readonly Color CardBg = new Color(0.055f, 0.110f, 0.082f);
        private static readonly Color BorderSubtle = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextMuted = new Color(1f, 1f, 1f, 0.60f);

        [MenuItem("JuegoTCG/🎨 Pulir Diseño Visual Móvil (Todas las Pantallas)")]
        public static void PolishAllMobileScreens()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("<color=yellow>[Styler] Sal del modo Play antes de aplicar el pulido visual.</color>");
                return;
            }

            // 0. Registrar todas las escenas del juego en los ajustes de compilación
            RegisterAllProjectScenesInBuildSettings();

            // 1. Pulir Mis Cartas
            EditorTools.MyCardsSceneBuilder.BuildMyCardsScene();
            Debug.Log("<color=green>[Styler] Pantalla de Mis Cartas / Álbum estilizada y alineada.</color>");

            // 2. Pulir Inicio
            EditorTools.HomeScreenSceneBuilder.BuildHomeScreenScene();
            Debug.Log("<color=green>[Styler] Pantalla de Inicio (Sobres, Misiones, Racha) estilizada.</color>");

            // 3. Pulir Tienda
            EditorTools.StoreSceneBuilder.BuildStoreScene();
            Debug.Log("<color=green>[Styler] Pantalla de Tienda (Packs, Monedas y Pases) estilizada.</color>");

            // 4. Pulir Perfil
            PolishProfileScene();
            Debug.Log("<color=green>[Styler] Pantalla de Perfil (Cancha 11 Ideal y Avatar) estilizada.</color>");

            // 5. Pulir Ajustes
            PolishSettingsScene();
            Debug.Log("<color=green>[Styler] Pantalla de Ajustes estilizada.</color>");

            // 6. Aplicar ajustes globales Pixel-Perfect
            PixelPerfectFigmaOptimizer.ApplyPixelPerfectOptimization();

            Debug.Log("<color=gold>[Styler:COMPLETO] ¡Todas las pantallas tienen ahora el diseño pulido, estético y responsivo para celulares!</color>");
        }

        private static void PolishProfileScene()
        {
            string scenePath = "Assets/_Project/Scenes/ProfileScene.unity";
            if (!File.Exists(scenePath)) return;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.referenceResolution = new Vector2(1080, 2400);
                    scaler.matchWidthOrHeight = 0.0f;
                }
            }
            EditorSceneManager.SaveScene(scene);
        }

        private static void PolishSettingsScene()
        {
            string scenePath = "Assets/_Project/Scenes/SettingsScene.unity";
            if (!File.Exists(scenePath)) return;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.referenceResolution = new Vector2(1080, 2400);
                    scaler.matchWidthOrHeight = 0.0f;
                }
            }
            EditorSceneManager.SaveScene(scene);
        }

        private static void RegisterAllProjectScenesInBuildSettings()
        {
            string[] scenes = new string[]
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

            var buildScenes = new EditorBuildSettingsScene[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);
            }
            EditorBuildSettings.scenes = buildScenes;
        }
    }
}

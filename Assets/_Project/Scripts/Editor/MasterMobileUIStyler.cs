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

        [MenuItem("JuegoTCG/⚙️ Herramientas y Build/🎨 Pulir Todas las Pantallas", priority = 40)]
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

            // 7. Abrir HomeScreenScene para visualización inmediata
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/HomeScreenScene.unity");

            Debug.Log("<color=gold>[Styler:COMPLETO] ¡Todas las pantallas tienen ahora el diseño pulido, estético y responsivo para celulares!</color>");
        }

        [MenuItem("JuegoTCG/📱 Pantallas AAA/🏠 Inicio (HomeScreen)", priority = 1)]
        public static void PolishAndOpenHome()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("<color=yellow>[Styler] Sal del modo Play antes de aplicar el pulido visual.</color>");
                return;
            }

            RegisterAllProjectScenesInBuildSettings();
            EditorTools.HomeScreenSceneBuilder.BuildHomeScreenScene();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/HomeScreenScene.unity");
            Debug.Log("<color=gold>[Styler:Inicio] ¡Pantalla de Inicio reconstruida con calidad AAA!</color>");
        }

        [MenuItem("JuegoTCG/📱 Pantallas AAA/🃏 Mis Cartas (Álbum)", priority = 2)]
        public static void PolishAndOpenMyCards()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("<color=yellow>[Styler] Sal del modo Play antes de aplicar el pulido visual.</color>");
                return;
            }

            RegisterAllProjectScenesInBuildSettings();
            EditorTools.MyCardsSceneBuilder.BuildMyCardsScene();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MyCardsScene.unity");
            Debug.Log("<color=gold>[Styler:MisCartas] ¡Pantalla de Mis Cartas / Álbum reconstruida con calidad AAA!</color>");
        }

        [MenuItem("JuegoTCG/📱 Pantallas AAA/🛒 Tienda (Store)", priority = 3)]
        public static void PolishAndOpenStore()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("<color=yellow>[Styler] Sal del modo Play antes de aplicar el pulido visual.</color>");
                return;
            }

            RegisterAllProjectScenesInBuildSettings();
            EditorTools.StoreSceneBuilder.BuildStoreScene();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/StoreScene.unity");
            Debug.Log("<color=gold>[Styler:Tienda] ¡Pantalla de Tienda reconstruida con calidad AAA!</color>");
        }

        [MenuItem("JuegoTCG/📱 Pantallas AAA/👥 Comunidad (Community)", priority = 4)]
        public static void PolishAndOpenCommunity()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("<color=yellow>[Styler] Sal del modo Play antes de aplicar el pulido visual.</color>");
                return;
            }

            RegisterAllProjectScenesInBuildSettings();
            EditorTools.CommunitySceneBuilder.BuildCommunityScene();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/CommunityScene.unity");
            Debug.Log("<color=gold>[Styler:Comunidad] ¡Pantalla de Comunidad reconstruida con calidad AAA!</color>");
        }

        [MenuItem("JuegoTCG/📱 Pantallas AAA/🏆 Vitrinas Públicas (Vitrines)", priority = 5)]
        public static void PolishAndOpenVitrines()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("<color=yellow>[Styler] Sal del modo Play antes de aplicar el pulido visual.</color>");
                return;
            }

            RegisterAllProjectScenesInBuildSettings();
            EditorTools.VitrinesSceneBuilder.BuildVitrinesScene();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/VitrinesScene.unity");
            Debug.Log("<color=gold>[Styler:Vitrinas] ¡Pantalla de Vitrinas Públicas reconstruida con calidad AAA!</color>");
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

using UnityEditor;
using UnityEngine;

namespace JuegoTCG.Editor
{
    /// <summary>
    /// Registra automáticamente todas las escenas del proyecto en Build Settings
    /// cada vez que Unity se abre o recarga scripts, evitando el error de 'Scene couldn't be loaded'.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoRegisterBuildScenes
    {
        private static readonly string[] RequiredScenes = new string[]
        {
            "Assets/_Project/Scenes/SplashScene.unity",
            "Assets/_Project/Scenes/LoginScene.unity",
            "Assets/_Project/Scenes/HomeScreenUIToolkitScene.unity",
            "Assets/_Project/Scenes/MyCardsSceneUIToolkit.unity",
            "Assets/_Project/Scenes/StoreSceneUIToolkit.unity",
            "Assets/_Project/Scenes/CommunitySceneUIToolkit.unity",
            "Assets/_Project/Scenes/VitrinesSceneUIToolkit.unity",
            "Assets/_Project/Scenes/TradeSceneUIToolkit.unity",
            "Assets/_Project/Scenes/MarketSceneUIToolkit.unity",
            "Assets/_Project/Scenes/FriendsSceneUIToolkit.unity",
            "Assets/_Project/Scenes/ProfileSceneUIToolkit.unity",
            "Assets/_Project/Scenes/SettingsSceneUIToolkit.unity",
            "Assets/_Project/Scenes/PackOpeningScene.unity"
        };

        static AutoRegisterBuildScenes()
        {
            RegisterScenes();
        }

        [MenuItem("JuegoTCG/⚙️ Herramientas y Build/🛠️ Registrar Escenas en Build Settings", priority = 42)]
        public static void RegisterScenes()
        {
            var buildScenes = new EditorBuildSettingsScene[RequiredScenes.Length];
            for (int i = 0; i < RequiredScenes.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(RequiredScenes[i], true);
            }
            EditorBuildSettings.scenes = buildScenes;
            Debug.Log("<color=green>[BuildSettings] 13 escenas registradas activas en los ajustes de compilación.</color>");
        }
    }
}

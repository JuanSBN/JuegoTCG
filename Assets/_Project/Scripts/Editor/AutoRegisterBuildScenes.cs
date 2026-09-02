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

        static AutoRegisterBuildScenes()
        {
            RegisterScenes();
        }

        [MenuItem("JuegoTCG/🛠️ Registrar Escenas en Build Settings")]
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

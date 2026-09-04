using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JuegoTCG.Editor
{
    public static class AndroidApkBuilder
    {
        [MenuItem("JuegoTCG/⚙️ Herramientas y Build/📦 Compilar APK Android", priority = 43)]
        public static void BuildAndroidApk()
        {
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string apkPath = Path.Combine(outputDirectory, "JuegoTCG_Alpha.apk");

            Debug.Log("<color=cyan>[Build] Preparando compilación de APK Android...</color>");

            // 1. Configurar escenas oficiales del juego (UI Toolkit 100%)
            string[] scenes = new string[]
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

            // 2. Ajustes de Player
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.juansbn.juegotcg");
            PlayerSettings.productName = "JuegoTCG";
            PlayerSettings.companyName = "JuanSBN";

            // 3. Desactivar Burst para el Player para evitar el error de bcl.exe
            EditorPrefs.SetBool("BurstEnableCompilation", false);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"<color=yellow>[Build] Iniciando compilación en: {apkPath}</color>");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[Build:EXITO] APK generado exitosamente ({summary.totalSize / (1024 * 1024)} MB) en: {apkPath}</color>");
                EditorUtility.RevealInFinder(apkPath);
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"<color=red>[Build:FALLO] La compilación falló con {summary.totalErrors} errores.</color>");
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        {
                            Debug.LogError($"[Build:Detalle] {msg.content}");
                        }
                    }
                }
            }
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using JuegoTCG.UI;

namespace JuegoTCG.EditorTools
{
    public static class UIToolkitFriendsSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/FriendsSceneUIToolkit.unity";
        private const string UXMLPath = "Assets/_Project/UI/Views/FriendsScreen.uxml";
        private const string PanelSettingsPath = "Assets/_Project/UI/PanelSettings.asset";

        [MenuItem("JuegoTCG/✨ UI Toolkit (UXML + USS)/👥 Amigos UI Toolkit", priority = 27)]
        public static void BuildUIToolkitFriendsScene()
        {
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/FriendsScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/FriendsScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Components/LiquidGlassNavBar.uss", ImportAssetOptions.ForceUpdate);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.08f, 0.06f);
            cam.orthographic = true;
            camGO.AddComponent<AudioListener>();

            // UI Document
            GameObject uiDocGO = new GameObject("UIDocument_FriendsScreen");
            UIDocument uiDoc = uiDocGO.AddComponent<UIDocument>();

            // Load UXML
            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
            if (uxml != null)
            {
                uiDoc.visualTreeAsset = uxml;
            }

            // Panel Settings
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1080, 2400);
                panelSettings.match = 0.0f;
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
                AssetDatabase.SaveAssets();
            }
            uiDoc.panelSettings = panelSettings;

            // Add Controllers
            uiDocGO.AddComponent<UIToolkitFriendsController>();
            uiDocGO.AddComponent<LiquidGlassNavBarController>();

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            JuegoTCG.Editor.AutoRegisterBuildScenes.RegisterScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath);

            Debug.Log($"<color=gold>[UIToolkit] ¡Escena de Amigos UI Toolkit generada y abierta con éxito en {ScenePath}!</color>");
        }
    }
}
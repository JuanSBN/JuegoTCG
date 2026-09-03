using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using JuegoTCG.UI;

namespace JuegoTCG.EditorTools
{
    public static class UIToolkitHomeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/HomeScreenUIToolkitScene.unity";
        private const string UXMLPath = "Assets/_Project/UI/Views/HomeScreen.uxml";
        private const string PanelSettingsPath = "Assets/_Project/UI/PanelSettings.asset";

        [MenuItem("JuegoTCG/✨ UI Toolkit (UXML + USS)/🏠 Inicio UI Toolkit", priority = 20)]
        public static void BuildUIToolkitHomeScene()
        {
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/HomeScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/HomeScreen.uxml", ImportAssetOptions.ForceUpdate);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.08f, 0.06f); // #0a140e
            cam.orthographic = true;
            camGO.AddComponent<AudioListener>();

            // UI Document
            GameObject uiDocGO = new GameObject("UIDocument_HomeScreen");
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
                panelSettings.match = 0.0f; // Match Width
                
                System.IO.Directory.CreateDirectory("Assets/_Project/UI");
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
                AssetDatabase.SaveAssets();
            }
            uiDoc.panelSettings = panelSettings;

            // Add Controller
            uiDocGO.AddComponent<UIToolkitHomeScreenController>();

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            JuegoTCG.Editor.AutoRegisterBuildScenes.RegisterScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath);

            Debug.Log($"<color=gold>[UIToolkit] ¡Escena de Inicio UI Toolkit generada y abierta con éxito en {ScenePath}!</color>");
        }
    }
}

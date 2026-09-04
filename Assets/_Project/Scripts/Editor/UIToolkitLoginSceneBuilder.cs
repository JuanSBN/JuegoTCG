using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using JuegoTCG.UI;

namespace JuegoTCG.EditorTools
{
    public static class UIToolkitLoginSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/LoginSceneUIToolkit.unity";
        private const string UXMLPath = "Assets/_Project/UI/Views/LoginScreen.uxml";
        private const string PanelSettingsPath = "Assets/_Project/UI/PanelSettings.asset";

        [MenuItem("JuegoTCG/✨ UI Toolkit (UXML + USS)/🔑 Login / Bienvenida UI Toolkit", priority = 20)]
        public static void BuildUIToolkitLoginScene()
        {
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/LoginScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/LoginScreen.uxml", ImportAssetOptions.ForceUpdate);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.08f, 0.06f);
            cam.orthographic = true;
            camGO.AddComponent<AudioListener>();

            // UI Document
            GameObject uiDocGO = new GameObject("UIDocument_LoginScreen");
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

            // Add Controller
            uiDocGO.AddComponent<UIToolkitLoginController>();

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Escena LoginSceneUIToolkit.unity (1080x2400) generada con éxito!</color>");
        }
    }
}

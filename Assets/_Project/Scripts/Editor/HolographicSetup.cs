#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using JuegoTCG.Cards;

namespace JuegoTCG.EditorTools
{
    public static class HolographicSetup
    {
        private const string MaterialsFolder = "Assets/_Project/Materials";
        private const string ShadersFolder = "Assets/_Project/Shaders";
        private const string PrefabPath = "Assets/_Project/Prefabs/Cards/CardPrefab.prefab";
        private const string ShaderPath = "Assets/_Project/Shaders/HolographicFoilShader.shadergraph";

        public static void SetupHolographicMaterial()
        {
            if (!Directory.Exists(MaterialsFolder))
            {
                Directory.CreateDirectory(MaterialsFolder);
                AssetDatabase.Refresh();
            }

            string matPath = $"{MaterialsFolder}/HolographicFoilMaterial.mat";
            Material holoMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            // 1. Try loading Shader directly via AssetDatabase
            Shader holoShader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            // 2. Fallback to Shader.Find
            if (holoShader == null)
            {
                holoShader = Shader.Find("Shader Graphs/HolographicFoilShader");
            }

            // 3. Fallback to Universal Render Pipeline Sprite Unlit
            if (holoShader == null)
            {
                holoShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
            }

            // 4. Ultimate fallback to Sprites/Default
            if (holoShader == null)
            {
                holoShader = Shader.Find("Sprites/Default");
            }

            if (holoMat == null)
            {
                holoMat = new Material(holoShader);
                AssetDatabase.CreateAsset(holoMat, matPath);
            }
            else if (holoShader != null)
            {
                holoMat.shader = holoShader;
            }

            AssetDatabase.SaveAssets();

            // Load CardPrefab and assign material & HolographicTilt
            GameObject prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabGO != null)
            {
                string instancePath = AssetDatabase.GetAssetPath(prefabGO);
                GameObject contents = PrefabUtility.LoadPrefabContents(instancePath);

                CardDisplay display = contents.GetComponent<CardDisplay>();
                if (display != null)
                {
                    SerializedObject so = new SerializedObject(display);
                    so.FindProperty("holographicMaterial").objectReferenceValue = holoMat;
                    so.ApplyModifiedProperties();
                }

                if (contents.GetComponent<HolographicTilt>() == null)
                {
                    contents.AddComponent<HolographicTilt>();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, instancePath);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=cyan>[JuegoTCG] ¡Material Holográfico y componente HolographicTilt configurados con éxito!</color>");
        }
    }
}
#endif

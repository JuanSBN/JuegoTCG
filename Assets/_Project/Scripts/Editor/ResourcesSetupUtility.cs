using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

namespace JuegoTCG.EditorTools
{
    /// <summary>
    /// Copia los CardData del Album Piloto y el CardPrefab a Assets/Resources/
    /// para que Resources.Load funcione en builds de mobile.
    /// Se ejecuta AUTOMÁTICAMENTE antes de cada compilación (IPreprocessBuildWithReport)
    /// o manualmente desde el menú JuegoTCG si se desea.
    /// </summary>
    public class ResourcesSetupUtility : IPreprocessBuildWithReport
    {
        private const string SourceCards = "Assets/_Project/ScriptableObjects/PilotAlbum";
        private const string SourcePrefab = "Assets/_Project/Prefabs/Cards/CardPrefab.prefab";
        private const string TargetCards = "Assets/Resources/PilotAlbum";
        private const string TargetPrefab = "Assets/Resources/CardPrefab.prefab";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[AutoBuild] Sincronizando Resources automáticamente antes de compilar...");
            ExecuteSetup(silent: true);
        }

        [MenuItem("JuegoTCG/Preparar Resources para Mobile Build", priority = 5)]
        public static void SetupResourcesManual()
        {
            ExecuteSetup(silent: false);
        }

        public static void ExecuteSetup(bool silent = false)
        {
            // 1. Crear carpetas si no existen
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(TargetCards))
                AssetDatabase.CreateFolder("Assets/Resources", "PilotAlbum");

            // 2. Copiar todas las CardData del album piloto
            string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { SourceCards });
            int copied = 0;
            foreach (string guid in guids)
            {
                string srcPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(srcPath);
                string dstPath = TargetCards + "/" + fileName;
                AssetDatabase.CopyAsset(srcPath, dstPath);
                copied++;
            }

            // 3. Copiar CardPrefab
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab) != null)
            {
                AssetDatabase.CopyAsset(SourcePrefab, TargetPrefab);
                Debug.Log("[ResourcesSetup] CardPrefab copiado a Resources.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[ResourcesSetup] " + copied + " CardData sincronizadas en " + TargetCards + ".</color>");

            if (!silent)
            {
                EditorUtility.DisplayDialog(
                    "Resources listo para mobile",
                    copied + " cartas del Album Piloto copiadas a Resources/PilotAlbum/\n\nAhora puedes compilar el APK.",
                    "Entendido"
                );
            }
        }
    }
}

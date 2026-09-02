using UnityEditor;
using UnityEngine;

namespace JuegoTCG.Editor
{
    public static class ForceProjectRecompile
    {
        [MenuItem("JuegoTCG/🔄 Forzar Recarga de Scripts (Fix Play Mode)")]
        public static void ForceRecompile()
        {
            Debug.Log("<color=cyan>[Recompile] Sincronizando base de datos de assets y forzando recarga de scripts...</color>");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorUtility.RequestScriptReload();
            Debug.Log("<color=green>[Recompile:LISTO] Scripts recargados y sincronizados con éxito. Ya puedes presionar Play ▶️.</color>");
        }
    }
}

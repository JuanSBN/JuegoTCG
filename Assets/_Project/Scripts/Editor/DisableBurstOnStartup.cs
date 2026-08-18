#if UNITY_EDITOR
using UnityEditor;

namespace JuegoTCG.EditorTools
{
    [InitializeOnLoad]
    public static class DisableBurstOnStartup
    {
        static DisableBurstOnStartup()
        {
            // Disables Burst JIT background compiler in Unity Editor
            EditorPrefs.SetBool("BurstCompilation", false);
        }
    }
}
#endif

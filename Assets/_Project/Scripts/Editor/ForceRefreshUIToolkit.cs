using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace JuegoTCG.EditorTools
{
    public static class ForceRefreshUIToolkit
    {
        [MenuItem("JuegoTCG/✨ UI Toolkit (UXML + USS)/🔄 Forzar Recarga UI Toolkit", priority = 10)]
        public static void ForceRefreshAll()
        {
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/HomeScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/HomeScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/SettingsScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/SettingsScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/FriendsScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/FriendsScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/ProfileScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/ProfileScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/MarketScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/MarketScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/TradeScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/TradeScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/StoreScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/StoreScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/MyCardsScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/MyCardsScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Styles/CommunityScreen.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Views/CommunityScreen.uxml", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Components/LiquidGlassNavBar.uss", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/_Project/UI/Components/LiquidGlassNavBar.uxml", ImportAssetOptions.ForceUpdate);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.OpenScene(activeScene.path);
            }

            var uiDocs = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var doc in uiDocs)
            {
                var tree = doc.visualTreeAsset;
                doc.visualTreeAsset = null;
                doc.visualTreeAsset = tree;
                EditorUtility.SetDirty(doc.gameObject);
            }

            Debug.Log("<color=green>[UIToolkit] ¡Todos los estilos USS y UXML han sido reimportados y el árbol visual reconstruido al 100%!</color>");
        }
    }
}
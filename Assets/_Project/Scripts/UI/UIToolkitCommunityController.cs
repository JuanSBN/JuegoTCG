using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno basado en UI Toolkit (Flexbox / USS) para la Pantalla Hub de Comunidad.
    /// Conecta con Vitrinas Públicas, Intercambio, Mercado y Amigos, integrando la Liquid Glass Nav Bar.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitCommunityController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Button cardVitrinas;
        private Button cardIntercambio;
        private Button cardMercado;
        private Button cardAmigos;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            BindUI();
        }

        private void BindUI()
        {
            // Cards
            cardVitrinas = root.Q<Button>("Card_Vitrinas");
            cardIntercambio = root.Q<Button>("Card_Intercambio");
            cardMercado = root.Q<Button>("Card_Mercado");
            cardAmigos = root.Q<Button>("Card_Amigos");

            if (cardVitrinas != null)
            {
                cardVitrinas.clicked += () => SceneManager.LoadScene("VitrinesSceneUIToolkit");
            }

            if (cardIntercambio != null)
            {
                cardIntercambio.clicked += () => SceneManager.LoadScene("TradeSceneUIToolkit");
            }

            if (cardMercado != null)
            {
                cardMercado.clicked += () => SceneManager.LoadScene("MarketSceneUIToolkit");
            }

            if (cardAmigos != null)
            {
                cardAmigos.clicked += () => SceneManager.LoadScene("FriendsSceneUIToolkit");
            }

            // Wire Liquid Glass Bottom Nav Bar (Tab Comunidad)
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Comunidad);
        }
    }
}
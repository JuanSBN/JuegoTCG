using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Social;

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

            TradeService.EnsureExists();
            SocialService.EnsureExists();

            BindUI();
            UpdateBadges();

            if (TradeService.Instance != null)
            {
                TradeService.Instance.OnOffersUpdated += UpdateBadges;
                _ = TradeService.Instance.RefreshCloudTradesAsync();
            }

            if (SocialService.Instance != null)
            {
                SocialService.Instance.OnRequestsChanged += UpdateBadges;
                _ = SocialService.Instance.RefreshCloudRequestsAndFriendsAsync();
            }
        }

        private void OnDisable()
        {
            if (TradeService.Instance != null)
            {
                TradeService.Instance.OnOffersUpdated -= UpdateBadges;
            }

            if (SocialService.Instance != null)
            {
                SocialService.Instance.OnRequestsChanged -= UpdateBadges;
            }
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

        private void UpdateBadges()
        {
            // Badge Intercambio
            var badgeIntercambio = cardIntercambio?.Q<VisualElement>(className: "community-card-badge");
            var labelIntercambio = cardIntercambio?.Q<Label>(className: "community-badge-text");
            int tradeCount = TradeService.Instance != null ? TradeService.Instance.ReceivedOffers.Count : 0;
            if (badgeIntercambio != null)
            {
                badgeIntercambio.style.display = tradeCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                if (labelIntercambio != null) labelIntercambio.text = tradeCount.ToString();
            }

            // Badge Amigos
            var badgeAmigos = cardAmigos?.Q<VisualElement>(className: "community-card-badge");
            var labelAmigos = cardAmigos?.Q<Label>(className: "community-badge-text");
            int reqCount = SocialService.Instance != null ? SocialService.Instance.PendingRequests.Count : 0;
            if (badgeAmigos != null)
            {
                badgeAmigos.style.display = reqCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                if (labelAmigos != null) labelAmigos.text = reqCount.ToString();
            }
        }
    }
}
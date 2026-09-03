using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla de Intercambios (TradeScreen).
    /// Maneja el filtrado dinámico entre ofertas Recibidas y Enviadas, badges de no leídas,
    /// aceptación y rechazo de ofertas con respuesta visual y navegación integrada.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitTradeController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Button backBtn;
        private Button tabReceivedBtn;
        private Button tabSentBtn;
        private VisualElement badgeReceived;
        private Label badgeCountReceived;

        private VisualElement cardTrade1;
        private VisualElement cardTrade2;
        private VisualElement cardTrade3;
        private VisualElement cardTradeSent1;
        private VisualElement emptyState;

        private Button btnAccept1;
        private Button btnReject1;
        private Button btnAccept2;
        private Button btnReject2;
        private Button btnAccept3;
        private Button btnReject3;
        private Button btnCancel1;
        private Button btnNewTrade;

        private bool isReceivedTab = true;
        private int unreadCount = 2;

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
            // Back Button
            backBtn = root.Q<Button>("BackBtn");
            if (backBtn != null)
            {
                backBtn.clicked += () => SceneManager.LoadScene("CommunitySceneUIToolkit");
            }

            // Tabs
            tabReceivedBtn = root.Q<Button>("Tab_Received");
            tabSentBtn = root.Q<Button>("Tab_Sent");
            badgeReceived = root.Q<VisualElement>("Badge_Received");
            badgeCountReceived = root.Q<Label>("BadgeCount_Received");

            if (tabReceivedBtn != null) tabReceivedBtn.clicked += () => SwitchTab(true);
            if (tabSentBtn != null) tabSentBtn.clicked += () => SwitchTab(false);

            // Cards
            cardTrade1 = root.Q<VisualElement>("Card_Trade_1");
            cardTrade2 = root.Q<VisualElement>("Card_Trade_2");
            cardTrade3 = root.Q<VisualElement>("Card_Trade_3");
            cardTradeSent1 = root.Q<VisualElement>("Card_Trade_Sent_1");
            emptyState = root.Q<VisualElement>("TradeEmptyState");

            // Buttons
            btnAccept1 = root.Q<Button>("Btn_Accept_1");
            btnReject1 = root.Q<Button>("Btn_Reject_1");
            btnAccept2 = root.Q<Button>("Btn_Accept_2");
            btnReject2 = root.Q<Button>("Btn_Reject_2");
            btnAccept3 = root.Q<Button>("Btn_Accept_3");
            btnReject3 = root.Q<Button>("Btn_Reject_3");
            btnCancel1 = root.Q<Button>("Btn_Cancel_1");
            btnNewTrade = root.Q<Button>("Btn_NewTrade");

            if (btnAccept1 != null) btnAccept1.clicked += () => AcceptTrade(cardTrade1, 1);
            if (btnReject1 != null) btnReject1.clicked += () => RejectTrade(cardTrade1, 1);

            if (btnAccept2 != null) btnAccept2.clicked += () => AcceptTrade(cardTrade2, 2);
            if (btnReject2 != null) btnReject2.clicked += () => RejectTrade(cardTrade2, 2);

            if (btnAccept3 != null) btnAccept3.clicked += () => AcceptTrade(cardTrade3, 3);
            if (btnReject3 != null) btnReject3.clicked += () => RejectTrade(cardTrade3, 3);

            if (btnCancel1 != null) btnCancel1.clicked += () => CancelSentTrade(cardTradeSent1);

            if (btnNewTrade != null)
            {
                btnNewTrade.clicked += () => Debug.Log("<color=gold>[Intercambio] Abrir modal para proponer nuevo intercambio a un amigo</color>");
            }

            // Bottom Navigation Bar (Tab Comunidad)
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Comunidad);

            UpdateBadgeDisplay();
        }

        private void SwitchTab(bool showReceived)
        {
            isReceivedTab = showReceived;

            if (isReceivedTab)
            {
                tabReceivedBtn.AddToClassList("trade-tab-pill-active");
                tabSentBtn.RemoveFromClassList("trade-tab-pill-active");

                if (cardTrade1 != null) cardTrade1.style.display = DisplayStyle.Flex;
                if (cardTrade2 != null) cardTrade2.style.display = DisplayStyle.Flex;
                if (cardTrade3 != null) cardTrade3.style.display = DisplayStyle.Flex;
                if (cardTradeSent1 != null) cardTradeSent1.style.display = DisplayStyle.None;
                if (emptyState != null) emptyState.style.display = DisplayStyle.None;
            }
            else
            {
                tabSentBtn.AddToClassList("trade-tab-pill-active");
                tabReceivedBtn.RemoveFromClassList("trade-tab-pill-active");

                if (cardTrade1 != null) cardTrade1.style.display = DisplayStyle.None;
                if (cardTrade2 != null) cardTrade2.style.display = DisplayStyle.None;
                if (cardTrade3 != null) cardTrade3.style.display = DisplayStyle.None;
                if (cardTradeSent1 != null) cardTradeSent1.style.display = DisplayStyle.Flex;
                if (emptyState != null) emptyState.style.display = DisplayStyle.None;
            }
        }

        private void AcceptTrade(VisualElement card, int id)
        {
            if (card != null)
            {
                card.style.display = DisplayStyle.None;
            }
            if (id <= 2 && unreadCount > 0)
            {
                unreadCount--;
                UpdateBadgeDisplay();
            }
            Debug.Log($"<color=green>[UI Toolkit] ¡Intercambio #{id} aceptado exitosamente!</color>");
        }

        private void RejectTrade(VisualElement card, int id)
        {
            if (card != null)
            {
                card.style.display = DisplayStyle.None;
            }
            if (id <= 2 && unreadCount > 0)
            {
                unreadCount--;
                UpdateBadgeDisplay();
            }
            Debug.Log($"<color=yellow>[UI Toolkit] Intercambio #{id} rechazado.</color>");
        }

        private void CancelSentTrade(VisualElement card)
        {
            if (card != null)
            {
                card.style.display = DisplayStyle.None;
            }
            if (emptyState != null)
            {
                emptyState.style.display = DisplayStyle.Flex;
                var emptyTitle = emptyState.Q<Label>("EmptyTitle");
                var emptyDesc = emptyState.Q<Label>("EmptyDesc");
                if (emptyTitle != null) emptyTitle.text = "No has enviado ninguna oferta.";
                if (emptyDesc != null) emptyDesc.text = "Propón un intercambio a un amigo y empieza a negociar.";
            }
            Debug.Log("<color=orange>[UI Toolkit] Oferta de intercambio cancelada.</color>");
        }

        private void UpdateBadgeDisplay()
        {
            if (badgeCountReceived != null)
            {
                badgeCountReceived.text = unreadCount.ToString();
            }
            if (badgeReceived != null)
            {
                badgeReceived.style.display = unreadCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
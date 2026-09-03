using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla de Amigos (FriendsScreen).
    /// Permite copiar código de amigo, agregar nuevos amigos, aceptar o rechazar solicitudes
    /// con actualización en vivo del badge, comparar colecciones e iniciar intercambios directos.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitFriendsController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Button backBtn;
        private Button btnCopyCode;
        private Label btnCopyCodeText;
        private TextField searchFriendInput;
        private Button btnAddFriend;

        // Solicitudes
        private VisualElement requestsSection;
        private VisualElement requestsBadge;
        private Label requestsBadgeCount;
        private VisualElement cardRequest1;
        private VisualElement cardRequest2;
        private Button btnAccept1;
        private Button btnReject1;
        private Button btnAccept2;
        private Button btnReject2;
        private int pendingRequestsCount = 2;

        // Modal de Comparar
        private VisualElement compareModal;
        private Label compareModalTitle;
        private Label compareModalDesc;
        private Button btnCloseCompare;

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
            // Back button
            backBtn = root.Q<Button>("BackBtn");
            if (backBtn != null)
            {
                backBtn.clicked += () => SceneManager.LoadScene("CommunitySceneUIToolkit");
            }

            // Copy code
            btnCopyCode = root.Q<Button>("Btn_CopyCode");
            btnCopyCodeText = root.Q<Label>("Btn_CopyCodeText");
            if (btnCopyCode != null)
            {
                btnCopyCode.clicked += CopyFriendCode;
            }

            // Add friend
            searchFriendInput = root.Q<TextField>("SearchFriendInput");
            btnAddFriend = root.Q<Button>("Btn_AddFriend");
            if (btnAddFriend != null)
            {
                btnAddFriend.clicked += AddFriendByCode;
            }

            // Solicitudes
            requestsSection = root.Q<VisualElement>("RequestsSection");
            requestsBadge = root.Q<VisualElement>("RequestsBadge");
            requestsBadgeCount = root.Q<Label>("RequestsBadgeCount");
            cardRequest1 = root.Q<VisualElement>("Card_Request_1");
            cardRequest2 = root.Q<VisualElement>("Card_Request_2");
            btnAccept1 = root.Q<Button>("Btn_Accept_1");
            btnReject1 = root.Q<Button>("Btn_Reject_1");
            btnAccept2 = root.Q<Button>("Btn_Accept_2");
            btnReject2 = root.Q<Button>("Btn_Reject_2");

            if (btnAccept1 != null) btnAccept1.clicked += () => ResolveRequest(cardRequest1, "NuevoJugador_99", true);
            if (btnReject1 != null) btnReject1.clicked += () => ResolveRequest(cardRequest1, "NuevoJugador_99", false);
            if (btnAccept2 != null) btnAccept2.clicked += () => ResolveRequest(cardRequest2, "FutbolFan_77", true);
            if (btnReject2 != null) btnReject2.clicked += () => ResolveRequest(cardRequest2, "FutbolFan_77", false);

            // Mis Amigos (Comparar e Intercambiar)
            WireFriend(1, "GoldenShot_7");
            WireFriend(2, "ElChampion");
            WireFriend(3, "MiAmigo_01");
            WireFriend(4, "FutbolFan_22");

            // Modal
            compareModal = root.Q<VisualElement>("CompareModal");
            compareModalTitle = root.Q<Label>("CompareModalTitle");
            compareModalDesc = root.Q<Label>("CompareModalDesc");
            btnCloseCompare = root.Q<Button>("Btn_CloseCompare");
            if (btnCloseCompare != null)
            {
                btnCloseCompare.clicked += () => compareModal.AddToClassList("modal-hidden");
            }

            // Bottom Nav
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Comunidad);
        }

        private void CopyFriendCode()
        {
            GUIUtility.systemCopyBuffer = "FCX-2847";
            if (btnCopyCodeText != null)
            {
                btnCopyCodeText.text = "¡COPIADO!";
                StartCoroutine(ResetCopyText());
            }
            Debug.Log("<color=gold>[Amigos] Código FCX-2847 copiado al portapapeles.</color>");
        }

        private IEnumerator ResetCopyText()
        {
            yield return new WaitForSecondsRealtime(2f);
            if (btnCopyCodeText != null)
            {
                btnCopyCodeText.text = "COPIAR";
            }
        }

        private void AddFriendByCode()
        {
            string code = searchFriendInput?.value?.Trim();
            if (!string.IsNullOrEmpty(code) && code.Length >= 6)
            {
                Debug.Log($"<color=green>[Amigos] Solicitud de amistad enviada al código {code}.</color>");
                if (searchFriendInput != null) searchFriendInput.value = string.Empty;
                ShowModal("SOLICITUD ENVIADA", $"Se ha enviado tu solicitud al jugador con código {code}.");
            }
            else
            {
                ShowModal("CÓDIGO INVÁLIDO", "Introduce un código de amigo válido (mínimo 6 caracteres).");
            }
        }

        private void ResolveRequest(VisualElement card, string name, bool accepted)
        {
            if (card != null) card.style.display = DisplayStyle.None;
            pendingRequestsCount = Mathf.Max(0, pendingRequestsCount - 1);

            if (requestsBadgeCount != null) requestsBadgeCount.text = pendingRequestsCount.ToString();
            if (requestsBadge != null) requestsBadge.style.display = pendingRequestsCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            string action = accepted ? "aceptada" : "rechazada";
            Debug.Log($"<color=cyan>[Amigos] Solicitud de {name} {action}.</color>");
        }

        private void WireFriend(int index, string friendName)
        {
            var btnCompare = root.Q<Button>($"Btn_Compare_{index}");
            var btnTrade = root.Q<Button>($"Btn_Trade_{index}");

            if (btnCompare != null)
            {
                btnCompare.clicked += () =>
                {
                    ShowModal($"COMPARANDO CON {friendName.ToUpper()}", $"Tu colección va a la par con {friendName}. Ambos tienen cartas que se pueden intercambiar.");
                };
            }

            if (btnTrade != null)
            {
                btnTrade.clicked += () =>
                {
                    Debug.Log($"<color=gold>[Amigos] Redirigiendo a Intercambio con {friendName}...</color>");
                    SceneManager.LoadScene("TradeSceneUIToolkit");
                };
            }
        }

        private void ShowModal(string title, string desc)
        {
            if (compareModalTitle != null) compareModalTitle.text = title;
            if (compareModalDesc != null) compareModalDesc.text = desc;
            if (compareModal != null) compareModal.RemoveFromClassList("modal-hidden");
        }
    }
}
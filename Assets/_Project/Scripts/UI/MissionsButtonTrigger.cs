using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [RequireComponent(typeof(Button))]
    public class MissionsButtonTrigger : MonoBehaviour
    {
        [SerializeField] private MissionsModalController targetModal;

        private void Awake()
        {
            Bind();
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnClick);
                btn.onClick.AddListener(OnClick);
            }
        }

        public void OnClick()
        {
            Debug.Log("<color=yellow>[MissionsButtonTrigger] Click detectado en botón Misiones!</color>");
            HomeScreenController home = FindFirstObjectByType<HomeScreenController>();
            if (home != null)
            {
                home.OnClickMissions();
                return;
            }

            if (targetModal == null)
            {
                targetModal = FindFirstObjectByType<MissionsModalController>(FindObjectsInactive.Include);
            }

            if (targetModal != null)
            {
                targetModal.Show();
            }
            else
            {
                Debug.LogError("[MissionsButtonTrigger] No se encontró MissionsModalController en la escena!");
            }
        }
    }
}

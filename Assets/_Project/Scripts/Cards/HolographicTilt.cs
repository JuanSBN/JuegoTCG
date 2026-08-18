using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JuegoTCG.Cards
{
    public class HolographicTilt : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler
    {
        [Header("Target Image / Material")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Material holoMaterial;

        private RectTransform rectTransform;
        private static readonly int TiltPosID = Shader.PropertyToID("_TiltPos");

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (cardImage == null)
            {
                cardImage = GetComponent<Image>();
            }

            if (cardImage != null && cardImage.material != null)
            {
                // Create unique instance of material for this card
                holoMaterial = Instantiate(cardImage.material);
                cardImage.material = holoMaterial;
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (rectTransform == null || holoMaterial == null) return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                // Normalize tilt coordinates from -0.5 to 0.5
                float normX = localPoint.x / rectTransform.rect.width;
                float normY = localPoint.y / rectTransform.rect.height;

                holoMaterial.SetVector(TiltPosID, new Vector4(normX, normY, 0, 0));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (holoMaterial != null)
            {
                // Reset tilt to center when pointer leaves
                holoMaterial.SetVector(TiltPosID, new Vector4(0, 0, 0, 0));
            }
        }
    }
}

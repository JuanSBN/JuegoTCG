using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JuegoTCG.Cards
{
    public class HolographicTilt : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Target Image / Material")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Material holoMaterial;

        [Header("3D Tilt Settings")]
        [SerializeField] private float maxTiltAngle = 14f;
        [SerializeField] private float returnSpeed = 8f;

        private RectTransform rectTransform;
        private static readonly int TiltPosID = Shader.PropertyToID("_TiltPos");
        private bool isPointerOver = false;
        private Quaternion targetRotation = Quaternion.identity;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (cardImage == null)
            {
                cardImage = GetComponent<Image>();
            }

            if (cardImage != null && cardImage.material != null)
            {
                holoMaterial = Instantiate(cardImage.material);
                cardImage.material = holoMaterial;
            }
        }

        private void Update()
        {
            if (rectTransform != null)
            {
                if (isPointerOver)
                {
                    rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, targetRotation, Time.deltaTime * 15f);
                }
                else
                {
                    rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, Quaternion.identity, Time.deltaTime * returnSpeed);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerOver = true;
            ProcessTilt(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerOver = false;
            ResetTilt();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (rectTransform == null) return;
            isPointerOver = true;
            ProcessTilt(eventData);
        }

        private void ProcessTilt(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                // Normalize tilt coordinates from -1 to 1
                float normX = Mathf.Clamp(localPoint.x / (rectTransform.rect.width * 0.5f), -1f, 1f);
                float normY = Mathf.Clamp(localPoint.y / (rectTransform.rect.height * 0.5f), -1f, 1f);

                // 3D Physical Tilt: tilt on X axis by Y offset, and Y axis by -X offset (similar to HTML prototype)
                float rotX = -normY * maxTiltAngle;
                float rotY = normX * maxTiltAngle;
                targetRotation = Quaternion.Euler(rotX, rotY, 0f);

                // Update Shader property
                if (holoMaterial != null)
                {
                    holoMaterial.SetVector(TiltPosID, new Vector4(normX * 0.5f, normY * 0.5f, 0, 0));
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            ResetTilt();
        }

        public void SetTargetMaterial(Material mat)
        {
            holoMaterial = mat;
            enabled = (mat != null);
        }

        public void ResetTilt()
        {
            targetRotation = Quaternion.identity;
            if (holoMaterial != null)
            {
                holoMaterial.SetVector(TiltPosID, new Vector4(0, 0, 0, 0));
            }
        }
    }
}

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
        [SerializeField] private bool canTilt = false;

        private RectTransform rectTransform;
        private static readonly int TiltPosID = Shader.PropertyToID("_TiltPos");
        private bool isPointerOver = false;
        private Quaternion targetRotation = Quaternion.identity;

        public bool CanTilt
        {
            get => canTilt;
            set
            {
                canTilt = value;
                if (!canTilt)
                {
                    isPointerOver = false;
                    ResetTilt();
                }
            }
        }

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
                if (isPointerOver && canTilt && holoMaterial != null)
                {
                    rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, targetRotation, Time.deltaTime * 15f);
                }
                else
                {
                    rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, Quaternion.identity, Time.deltaTime * returnSpeed);
                }
            }
        }

        private void OnDisable()
        {
            isPointerOver = false;
            ResetTilt();
            if (rectTransform != null)
            {
                rectTransform.localRotation = Quaternion.identity;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || !canTilt || holoMaterial == null) return;
            isPointerOver = true;
            ProcessTilt(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isActiveAndEnabled) return;
            isPointerOver = false;
            ResetTilt();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || !canTilt || holoMaterial == null || rectTransform == null) return;
            isPointerOver = true;
            ProcessTilt(eventData);
        }

        private void ProcessTilt(PointerEventData eventData)
        {
            if (!canTilt || holoMaterial == null) return;

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
            if (!isActiveAndEnabled) return;
            isPointerOver = false;
            ResetTilt();
        }

        public void SetTargetMaterial(Material mat)
        {
            holoMaterial = mat;
            // No habilitar automaticamente aqui. CanTilt y enabled se gestionan segun cara frontal vs reverso.
            if (mat == null)
            {
                canTilt = false;
                enabled = false;
            }
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

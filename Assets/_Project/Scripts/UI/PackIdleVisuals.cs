using UnityEngine;
using TMPro;

namespace JuegoTCG.UI
{
    public class PackIdleVisuals : MonoBehaviour
    {
        [Header("Pack Transform (Idle Pulse)")]
        [SerializeField] private RectTransform packTransform;
        [SerializeField] private float pulseSpeed = 2.4f;
        [SerializeField] private float scaleMin = 1.0f;
        [SerializeField] private float scaleMax = 1.04f;

        [Header("Rotating Rays")]
        [SerializeField] private RectTransform raysTransform;
        [SerializeField] private float rayRotationSpeed = -30f; // degrees per second

        [Header("CTA Label (Fade Pulse)")]
        [SerializeField] private TMP_Text ctaText;
        [SerializeField] private float ctaPulseSpeed = 1.8f;
        [SerializeField] private float alphaMin = 0.5f;
        [SerializeField] private float alphaMax = 1.0f;

        private void Update()
        {
            float time = Time.time;

            // 1. Pack Idle Pulse
            if (packTransform != null)
            {
                float t = (Mathf.Sin(time * pulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(scaleMin, scaleMax, t);
                packTransform.localScale = new Vector3(scale, scale, 1f);
            }

            // 2. Rotating Rays
            if (raysTransform != null)
            {
                raysTransform.Rotate(0f, 0f, rayRotationSpeed * Time.deltaTime);
            }

            // 3. CTA Text Fade Pulse
            if (ctaText != null)
            {
                float tAlpha = (Mathf.Sin(time * ctaPulseSpeed) + 1f) * 0.5f;
                Color c = ctaText.color;
                c.a = Mathf.Lerp(alphaMin, alphaMax, tAlpha);
                ctaText.color = c;
            }
        }
    }
}

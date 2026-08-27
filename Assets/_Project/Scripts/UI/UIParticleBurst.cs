using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class UIParticleBurst : MonoBehaviour
    {
        private static UIParticleBurst instance;
        public static UIParticleBurst Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindAnyObjectByType<UIParticleBurst>();
                }
                return instance;
            }
        }

        [SerializeField] private Transform particlesContainer;

        private void Awake()
        {
            instance = this;
            if (particlesContainer == null)
            {
                particlesContainer = transform;
            }
        }

        public void PlayBurst(Vector2 centerPosition, int count, Color burstColor)
        {
            StartCoroutine(SpawnBurstRoutine(centerPosition, count, burstColor));
        }

        private IEnumerator SpawnBurstRoutine(Vector2 centerPosition, int count, Color burstColor)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject pGO = new GameObject($"UIParticle_{i}");
                pGO.transform.SetParent(particlesContainer, false);

                RectTransform rect = pGO.AddComponent<RectTransform>();
                rect.anchoredPosition = centerPosition;
                float size = Random.Range(14f, 26f);
                rect.sizeDelta = new Vector2(size, size);

                Image img = pGO.AddComponent<Image>();
                Color c = Color.Lerp(burstColor, Color.white, Random.Range(0f, 0.45f));
                img.color = c;

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(320f, 750f);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float lifetime = Random.Range(0.6f, 1.1f);

                StartCoroutine(AnimateParticle(rect, img, dir, speed, lifetime));
            }
            yield return null;
        }

        private IEnumerator AnimateParticle(RectTransform rect, Image img, Vector2 dir, float speed, float duration)
        {
            float timer = 0f;
            Vector3 startScale = Vector3.one;

            while (timer < duration)
            {
                if (rect == null) yield break;
                timer += Time.deltaTime;
                float progress = timer / duration;

                float currentSpeed = Mathf.Lerp(speed, 0f, progress);
                rect.anchoredPosition += dir * currentSpeed * Time.deltaTime;

                float alpha = Mathf.Lerp(1f, 0f, progress * progress);
                if (img != null)
                {
                    Color c = img.color;
                    c.a = alpha;
                    img.color = c;
                }
                rect.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);

                yield return null;
            }

            if (rect != null)
            {
                Destroy(rect.gameObject);
            }
        }
    }
}

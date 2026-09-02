using UnityEngine;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Ajusta automáticamente el RectTransform al área segura (Safe Area) del dispositivo móvil,
    /// evitando solapamientos con el notch de la cámara frontal y la barra de navegación de gestos.
    /// Compatible con pantallas 19.5:9 y 20:9 (Moto G, Infinix, Xiaomi, Samsung, etc.).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform panel;
        private Rect lastSafeArea = new Rect(0, 0, 0, 0);
        private Vector2Int lastScreenSize = new Vector2Int(0, 0);
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            panel = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea ||
                lastScreenSize.x != Screen.width ||
                lastScreenSize.y != Screen.height ||
                lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (panel == null) return;

            Rect safeArea = Screen.safeArea;

            // En el editor de Unity cuando safeArea es pantalla completa
            if (safeArea.width <= 0 || safeArea.height <= 0)
            {
                safeArea = new Rect(0, 0, Screen.width, Screen.height);
            }

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            // Convertir coordenadas de Safe Area a anclas normalizadas (0 a 1)
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
        }
    }
}

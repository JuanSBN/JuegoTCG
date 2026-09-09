using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace JuegoTCG.Core
{
    /// <summary>
    /// Optimizador de rendimiento móvil para JuegoTCG.
    /// Desbloquea los 60 FPS (o la tasa de refresco nativa de pantallas 90Hz/120Hz)
    /// eliminando el capado por defecto a 30 FPS que Unity aplica en dispositivos móviles.
    /// </summary>
    public class MobilePerformanceOptimizer : MonoBehaviour
    {
        private const string PREF_TARGET_FPS = "App_TargetFrameRate";
        private static MobilePerformanceOptimizer instance;
        private static int cachedTargetFps = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializePerformance()
        {
            if (instance != null) return;

            // 1. En móviles (Android / iOS), vSyncCount debe ser 0 para que targetFrameRate funcione
            QualitySettings.vSyncCount = 0;

            // 2. Obtener tasa de refresco de pantalla del dispositivo
            int detectedRefreshRate = 60;
#if UNITY_2022_2_OR_NEWER
            double refreshVal = Screen.currentResolution.refreshRateRatio.value;
            if (refreshVal > 0)
            {
                detectedRefreshRate = Mathf.RoundToInt((float)refreshVal);
            }
#else
            if (Screen.currentResolution.refreshRate > 0)
            {
                detectedRefreshRate = Screen.currentResolution.refreshRate;
            }
#endif

            // 3. Determinar FPS objetivo: mínimo 60 FPS, adaptado a 60, 90 o 120Hz si la pantalla lo soporta
            int savedFps = PlayerPrefs.GetInt(PREF_TARGET_FPS, -1);
            if (savedFps > 0)
            {
                cachedTargetFps = savedFps;
            }
            else
            {
                if (detectedRefreshRate >= 115) cachedTargetFps = 120;
                else if (detectedRefreshRate >= 85) cachedTargetFps = 90;
                else cachedTargetFps = 60;
            }

            // Aplicar la tasa de frames objetivo
            Application.targetFrameRate = cachedTargetFps;

            // 4. Evitar que la pantalla se apague durante el juego
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Crear objeto persistente que mantenga la configuración viva a través de cambios de escena
            GameObject go = new GameObject("[MobilePerformanceOptimizer]");
            instance = go.AddComponent<MobilePerformanceOptimizer>();
            DontDestroyOnLoad(go);

            Debug.Log($"<color=#10B981>[Performance] FPS optimizado: {Application.targetFrameRate} FPS (Pantalla detectada: {detectedRefreshRate} Hz, VSync: {QualitySettings.vSyncCount})</color>");
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = cachedTargetFps;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = cachedTargetFps;
            OptimizeAllScrollViewsInScene();
        }

        private void Update()
        {
            // Garantizar que ningún ciclo de vida de Android o carga de escena reduzca los FPS
            if (Application.targetFrameRate != cachedTargetFps || QualitySettings.vSyncCount != 0)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = cachedTargetFps;
            }
        }

        /// <summary>
        /// Optimiza automáticamente todos los ScrollViews de UI Toolkit en la escena para máxima fluidez a 60 FPS en móviles.
        /// </summary>
        public static void OptimizeAllScrollViewsInScene()
        {
            var uiDocs = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var doc in uiDocs)
            {
                if (doc != null && doc.rootVisualElement != null)
                {
                    doc.rootVisualElement.Query<ScrollView>().ForEach(scroll =>
                    {
                        ApplyOptimalScrollSettings(scroll);
                    });
                }
            }
        }

        /// <summary>
        /// Aplica la configuración de scroll suave a 60/120 Hz a un ScrollView de UI Toolkit.
        /// </summary>
        public static void ApplyOptimalScrollSettings(ScrollView scroll)
        {
            if (scroll == null) return;
            scroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
            scroll.elasticAnimationIntervalMs = 8; // Tick rate de animación a 125 Hz (elimina el capado interno de Unity de 33ms = 30 FPS)
            scroll.scrollDecelerationRate = 0.88f; // Inercia fluida móvil natural (evita el frenazo abrupto de 0.135f)
            scroll.elasticity = 0.1f;
        }

        /// <summary>
        /// Permite cambiar manualmente el objetivo de FPS (ej: 60, 90, 120 o 30 para ahorro de batería).
        /// </summary>
        public static void SetTargetFrameRate(int fps)
        {
            cachedTargetFps = fps;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
            PlayerPrefs.SetInt(PREF_TARGET_FPS, fps);
            PlayerPrefs.Save();
            Debug.Log($"<color=#10B981>[Performance] FPS cambiado a: {fps} FPS</color>");
        }
    }
}

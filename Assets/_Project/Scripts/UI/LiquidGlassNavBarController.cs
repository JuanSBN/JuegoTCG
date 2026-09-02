using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador modular e independiente para la Barra de Navegación "Liquid Glass".
    /// Maneja la cápsula activa deslizante con animación suave, micro-interacciones táctiles
    /// y navegación entre pantallas sin duplicar lógica.
    /// </summary>
    public class LiquidGlassNavBarController : MonoBehaviour
    {
        public enum TabType
        {
            Inicio = 0,
            Cartas = 1,
            Tienda = 2,
            Comunidad = 3,
            Perfil = 4
        }

        [Header("Configuración de Pestaña Inicial")]
        [SerializeField] private TabType currentTab = TabType.Inicio;

        private VisualElement root;
        private VisualElement navBar;
        private VisualElement activeIndicator;
        private readonly List<Button> tabButtons = new List<Button>();
        private Coroutine slideCoroutine;

        public void Initialize(VisualElement rootElement, TabType activeTab)
        {
            root = rootElement;
            currentTab = activeTab;

            navBar = root.Q<VisualElement>("LiquidGlassNavBar");
            if (navBar == null) return;

            activeIndicator = navBar.Q<VisualElement>("NavActiveIndicator");

            tabButtons.Clear();
            tabButtons.Add(navBar.Q<Button>("Nav_Inicio"));
            tabButtons.Add(navBar.Q<Button>("Nav_Cartas"));
            tabButtons.Add(navBar.Q<Button>("Nav_Tienda"));
            tabButtons.Add(navBar.Q<Button>("Nav_Comunidad"));
            tabButtons.Add(navBar.Q<Button>("Nav_Perfil"));

            for (int i = 0; i < tabButtons.Count; i++)
            {
                int index = i;
                Button btn = tabButtons[i];
                if (btn == null) continue;

                btn.clicked += () => OnTabClicked((TabType)index);
            }

            navBar.RegisterCallback<GeometryChangedEvent>(OnNavBarGeometryChanged);
        }

        private void OnNavBarGeometryChanged(GeometryChangedEvent evt)
        {
            navBar.UnregisterCallback<GeometryChangedEvent>(OnNavBarGeometryChanged);
            SnapToTab(currentTab);
        }

        public void SnapToTab(TabType tab)
        {
            currentTab = tab;
            int index = (int)tab;
            if (index < 0 || index >= tabButtons.Count) return;

            UpdateTabClasses(index);

            Button targetBtn = tabButtons[index];
            if (targetBtn != null && activeIndicator != null)
            {
                float targetX = targetBtn.layout.x + (targetBtn.layout.width - activeIndicator.layout.width) * 0.5f;
                if (targetX < 0 || float.IsNaN(targetX))
                {
                    float totalWidth = navBar.layout.width > 0 ? navBar.layout.width : 1016f;
                    float tabWidth = (totalWidth - 16f) / 5f;
                    targetX = 8f + (index * tabWidth) + (tabWidth - 180f) * 0.5f;
                }
                activeIndicator.style.left = targetX;
            }
        }

        public void OnTabClicked(TabType targetTab)
        {
            if (targetTab == currentTab) return;

            int targetIndex = (int)targetTab;
            UpdateTabClasses(targetIndex);

            Button targetBtn = tabButtons[targetIndex];
            if (targetBtn != null && activeIndicator != null)
            {
                float targetX = targetBtn.layout.x + (targetBtn.layout.width - activeIndicator.layout.width) * 0.5f;
                if (float.IsNaN(targetX) || targetX <= 0)
                {
                    float totalWidth = navBar.layout.width > 0 ? navBar.layout.width : 1016f;
                    float tabWidth = (totalWidth - 16f) / 5f;
                    targetX = 8f + (targetIndex * tabWidth) + (tabWidth - 180f) * 0.5f;
                }

                if (slideCoroutine != null) StopCoroutine(slideCoroutine);
                slideCoroutine = StartCoroutine(AnimateIndicatorSlide(targetX, targetTab));
            }
            else
            {
                NavigateToScene(targetTab);
            }
        }

        private IEnumerator AnimateIndicatorSlide(float targetX, TabType targetTab)
        {
            float startX = activeIndicator.resolvedStyle.left;
            if (float.IsNaN(startX) || startX <= 0)
            {
                float totalWidth = navBar.layout.width > 0 ? navBar.layout.width : 1016f;
                float tabWidth = (totalWidth - 16f) / 5f;
                startX = 8f + ((int)currentTab * tabWidth) + (tabWidth - 180f) * 0.5f;
            }

            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeT = 1f - Mathf.Pow(1f - t, 3);

                activeIndicator.style.left = Mathf.Lerp(startX, targetX, easeT);
                yield return null;
            }

            activeIndicator.style.left = targetX;
            currentTab = targetTab;

            yield return new WaitForSecondsRealtime(0.06f);
            NavigateToScene(targetTab);
        }

        private void UpdateTabClasses(int activeIndex)
        {
            for (int i = 0; i < tabButtons.Count; i++)
            {
                Button btn = tabButtons[i];
                if (btn == null) continue;

                if (i == activeIndex)
                {
                    btn.AddToClassList("nav-tab-active");
                }
                else
                {
                    btn.RemoveFromClassList("nav-tab-active");
                }
            }
        }

        private void NavigateToScene(TabType tab)
        {
            switch (tab)
            {
                case TabType.Inicio:
                    SceneManager.LoadScene("HomeScreenUIToolkitScene");
                    break;
                case TabType.Cartas:
                    SceneManager.LoadScene("MyCardsScene");
                    break;
                case TabType.Tienda:
                    SceneManager.LoadScene("StoreScene");
                    break;
                case TabType.Comunidad:
                    SceneManager.LoadScene("VitrinesSceneUIToolkit");
                    break;
                case TabType.Perfil:
                    SceneManager.LoadScene("ProfileScene");
                    break;
            }
        }
    }
}

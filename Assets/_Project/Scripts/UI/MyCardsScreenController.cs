using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class MyCardsScreenController : MonoBehaviour
    {
        [System.Serializable]
        public struct CardItemData
        {
            public string name;
            public string initials;
            public string rarity;
            public int count;
        }

        [Header("Header & Counters")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text totalCardsCountText;
        [SerializeField] private Button searchButton;

        [Header("Filters")]
        [SerializeField] private Button[] filterButtons;
        [SerializeField] private Button scrollLeftBtn;
        [SerializeField] private Button scrollRightBtn;
        [SerializeField] private ScrollRect filterScrollRect;

        [Header("Bottom Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private string activeFilter = "Rareza";

        private void Start()
        {
            InitializeHeader();
            BindNavigationEvents();
        }

        private void InitializeHeader()
        {
            if (titleText != null) titleText.text = "MIS CARTAS";
            if (totalCardsCountText != null) totalCardsCountText.text = "1232 cartas";
        }

        private void BindNavigationEvents()
        {
            if (tabInicioButton != null)
            {
                tabInicioButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[MyCardsScreen] Regresando a Pantalla de Inicio...</color>");
                    SceneManager.LoadScene("HomeScreenScene");
                });
            }

            if (tabComunidadButton != null)
            {
                tabComunidadButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[MyCardsScreen] Navegando a Comunidad...</color>");
                    SceneManager.LoadScene("CommunityScene");
                });
            }

            if (tabPerfilButton != null)
            {
                tabPerfilButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[MyCardsScreen] Navegando a Perfil...</color>");
                    SceneManager.LoadScene("ProfileScene");
                });
            }

            if (scrollLeftBtn != null && filterScrollRect != null)
            {
                scrollLeftBtn.onClick.AddListener(() =>
                {
                    filterScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(filterScrollRect.horizontalNormalizedPosition - 0.25f);
                });
            }

            if (scrollRightBtn != null && filterScrollRect != null)
            {
                scrollRightBtn.onClick.AddListener(() =>
                {
                    filterScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(filterScrollRect.horizontalNormalizedPosition + 0.25f);
                });
            }

            if (searchButton != null)
            {
                searchButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=yellow>[MyCardsScreen] Buscar cartas activado</color>");
                });
            }
        }

        public void SetActiveFilter(string filterName)
        {
            activeFilter = filterName;
            Debug.Log($"<color=gold>[MyCardsScreen] Filtro seleccionado: {filterName}</color>");
        }
    }
}

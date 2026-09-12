using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using JuegoTCG.Cards;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para el modal de Data Packs de la comunidad.
    /// Permite navegación del catálogo remoto, instalación en 1-clic con barra de progreso,
    /// importación manual desde URL y restauración de cartas originales.
    /// </summary>
    public class DataPacksModalController : MonoBehaviour
    {
        private VisualElement modalRoot;
        private Button btnClose;

        // Active Pack Box
        private Label lblCurrentTitle;
        private Label lblCurrentSubtitle;
        private Button btnRestoreDefault;
        private VisualElement badgePackStatus;
        private Label lblBadgeStatusText;

        // Progress
        private VisualElement progressBox;
        private Label lblProgressStatus;
        private VisualElement progressFill;

        // Catalog & Manual URL
        private ScrollView packsListScroll;
        private TextField inputUrl;
        private Button btnInstallFromUrl;

        private List<DataPackIndexItem> loadedPacks = new List<DataPackIndexItem>();

        public void Initialize(VisualElement rootElement)
        {
            if (rootElement == null) return;

            modalRoot = rootElement.Q<VisualElement>("DataPacksModal");
            if (modalRoot == null)
            {
                // Si el elemento raíz ya es el modal
                if (rootElement.name == "DataPacksModal") modalRoot = rootElement;
                else return;
            }

            btnClose = modalRoot.Q<Button>("Btn_CloseDataPacks");
            if (btnClose != null) btnClose.clicked += Hide;

            lblCurrentTitle = modalRoot.Q<Label>("Lbl_CurrentPackTitle");
            lblCurrentSubtitle = modalRoot.Q<Label>("Lbl_CurrentPackSubtitle");
            btnRestoreDefault = modalRoot.Q<Button>("Btn_RestoreDefaultPack");
            badgePackStatus = modalRoot.Q<VisualElement>("Badge_PackStatus");
            lblBadgeStatusText = modalRoot.Q<Label>("Lbl_BadgeStatusText");

            if (btnRestoreDefault != null)
            {
                btnRestoreDefault.clicked += OnRestoreDefaultClicked;
            }

            progressBox = modalRoot.Q<VisualElement>("DataPackProgressBox");
            lblProgressStatus = modalRoot.Q<Label>("Lbl_InstallProgressStatus");
            progressFill = modalRoot.Q<VisualElement>("ProgressBarFill");

            packsListScroll = modalRoot.Q<ScrollView>("DataPacksListScroll");
            inputUrl = modalRoot.Q<TextField>("Input_DataPackUrl");
            btnInstallFromUrl = modalRoot.Q<Button>("Btn_InstallFromUrl");

            if (btnInstallFromUrl != null)
            {
                btnInstallFromUrl.clicked += OnInstallFromUrlClicked;
            }

            UpdateActivePackVisuals();
        }

        public void Show()
        {
            if (modalRoot == null) return;
            modalRoot.RemoveFromClassList("modal-hidden");
            UpdateActivePackVisuals();
            FetchCatalog();
        }

        public void Hide()
        {
            if (modalRoot == null) return;
            modalRoot.AddToClassList("modal-hidden");
        }

        private void UpdateActivePackVisuals()
        {
            DataPackManager.EnsureInitialized();
            bool hasActive = DataPackManager.IsDataPackActive;
            DataPackManifest manifest = DataPackManager.ActiveManifest;

            if (hasActive && manifest != null)
            {
                if (lblCurrentTitle != null) lblCurrentTitle.text = manifest.title;
                if (lblCurrentSubtitle != null)
                {
                    string photosText = manifest.hasPhotos ? "Fotos HD" : "Sin fotos";
                    lblCurrentSubtitle.text = $"v{manifest.version} · Por {manifest.author} · {photosText}";
                }
                if (btnRestoreDefault != null) btnRestoreDefault.style.display = DisplayStyle.Flex;
                if (badgePackStatus != null)
                {
                    badgePackStatus.style.display = DisplayStyle.Flex;
                    if (lblBadgeStatusText != null) lblBadgeStatusText.text = "ACTIVO";
                }
            }
            else
            {
                if (lblCurrentTitle != null) lblCurrentTitle.text = "Sin pack activo (Cartas originales)";
                if (lblCurrentSubtitle != null) lblCurrentSubtitle.text = "Nombres y clubes ficticios de FC Piloto por defecto.";
                if (btnRestoreDefault != null) btnRestoreDefault.style.display = DisplayStyle.None;
                if (badgePackStatus != null) badgePackStatus.style.display = DisplayStyle.None;
            }
        }

        private void FetchCatalog()
        {
            if (packsListScroll == null) return;

            // Renderizar primero los elementos en caché o fallback
            List<DataPackIndexItem> initialList = DataPackIndexService.GetCachedOrFallbackList();
            PopulateCatalogList(initialList);

            // Intentar actualizar catálogo remoto
            if (DataPackInstaller.Instance != null)
            {
                DataPackInstaller.Instance.StartCoroutine(DataPackIndexService.FetchIndexRoutine(
                    onSuccess: (items) => PopulateCatalogList(items),
                    onError: (err) => Debug.Log($"[DataPacksModal] Catálogo local cargado: {err}")
                ));
            }
        }

        private void PopulateCatalogList(List<DataPackIndexItem> items)
        {
            if (packsListScroll == null || items == null) return;
            packsListScroll.Clear();
            loadedPacks = items;

            string activePackTitle = DataPackManager.ActiveManifest != null ? DataPackManager.ActiveManifest.title : "";

            foreach (var pack in items)
            {
                VisualElement card = new VisualElement();
                card.AddToClassList("dp-pack-card");

                bool isCurrentActive = !string.IsNullOrEmpty(activePackTitle) &&
                    (string.Equals(pack.title, activePackTitle, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(pack.id, DataPackManager.ActiveManifest?.packId, StringComparison.OrdinalIgnoreCase));

                if (isCurrentActive)
                {
                    card.AddToClassList("dp-pack-card-active");
                }

                // Info Column
                VisualElement infoCol = new VisualElement();
                infoCol.AddToClassList("dp-pack-info");

                // Title row
                VisualElement titleRow = new VisualElement();
                titleRow.AddToClassList("dp-pack-title-row");

                Label titleLbl = new Label(pack.title);
                titleLbl.AddToClassList("dp-pack-title");
                titleRow.Add(titleLbl);

                if (pack.isRecommended)
                {
                    VisualElement recPill = new VisualElement();
                    recPill.AddToClassList("dp-pack-recommended-pill");
                    Label recText = new Label("RECOMENDADO");
                    recText.AddToClassList("dp-pack-recommended-text");
                    recPill.Add(recText);
                    titleRow.Add(recPill);
                }
                infoCol.Add(titleRow);

                // Meta row (Author, Version, Size)
                VisualElement metaRow = new VisualElement();
                metaRow.AddToClassList("dp-pack-meta-row");
                Label metaLbl = new Label($"Por: {pack.author} · v{pack.version} · {pack.sizeMb}");
                metaLbl.AddToClassList("dp-pack-meta-text");
                metaRow.Add(metaLbl);
                infoCol.Add(metaRow);

                // Description
                Label descLbl = new Label(pack.description);
                descLbl.AddToClassList("dp-pack-desc");
                infoCol.Add(descLbl);

                card.Add(infoCol);

                // Action Button
                Button actionBtn = new Button();
                actionBtn.AddToClassList("dp-btn-install");

                Label btnText = new Label();
                btnText.AddToClassList("dp-btn-install-text");

                if (isCurrentActive)
                {
                    actionBtn.AddToClassList("dp-btn-installed");
                    btnText.AddToClassList("dp-btn-installed-text");
                    btnText.text = "✓ ACTIVO";
                    actionBtn.SetEnabled(false);
                }
                else
                {
                    btnText.text = "INSTALAR (1 CLIC)";
                    string downloadUrl = pack.downloadUrl;
                    actionBtn.clicked += () => StartInstall(downloadUrl);
                }

                actionBtn.Add(btnText);
                card.Add(actionBtn);

                packsListScroll.Add(card);
            }
        }

        private void StartInstall(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            ShowProgress(0f, "Conectando al servidor...");

            DataPackInstaller.Instance.InstallFromUrl(
                url: url,
                onProgress: (progress, statusText) => ShowProgress(progress, statusText),
                onComplete: (success, errorMsg) =>
                {
                    HideProgress();
                    if (success)
                    {
                        UpdateActivePackVisuals();
                        PopulateCatalogList(loadedPacks);
                    }
                    else
                    {
                        Debug.LogError($"[DataPacksModal] Falló instalación: {errorMsg}");
                    }
                }
            );
        }

        private void OnInstallFromUrlClicked()
        {
            if (inputUrl == null || string.IsNullOrWhiteSpace(inputUrl.value)) return;
            string targetUrl = inputUrl.value.Trim();
            StartInstall(targetUrl);
        }

        private void OnRestoreDefaultClicked()
        {
            ShowProgress(0.5f, "Restaurando cartas originales...");
            DataPackInstaller.Instance.UninstallActivePack((success, error) =>
            {
                HideProgress();
                UpdateActivePackVisuals();
                PopulateCatalogList(loadedPacks);
            });
        }

        private void ShowProgress(float progress, string statusText)
        {
            if (progressBox != null)
            {
                progressBox.style.display = DisplayStyle.Flex;
            }
            if (lblProgressStatus != null)
            {
                lblProgressStatus.text = statusText;
            }
            if (progressFill != null)
            {
                progressFill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
            }
        }

        private void HideProgress()
        {
            if (progressBox != null)
            {
                progressBox.style.display = DisplayStyle.None;
            }
        }
    }
}

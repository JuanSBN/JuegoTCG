#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using JuegoTCG.Cards;

namespace JuegoTCG.Tests
{
    /// <summary>
    /// Suite de pruebas automatizadas para verificar el sistema de Fondos Personalizados de Data Packs,
    /// resolución de capas (Fondo Global vs Específico), caché en memoria y vinculación con CardDisplay.
    /// </summary>
    public static class DataPackSystemTests
    {
        [MenuItem("JuegoTCG/Test/Run Data Pack Background Tests")]
        public static void RunAllTests()
        {
            Debug.Log("<color=cyan><b>======================================================================</b></color>");
            Debug.Log("<color=cyan><b>=== INICIANDO SUITE DE PRUEBAS DE FONDOS PERSONALIZADOS (DATA PACKS) ===</b></color>");
            Debug.Log("<color=cyan><b>======================================================================</b></color>");

            int passed = 0;
            int total = 0;

            string backupDir = Path.Combine(Application.temporaryCachePath, "DataPackBackup_" + Guid.NewGuid().ToString("N"));
            string activeDir = DataPackManager.ActiveDirectory;
            bool hadActivePack = Directory.Exists(activeDir);

            try
            {
                // Respaldar pack activo actual si existe
                if (hadActivePack)
                {
                    CopyDirectory(activeDir, backupDir);
                }

                // TEST 1
                total++;
                Test1_ManifestParsingAndSerialization();
                passed++;
                Debug.Log("<color=green>[TEST 1 PASÓ] Serialización y parsing de hasCustomBackgrounds en DataPackManifest.</color>");

                // TEST 2
                total++;
                Test2_GlobalBackgroundResolution();
                passed++;
                Debug.Log("<color=green>[TEST 2 PASÓ] Resolución de fondo global (background.png en raíz) para todas las cartas.</color>");

                // TEST 3
                total++;
                Test3_SpecificCardBackgroundOverridesGlobal();
                passed++;
                Debug.Log("<color=green>[TEST 3 PASÓ] Fondo específico (backgrounds/{cardId}.png) tiene prioridad sobre el global.</color>");

                // TEST 4
                total++;
                Test4_AlternativeGlobalBackgroundInFolder();
                passed++;
                Debug.Log("<color=green>[TEST 4 PASÓ] Soporte de fondo global alternativo en subcarpeta (backgrounds/default.png).</color>");

                // TEST 5
                total++;
                Test5_RuntimeCacheIntegrityAndPerformance();
                passed++;
                Debug.Log("<color=green>[TEST 5 PASÓ] Caché en memoria: llamadas repetidas retornan la misma instancia sin I/O a disco.</color>");

                // TEST 6
                total++;
                Test6_CardDisplayHierarchyAndBinding();
                passed++;
                Debug.Log("<color=green>[TEST 6 PASÓ] CardDisplay vincula CardBackgroundImage correctamente detrás de PlayerArtImage.</color>");

                // TEST 7
                total++;
                Test7_CardDisplayDynamicFallbackCreation();
                passed++;
                Debug.Log("<color=green>[TEST 7 PASÓ] CardDisplay crea dinámicamente CardBackgroundImage si la referencia venía null.</color>");

                // TEST 8
                total++;
                Test8_CardDisplayDeactivatesWhenNoBackground();
                passed++;
                Debug.Log("<color=green>[TEST 8 PASÓ] CardBackgroundImage se desactiva limpiamente cuando la carta no tiene fondo.</color>");

                // TEST 9
                total++;
                Test9_EndToEndZipExtractionAndReload();
                passed++;
                Debug.Log("<color=green>[TEST 9 PASÓ] Flujo integral E2E: empaquetado ZIP -> extracción atómica -> recarga de álbum.</color>");

                Debug.Log($"<color=lime><b>======================================================================</b></color>");
                Debug.Log($"<color=lime><b>=== RESULTADO FINAL: {passed}/{total} PRUEBAS EXITOSAS (100% PASS) ===</b></color>");
                Debug.Log($"<color=lime><b>======================================================================</b></color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red><b>[ERROR EN PRUEBAS] Falló el test #{total}: {ex.Message}</b></color>\n{ex.StackTrace}");
            }
            finally
            {
                // Restaurar estado previo
                try
                {
                    if (Directory.Exists(activeDir))
                    {
                        Directory.Delete(activeDir, true);
                    }

                    if (hadActivePack && Directory.Exists(backupDir))
                    {
                        CopyDirectory(backupDir, activeDir);
                        Directory.Delete(backupDir, true);
                    }

                    DataPackManager.ReloadActiveDataPack();
                }
                catch (Exception cleanupEx)
                {
                    Debug.LogWarning($"[DataPackSystemTests] Advertencia limpiando pruebas: {cleanupEx.Message}");
                }
            }
        }

        #region Tests Específicos

        private static void Test1_ManifestParsingAndSerialization()
        {
            string jsonWithBg = @"{
                ""schemaVersion"": 2,
                ""packId"": ""test_pack_champions"",
                ""title"": ""Champions Pack"",
                ""author"": ""Tester"",
                ""version"": ""1.0.0"",
                ""totalCards"": 5,
                ""hasPhotos"": true,
                ""hasCustomBackgrounds"": true,
                ""hasCustomFlags"": false
            }";

            DataPackManifest manifest = JsonUtility.FromJson<DataPackManifest>(jsonWithBg);
            Assert(manifest != null, "Manifest no debe ser null.");
            Assert(manifest.hasCustomBackgrounds == true, "hasCustomBackgrounds debe ser true.");
            Assert(manifest.hasPhotos == true, "hasPhotos debe ser true.");

            string jsonWithoutBg = @"{
                ""schemaVersion"": 2,
                ""packId"": ""test_pack_simple"",
                ""title"": ""Simple Pack"",
                ""author"": ""Tester"",
                ""version"": ""1.0.0"",
                ""totalCards"": 1
            }";

            DataPackManifest manifest2 = JsonUtility.FromJson<DataPackManifest>(jsonWithoutBg);
            Assert(manifest2.hasCustomBackgrounds == false, "hasCustomBackgrounds debe ser false por defecto cuando se omite.");
        }

        private static void Test2_GlobalBackgroundResolution()
        {
            SetupCleanActiveDirectory();

            // Crear manifest y background.png global en la raíz
            WriteDummyManifest(hasBg: true);
            byte[] redPng = CreateDummyPngBytes(16, 16, Color.red);
            File.WriteAllBytes(Path.Combine(DataPackManager.ActiveDirectory, "background.png"), redPng);

            DataPackManager.ReloadActiveDataPack();

            Assert(DataPackManager.IsDataPackActive, "DataPack debe estar activo.");
            Assert(DataPackManager.HasCardBackground("card_cualquiera"), "HasCardBackground debe retornar true cuando existe fondo global.");

            Sprite bgSprite = DataPackManager.GetCardBackground("card_cualquiera");
            Assert(bgSprite != null, "GetCardBackground no debe retornar null cuando existe fondo global.");
            Assert(bgSprite.texture.width == 16, "El ancho de textura de fondo debe ser 16.");
        }

        private static void Test3_SpecificCardBackgroundOverridesGlobal()
        {
            SetupCleanActiveDirectory();

            // Fondo global rojo
            WriteDummyManifest(hasBg: true);
            byte[] redPng = CreateDummyPngBytes(16, 16, Color.red);
            File.WriteAllBytes(Path.Combine(DataPackManager.ActiveDirectory, "background.png"), redPng);

            // Fondo específico azul para card_legendaria
            string bgDir = DataPackManager.BackgroundsDirectory;
            Directory.CreateDirectory(bgDir);
            byte[] bluePng = CreateDummyPngBytes(32, 32, Color.blue);
            File.WriteAllBytes(Path.Combine(bgDir, "card_legendaria.png"), bluePng);

            DataPackManager.ReloadActiveDataPack();

            // card_legendaria debe tener el fondo específico (32x32)
            Sprite legendSprite = DataPackManager.GetCardBackground("card_legendaria");
            Assert(legendSprite != null, "El fondo de card_legendaria no debe ser null.");
            Assert(legendSprite.texture.width == 32, "card_legendaria debe resolver su fondo específico (32x32) en lugar del global (16x16).");

            // card_comun debe tener el fondo global (16x16)
            Sprite regularSprite = DataPackManager.GetCardBackground("card_comun");
            Assert(regularSprite != null, "El fondo de card_comun no debe ser null.");
            Assert(regularSprite.texture.width == 16, "card_comun debe resolver el fondo global (16x16).");
        }

        private static void Test4_AlternativeGlobalBackgroundInFolder()
        {
            SetupCleanActiveDirectory();

            WriteDummyManifest(hasBg: true);
            // Sin background.png en raíz, pero con backgrounds/default.png
            string bgDir = DataPackManager.BackgroundsDirectory;
            Directory.CreateDirectory(bgDir);
            byte[] greenPng = CreateDummyPngBytes(24, 24, Color.green);
            File.WriteAllBytes(Path.Combine(bgDir, "default.png"), greenPng);

            DataPackManager.ReloadActiveDataPack();

            Sprite bgSprite = DataPackManager.GetCardBackground("card_arbitraria");
            Assert(bgSprite != null, "Debe resolver backgrounds/default.png como fondo global.");
            Assert(bgSprite.texture.width == 24, "La textura debe ser la de default.png (24x24).");
        }

        private static void Test5_RuntimeCacheIntegrityAndPerformance()
        {
            SetupCleanActiveDirectory();
            WriteDummyManifest(hasBg: true);

            string bgDir = DataPackManager.BackgroundsDirectory;
            Directory.CreateDirectory(bgDir);
            byte[] testPng = CreateDummyPngBytes(20, 20, Color.yellow);
            File.WriteAllBytes(Path.Combine(bgDir, "card_cache_test.png"), testPng);

            DataPackManager.ReloadActiveDataPack();

            // Primera llamada: carga desde disco y guarda en caché
            Sprite s1 = DataPackManager.GetCardBackground("card_cache_test");
            // Segunda llamada: debe retornar la misma instancia en memoria
            Sprite s2 = DataPackManager.GetCardBackground("card_cache_test");

            Assert(s1 != null && s2 != null, "Los sprites no deben ser null.");
            Assert(ReferenceEquals(s1, s2), "Ambas llamadas deben retornar la misma referencia de Sprite en memoria (O(1) cache).");
        }

        private static void Test6_CardDisplayHierarchyAndBinding()
        {
            SetupCleanActiveDirectory();
            WriteDummyManifest(hasBg: true);
            byte[] bgPng = CreateDummyPngBytes(16, 16, Color.magenta);
            File.WriteAllBytes(Path.Combine(DataPackManager.ActiveDirectory, "background.png"), bgPng);
            DataPackManager.ReloadActiveDataPack();

            // Crear jerarquía CardDisplay
            GameObject root = new GameObject("TestCardRoot");
            CardDisplay cd = root.AddComponent<CardDisplay>();

            GameObject frontContainer = new GameObject("FrontContainer");
            frontContainer.transform.SetParent(root.transform);

            GameObject cardBaseBg = new GameObject("CardBaseBackground");
            cardBaseBg.transform.SetParent(frontContainer.transform);

            GameObject bgImageGo = new GameObject("CardBackgroundImage");
            bgImageGo.transform.SetParent(cardBaseBg.transform);
            Image bgImage = bgImageGo.AddComponent<Image>();

            GameObject artImageGo = new GameObject("PlayerArtImage");
            artImageGo.transform.SetParent(cardBaseBg.transform);
            Image artImage = artImageGo.AddComponent<Image>();

            // Asignar campos por SerializedObject
            SerializedObject so = new SerializedObject(cd);
            so.FindProperty("frontContainer").objectReferenceValue = frontContainer;
            so.FindProperty("playerArtImage").objectReferenceValue = artImage;
            so.FindProperty("cardBackgroundImage").objectReferenceValue = bgImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            CardData dummyCard = ScriptableObject.CreateInstance<CardData>();
            dummyCard.cardId = "test_cd_01";
            dummyCard.playerName = "Test Striker";

            cd.SetCard(dummyCard);

            Assert(bgImageGo.activeSelf == true, "CardBackgroundImage debe estar activo cuando hay fondo disponible.");
            Assert(bgImage.sprite != null, "El sprite de CardBackgroundImage debe estar asignado.");
            Assert(bgImageGo.transform.GetSiblingIndex() < artImageGo.transform.GetSiblingIndex(), 
                "CardBackgroundImage debe ubicarse como hermano detrás de PlayerArtImage.");

            UnityEngine.Object.DestroyImmediate(dummyCard);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void Test7_CardDisplayDynamicFallbackCreation()
        {
            SetupCleanActiveDirectory();
            WriteDummyManifest(hasBg: true);
            byte[] bgPng = CreateDummyPngBytes(16, 16, Color.cyan);
            File.WriteAllBytes(Path.Combine(DataPackManager.ActiveDirectory, "background.png"), bgPng);
            DataPackManager.ReloadActiveDataPack();

            GameObject root = new GameObject("TestCardLegacyRoot");
            CardDisplay cd = root.AddComponent<CardDisplay>();

            GameObject frontContainer = new GameObject("FrontContainer");
            frontContainer.transform.SetParent(root.transform);

            GameObject cardBaseBg = new GameObject("CardBaseBackground");
            cardBaseBg.transform.SetParent(frontContainer.transform);

            GameObject artImageGo = new GameObject("PlayerArtImage");
            artImageGo.transform.SetParent(cardBaseBg.transform);
            Image artImage = artImageGo.AddComponent<Image>();

            // cardBackgroundImage se deja en null intencionalmente
            SerializedObject so = new SerializedObject(cd);
            so.FindProperty("frontContainer").objectReferenceValue = frontContainer;
            so.FindProperty("playerArtImage").objectReferenceValue = artImage;
            so.FindProperty("cardBackgroundImage").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            CardData dummyCard = ScriptableObject.CreateInstance<CardData>();
            dummyCard.cardId = "test_legacy_01";
            dummyCard.playerName = "Legacy Player";

            cd.SetCard(dummyCard);

            Transform createdBg = cardBaseBg.transform.Find("CardBackgroundImage");
            Assert(createdBg != null, "CardDisplay debe crear dinámicamente CardBackgroundImage si venía nulo en el prefab.");
            Assert(createdBg.gameObject.activeSelf == true, "El GameObject creado dinámicamente debe estar activo.");
            Assert(createdBg.GetSiblingIndex() < artImageGo.transform.GetSiblingIndex(),
                "El GameObject creado dinámicamente debe ubicarse detrás de PlayerArtImage.");

            UnityEngine.Object.DestroyImmediate(dummyCard);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void Test8_CardDisplayDeactivatesWhenNoBackground()
        {
            SetupCleanActiveDirectory();
            // Data pack sin fondos
            WriteDummyManifest(hasBg: false);
            DataPackManager.ReloadActiveDataPack();

            GameObject root = new GameObject("TestCardNoBgRoot");
            CardDisplay cd = root.AddComponent<CardDisplay>();

            GameObject frontContainer = new GameObject("FrontContainer");
            frontContainer.transform.SetParent(root.transform);

            GameObject cardBaseBg = new GameObject("CardBaseBackground");
            cardBaseBg.transform.SetParent(frontContainer.transform);

            GameObject bgImageGo = new GameObject("CardBackgroundImage");
            bgImageGo.transform.SetParent(cardBaseBg.transform);
            Image bgImage = bgImageGo.AddComponent<Image>();

            GameObject artImageGo = new GameObject("PlayerArtImage");
            artImageGo.transform.SetParent(cardBaseBg.transform);
            Image artImage = artImageGo.AddComponent<Image>();

            SerializedObject so = new SerializedObject(cd);
            so.FindProperty("frontContainer").objectReferenceValue = frontContainer;
            so.FindProperty("playerArtImage").objectReferenceValue = artImage;
            so.FindProperty("cardBackgroundImage").objectReferenceValue = bgImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            CardData dummyCard = ScriptableObject.CreateInstance<CardData>();
            dummyCard.cardId = "test_no_bg";
            dummyCard.playerName = "Player Without Bg";

            cd.SetCard(dummyCard);

            Assert(bgImageGo.activeSelf == false, "CardBackgroundImage debe estar desactivado cuando la carta no tiene fondo.");

            UnityEngine.Object.DestroyImmediate(dummyCard);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void Test9_EndToEndZipExtractionAndReload()
        {
            string tempDir = Path.Combine(Application.temporaryCachePath, "DataPackZipTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string zipPath = Path.Combine(tempDir, "test_datapack.zip");

            try
            {
                // Crear un paquete ZIP completo en memoria y guardarlo a disco
                using (var zipStream = new FileStream(zipPath, FileMode.Create))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    // manifest.json
                    var manifestEntry = archive.CreateEntry("manifest.json");
                    using (var writer = new StreamWriter(manifestEntry.Open()))
                    {
                        writer.Write(@"{
                            ""schemaVersion"": 2,
                            ""packId"": ""e2e_champions_pack"",
                            ""title"": ""E2E Champions Pack"",
                            ""author"": ""Test Bot"",
                            ""version"": ""1.0.0"",
                            ""totalCards"": 1,
                            ""hasPhotos"": true,
                            ""hasCustomBackgrounds"": true
                        }");
                    }

                    // database.json
                    var dbEntry = archive.CreateEntry("database.json");
                    using (var writer = new StreamWriter(dbEntry.Open()))
                    {
                        writer.Write(@"{
                            ""cards"": [
                                {
                                    ""cardId"": ""hero_e2e"",
                                    ""playerName"": ""Super Star"",
                                    ""initials"": ""SS"",
                                    ""position"": ""DEL""
                                }
                            ]
                        }");
                    }

                    // background.png
                    var bgEntry = archive.CreateEntry("background.png");
                    using (var stream = bgEntry.Open())
                    {
                        byte[] png = CreateDummyPngBytes(16, 16, Color.cyan);
                        stream.Write(png, 0, png.Length);
                    }

                    // photos/hero_e2e.png
                    var photoEntry = archive.CreateEntry("photos/hero_e2e.png");
                    using (var stream = photoEntry.Open())
                    {
                        byte[] png = CreateDummyPngBytes(16, 16, Color.green);
                        stream.Write(png, 0, png.Length);
                    }
                }

                // Descomprimir el ZIP a ActiveDirectory simulando instalación atómica
                SetupCleanActiveDirectory();
                ZipFile.ExtractToDirectory(zipPath, DataPackManager.ActiveDirectory);

                // Recargar DataPackManager
                DataPackManager.ReloadActiveDataPack();

                Assert(DataPackManager.IsDataPackActive, "El pack E2E debe estar activo tras la extracción.");
                Assert(DataPackManager.ActiveManifest.hasCustomBackgrounds, "hasCustomBackgrounds debe ser true en el manifest extraído.");
                Assert(DataPackManager.GetPlayerName("hero_e2e", "Super Star") == "Super Star", "El nombre sustituido debe ser 'Super Star'.");
                Assert(DataPackManager.GetCardArt("hero_e2e") != null, "La foto del jugador extraída debe estar disponible.");
                Assert(DataPackManager.GetCardBackground("hero_e2e") != null, "El fondo de Champions extraído debe estar disponible.");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        #endregion

        #region Utilidades de Test

        private static void SetupCleanActiveDirectory()
        {
            string dir = DataPackManager.ActiveDirectory;
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);
        }

        private static void WriteDummyManifest(bool hasBg)
        {
            string manifestJson = $@"{{
                ""schemaVersion"": 2,
                ""packId"": ""test_pack"",
                ""title"": ""Test Pack"",
                ""author"": ""Tester"",
                ""version"": ""1.0.0"",
                ""totalCards"": 1,
                ""hasPhotos"": true,
                ""hasCustomBackgrounds"": {hasBg.ToString().ToLower()}
            }}";
            File.WriteAllBytes(Path.Combine(DataPackManager.ActiveDirectory, "manifest.json"), System.Text.Encoding.UTF8.GetBytes(manifestJson));
        }

        private static byte[] CreateDummyPngBytes(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            return bytes;
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"[ASSERTION FAILED] {message}");
            }
        }

        #endregion
    }
}
#endif

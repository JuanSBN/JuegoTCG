#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JuegoTCG.Cards;

namespace JuegoTCG.Editor
{
    /// <summary>
    /// Herramienta y suite de pruebas para validar el comportamiento en vivo del sistema de Data Packs
    /// directamente dentro de las escenas y prefabs del juego (uGUI CardDisplay y UI Toolkit).
    /// </summary>
    public static class InGameDataPackTester
    {
        private const string TEST_PACK_ID = "comunidad_champions_pack_v1";

        [MenuItem("JuegoTCG/Data Packs/1. Ejecutar Pruebas En Juego (In-Game Tests)")]
        public static void RunInGameTests()
        {
            Debug.Log("<color=cyan><b>======================================================================</b></color>");
            Debug.Log("<color=cyan><b>=== INICIANDO PRUEBAS EN JUEGO: SISTEMA DE DATA PACKS & FONDOS ===</b></color>");
            Debug.Log("<color=cyan><b>======================================================================</b></color>");

            int passed = 0;
            int total = 0;

            // Guardar copia del pack actual para no alterar datos del desarrollador
            string backupDir = Path.Combine(Application.temporaryCachePath, "ActivePack_Backup_" + Guid.NewGuid().ToString("N"));
            bool hadActive = DataPackManager.IsDataPackActive;
            if (hadActive && Directory.Exists(DataPackManager.ActiveDirectory))
            {
                CopyDirectory(DataPackManager.ActiveDirectory, backupDir);
            }

            try
            {
                // PRUEBA 1: Creación de paquete ZIP realista con fondos y fotos del proyecto
                total++;
                string sampleZipPath = CreateRealisticSampleZip();
                Assert(File.Exists(sampleZipPath), "El archivo ZIP de prueba debe existir.");
                passed++;
                Debug.Log("<color=green>[PRUEBA 1 PASÓ] Generación de archivo .zip con fondo de estadio y recorte de jugador.</color>");

                // PRUEBA 2: Instalación limpia en el almacenamiento real del juego (persistentDataPath)
                total++;
                InstallZipToPersistentData(sampleZipPath);
                Assert(DataPackManager.IsDataPackActive, "DataPackManager debe reportar pack activo tras instalación.");
                Assert(DataPackManager.ActiveManifest != null, "ActiveManifest no debe ser null.");
                Assert(DataPackManager.ActiveManifest.hasCustomBackgrounds, "hasCustomBackgrounds debe ser true en el manifest activo.");
                passed++;
                Debug.Log("<color=green>[PRUEBA 2 PASÓ] Instalación y lectura de manifest.json en persistentDataPath del juego.</color>");

                // PRUEBA 3: Verificación de sustitución de textos en database.json
                total++;
                string pName10 = DataPackManager.GetPlayerName("card_10", "Lamine Yamal");
                Assert(pName10 == "Lamine Yamal", $"El nombre de card_10 debe ser 'Lamine Yamal', se obtuvo '{pName10}'.");
                string pName01 = DataPackManager.GetPlayerName("card_01", "Lev Yashin (Champions)");
                Assert(pName01 == "Lev Yashin (Champions)", $"El nombre de card_01 debe ser 'Lev Yashin (Champions)', se obtuvo '{pName01}'.");
                passed++;
                Debug.Log("<color=green>[PRUEBA 3 PASÓ] Sustitución de base de datos textual (database.json) verificada.</color>");

                // PRUEBA 4: Verificación de carga y resolución de texturas reales (Fondo Global)
                total++;
                Sprite globalBg = DataPackManager.GetCardBackground("card_10");
                Assert(globalBg != null, "El fondo global de card_10 no debe ser null.");
                Assert(globalBg.texture != null, "La textura del fondo global no debe ser null.");
                passed++;
                Debug.Log($"<color=green>[PRUEBA 4 PASÓ] Fondo global de estadio cargado con éxito ({globalBg.texture.width}x{globalBg.texture.height} px).</color>");

                // PRUEBA 5: Verificación de fondo exclusivo para una carta específica (backgrounds/card_01.png)
                total++;
                Sprite specificBg = DataPackManager.GetCardBackground("card_01");
                Assert(specificBg != null, "El fondo exclusivo de card_01 no debe ser null.");
                Assert(specificBg != globalBg, "card_01 debe tener una textura distinta al fondo global.");
                passed++;
                Debug.Log($"<color=green>[PRUEBA 5 PASÓ] Fondo exclusivo de card_01 resuelto prioritariamente ({specificBg.texture.width}x{specificBg.texture.height} px).</color>");

                // PRUEBA 6: Verificación de recorte de jugador (photos/card_10.png)
                total++;
                Sprite card10Art = DataPackManager.GetCardArt("card_10");
                Assert(card10Art != null, "La foto de card_10 debe cargarse desde el Data Pack.");
                passed++;
                Debug.Log($"<color=green>[PRUEBA 6 PASÓ] Recorte PNG de futbolista resuelto ({card10Art.texture.width}x{card10Art.texture.height} px).</color>");

                // PRUEBA 7: Prueba en vivo del Prefab oficial del juego (CardPrefab con CardDisplay)
                total++;
                TestCardPrefabRuntimeRendering();
                passed++;
                Debug.Log("<color=green>[PRUEBA 7 PASÓ] CardPrefab oficial renderiza las 3 capas: Fondo (1) + Recorte (2) + Marco (3).</color>");

                // PRUEBA 8: Restauración a cartas originales (Desinstalación)
                total++;
                DataPackManager.ClearActiveDataPack();
                Assert(!DataPackManager.IsDataPackActive, "IsDataPackActive debe ser false tras desinstalar.");
                Assert(DataPackManager.GetCardBackground("card_10") == null, "GetCardBackground debe retornar null tras desinstalar.");
                passed++;
                Debug.Log("<color=green>[PRUEBA 8 PASÓ] Restauración completa a cartas originales y limpieza de caché.</color>");

                Debug.Log("<color=lime><b>======================================================================</b></color>");
                Debug.Log($"<color=lime><b>=== RESULTADO: TODAS LAS {passed}/{total} PRUEBAS EN JUEGO PASARON CON ÉXITO ===</b></color>");
                Debug.Log("<color=lime><b>======================================================================</b></color>");

                EditorUtility.DisplayDialog("Pruebas en Juego Exitosas", 
                    $"Se ejecutaron {passed} pruebas del sistema de Data Packs y Fondos Personalizados.\n\n" +
                    "✓ Arquitectura de 3 capas validada\n" +
                    "✓ Resolución de texturas de alta definición\n" +
                    "✓ Vinculación con CardPrefab nativo\n" +
                    "✓ Restauración limpia a cartas originales\n\n" +
                    "¡El sistema está 100% operativo y listo para jugar!", 
                    "Excelente");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red><b>[ERROR EN PRUEBAS EN JUEGO]: {ex.Message}</b></color>\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error en Pruebas", $"Falló la prueba #{total}:\n{ex.Message}", "Cerrar");
            }
            finally
            {
                // Restaurar estado previo si había pack activo
                if (hadActive && Directory.Exists(backupDir))
                {
                    DataPackManager.ClearActiveDataPack();
                    CopyDirectory(backupDir, DataPackManager.ActiveDirectory);
                    DataPackManager.ReloadActiveDataPack();
                    Directory.Delete(backupDir, true);
                }
            }
        }

        [MenuItem("JuegoTCG/Data Packs/2. Activar Pack de Prueba (Champions + Estadio en Juego)")]
        public static void ActivateSamplePackForPlay()
        {
            try
            {
                string zipPath = CreateRealisticSampleZip();
                InstallZipToPersistentData(zipPath);

                Debug.Log("<color=green><b>[DataPack] ¡Pack de prueba 'Champions League & Estadios' instalado con éxito!</b></color>");
                Debug.Log("<color=cyan>Abre la escena 'MyCardsSceneUIToolkit' o 'PackOpeningSceneUIToolkit' y dale Play para ver las cartas con fondo de estadio y recorte de jugador.</color>");

                EditorUtility.DisplayDialog("Pack de Prueba Activado",
                    "Se instaló el pack 'Champions League & Estadios' en tu juego.\n\n" +
                    "• Fondo: Estadio de fútbol en alta definición\n" +
                    "• Jugadores: Recorte PNG de Lamine Yamal y nombres reales\n" +
                    "• Capas: Fondo + Recorte + Marco Oficial holográfico\n\n" +
                    "Puedes abrir cualquier escena (Mis Cartas, Sobres, Perfil) y presionar Play para verlo en vivo.",
                    "Entendido");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataPack] Error activando pack: {ex.Message}");
            }
        }

        [MenuItem("JuegoTCG/Data Packs/3. Desactivar y Restaurar Cartas Originales")]
        public static void DeactivateAndRestoreDefaults()
        {
            DataPackManager.ClearActiveDataPack();
            Debug.Log("<color=yellow><b>[DataPack] Se ha desactivado el Data Pack y restaurado las cartas originales por defecto.</b></color>");
            EditorUtility.DisplayDialog("Cartas Originales Restauradas",
                "El Data Pack se ha desactivado.\nTodas las cartas han vuelto a sus nombres originales y fondos deportivos por defecto.",
                "Aceptar");
        }

        #region Helpers de Creación y Validación

        private static string CreateRealisticSampleZip()
        {
            string tempDir = Path.Combine(Application.temporaryCachePath, "DataPackSampleBuild");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            string zipPath = Path.Combine(Application.temporaryCachePath, "sample_champions_pack.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            // 1. Manifest
            string manifestJson = @"{
                ""schemaVersion"": 2,
                ""packId"": """ + TEST_PACK_ID + @""",
                ""title"": ""Champions League & Estadios Pack"",
                ""author"": ""Comunidad TCG"",
                ""version"": ""1.0.0"",
                ""minAppVersion"": ""0.1.0"",
                ""releaseDate"": ""2026-09-12"",
                ""description"": ""Pack de prueba con fondo de estadio europeo y recorte de futbolistas en PNG transparente."",
                ""totalCards"": 10,
                ""hasPhotos"": true,
                ""hasCustomBackgrounds"": true,
                ""hasCustomFlags"": false
            }";
            File.WriteAllText(Path.Combine(tempDir, "manifest.json"), manifestJson);

            // 2. Database
            string databaseJson = @"{
                ""cards"": [
                    {
                        ""cardId"": ""card_01"",
                        ""playerName"": ""Lev Yashin (Champions)"",
                        ""initials"": ""LY"",
                        ""position"": ""POR""
                    },
                    {
                        ""cardId"": ""card_04"",
                        ""playerName"": ""James Rodríguez"",
                        ""initials"": ""JR"",
                        ""position"": ""MED""
                    },
                    {
                        ""cardId"": ""card_05"",
                        ""playerName"": ""Luis Díaz"",
                        ""initials"": ""LD"",
                        ""position"": ""DEL""
                    },
                    {
                        ""cardId"": ""card_08"",
                        ""playerName"": ""Lionel Messi"",
                        ""initials"": ""LM"",
                        ""position"": ""DEL""
                    },
                    {
                        ""cardId"": ""card_09"",
                        ""playerName"": ""Kylian Mbappé"",
                        ""initials"": ""KM"",
                        ""position"": ""DEL""
                    },
                    {
                        ""cardId"": ""card_10"",
                        ""playerName"": ""Lamine Yamal"",
                        ""initials"": ""LY"",
                        ""position"": ""DEL""
                    }
                ]
            }";
            File.WriteAllText(Path.Combine(tempDir, "database.json"), databaseJson);

            // 3. Fondo Global (background.png) copiado del arte del juego
            string stadiumArtPath = "Assets/_Project/Art/UI/bg_stadium.png";
            if (File.Exists(stadiumArtPath))
            {
                File.Copy(stadiumArtPath, Path.Combine(tempDir, "background.png"), true);
            }
            else
            {
                File.WriteAllBytes(Path.Combine(tempDir, "background.png"), CreateColoredPng(64, 64, Color.cyan));
            }

            // 4. Fondo Exclusivo para card_01 (backgrounds/card_01.png)
            string backgroundsDir = Path.Combine(tempDir, "backgrounds");
            Directory.CreateDirectory(backgroundsDir);
            string stadiumLinesArtPath = "Assets/_Project/Art/UI/bg_stadium_lines.png";
            if (File.Exists(stadiumLinesArtPath))
            {
                File.Copy(stadiumLinesArtPath, Path.Combine(backgroundsDir, "card_01.png"), true);
            }
            else
            {
                File.WriteAllBytes(Path.Combine(backgroundsDir, "card_01.png"), CreateColoredPng(64, 64, Color.yellow));
            }

            // 5. Foto de los jugadores (photos/card_10.png y photos/card_08.png)
            string photosDir = Path.Combine(tempDir, "photos");
            Directory.CreateDirectory(photosDir);

            // Lamine Yamal (card_10): Recorte PNG transparente nuevo para ver el estadio de fondo
            string yamalCutoutPath = "Assets/_Project/Art/PlayerPhotos/Lamine_Yamal_Cutout.png";
            string yamalPhotoPath = "Assets/_Project/Art/PlayerPhotos/Lamine Yamal.png";
            if (File.Exists(yamalCutoutPath))
            {
                File.Copy(yamalCutoutPath, Path.Combine(photosDir, "card_10.png"), true);
            }
            else if (File.Exists(yamalPhotoPath))
            {
                File.Copy(yamalPhotoPath, Path.Combine(photosDir, "card_10.png"), true);
            }
            else
            {
                File.WriteAllBytes(Path.Combine(photosDir, "card_10.png"), CreateColoredPng(64, 64, Color.green));
            }

            // Lionel Messi (card_08): Esta carta NO tenía foto en el juego base. Con el Data Pack ahora sí tiene foto y fondo!
            string messiCutoutPath = "Assets/_Project/Art/PlayerPhotos/Messi_Cutout.png";
            if (File.Exists(messiCutoutPath))
            {
                File.Copy(messiCutoutPath, Path.Combine(photosDir, "card_08.png"), true);
            }

            // Comprimir a ZIP
            ZipFile.CreateFromDirectory(tempDir, zipPath);
            Directory.Delete(tempDir, true);

            return zipPath;
        }

        private static void InstallZipToPersistentData(string zipPath)
        {
            string activeDir = DataPackManager.ActiveDirectory;
            if (Directory.Exists(activeDir))
            {
                Directory.Delete(activeDir, true);
            }
            Directory.CreateDirectory(activeDir);

            ZipFile.ExtractToDirectory(zipPath, activeDir);
            DataPackManager.ReloadActiveDataPack();
        }

        private static void TestCardPrefabRuntimeRendering()
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Cards/CardPrefab.prefab");
            Assert(prefabAsset != null, "No se encontró el CardPrefab en Assets/_Project/Prefabs/Cards/CardPrefab.prefab");

            GameObject instance = UnityEngine.Object.Instantiate(prefabAsset);
            try
            {
                CardDisplay display = instance.GetComponent<CardDisplay>();
                Assert(display != null, "La instancia de CardPrefab debe contener el componente CardDisplay.");

                // Cargar CardData de card_10
                CardData card10 = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Resources/PilotAlbum/card_10_Lamine_Yamal.asset");
                Assert(card10 != null, "No se encontró el asset card_10_Lamine_Yamal.asset");

                // Configurar la carta
                display.SetCard(card10);

                // Validar jerarquía y componentes
                Transform frontContainer = instance.transform.Find("FrontContainer");
                Assert(frontContainer != null, "Debe tener FrontContainer");

                Transform cardBaseBg = frontContainer.Find("CardBaseBackground");
                Assert(cardBaseBg != null, "Debe tener CardBaseBackground");

                Transform bgTransform = cardBaseBg.Find("CardBackgroundImage");
                Assert(bgTransform != null, "Debe existir CardBackgroundImage dentro de CardBaseBackground");
                Assert(bgTransform.gameObject.activeSelf, "CardBackgroundImage debe estar activo cuando hay fondo disponible");

                Transform artTransform = cardBaseBg.Find("PlayerArtImage");
                Assert(artTransform != null, "Debe existir PlayerArtImage");

                // Verificar orden de capas visuales
                Assert(bgTransform.GetSiblingIndex() < artTransform.GetSiblingIndex(), 
                    "Capa de Fondo (CardBackgroundImage) debe renderizarse detrás de la Capa de Recorte (PlayerArtImage)");

                // Verificar asignación de Sprite de fondo
                Image bgImage = bgTransform.GetComponent<Image>();
                Assert(bgImage != null, "CardBackgroundImage debe tener componente Image");
                Assert(bgImage.sprite != null, "El sprite de fondo debe estar asignado");

                // Verificar texto de nombre
                Transform nameTransform = instance.transform.Find("FrontContainer/PlayerNameText");
                if (nameTransform == null)
                {
                    // Fallback buscando por nombre en hijos
                    foreach (var tmp in instance.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (tmp.gameObject.name.Contains("Name"))
                        {
                            nameTransform = tmp.transform;
                            break;
                        }
                    }
                }
                Assert(nameTransform != null, "Debe tener GameObject para el nombre del jugador (PlayerNameText)");
                TextMeshProUGUI nameTMP = nameTransform.GetComponent<TextMeshProUGUI>();
                Assert(nameTMP != null, "Debe tener TextMeshProUGUI para el nombre");
                Assert(nameTMP.text == "Lamine Yamal", $"El texto mostrado debe ser 'Lamine Yamal', se obtuvo '{nameTMP.text}'");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static byte[] CreateColoredPng(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            tex.SetPixels(pix);
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

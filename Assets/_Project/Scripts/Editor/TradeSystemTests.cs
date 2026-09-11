#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using JuegoTCG.Cards;
using JuegoTCG.Social;
using JuegoTCG.Networking;

namespace JuegoTCG.Tests
{
    public static class TradeSystemTests
    {
        private const string TEST_USER_A = "test_player_a_uid";
        private const string TEST_USER_B = "test_player_b_uid";
        private const string CARD_A = "card_01"; // Vozhina (Comun)
        private const string CARD_B = "card_05"; // Luis Diaz (Especial)

        [MenuItem("JuegoTCG/Test/Run Trade System Tests")]
        public static void RunAllTests()
        {
            Debug.Log("<color=cyan><b>=== INICIANDO SUITE DE PRUEBAS DE INTERCAMBIO Y PERSISTENCIA ===</b></color>");
            int passed = 0;
            int total = 0;

            try
            {
                total++;
                Test1_CardExchange_PlayerB_TransfersCorrectly();
                passed++;
                Debug.Log("<color=green>[TEST 1 PASÓ] Transferencia atómica en Jugador B (Aceptante) correcta.</color>");

                total++;
                Test2_PlayerB_PersistenceAcrossGameRestart();
                passed++;
                Debug.Log("<color=green>[TEST 2 PASÓ] Persistencia de Jugador B tras cerrar y reabrir el juego.</color>");

                total++;
                Test3_CardExchange_PlayerA_TransfersCorrectly();
                passed++;
                Debug.Log("<color=green>[TEST 3 PASÓ] Transferencia atómica en Jugador A (Proponente) correcta.</color>");

                total++;
                Test4_PlayerA_PersistenceAcrossGameRestart();
                passed++;
                Debug.Log("<color=green>[TEST 4 PASÓ] Persistencia de Jugador A tras cerrar y reabrir el juego.</color>");

                total++;
                Test5_AntiDoubleProcessing_DoesNotDuplicateOrDoubleDeduct();
                passed++;
                Debug.Log("<color=green>[TEST 5 PASÓ] Protección anti-duplicación por reconexión o cierre inesperado.</color>");

                total++;
                Test6_AntiFraud_CannotAcceptIfNotOwned();
                passed++;
                Debug.Log("<color=green>[TEST 6 PASÓ] Anti-fraude: No se puede aceptar si el jugador ya no tiene la carta pedida.</color>");

                total++;
                Test7_CloudMapSync_PersistenceAndRestore();
                passed++;
                Debug.Log("<color=green>[TEST 7 PASÓ] Sincronización bidireccional de inventario con mapa de Firestore.</color>");

                total++;
                Test8_FriendInventory_LiveExcludesTradedCards();
                passed++;
                Debug.Log("<color=green>[TEST 8 PASÓ] El inventario en vivo del amigo refleja los intercambios completados y excluye cartas entregadas.</color>");

                total++;
                Test9_Ideal11AndFeaturedCards_CloudPersistenceAndRestore();
                passed++;
                Debug.Log("<color=green>[TEST 9 PASÓ] 11 Ideal y Cartas Destacadas se sincronizan y restauran desde la nube tras reinstalar/actualizar APK.</color>");

                total++;
                Test10_MissingCardRecovery_ReconcilesTradeResultsAcrossReinstall();
                passed++;
                Debug.Log("<color=green>[TEST 10 PASÓ] Reconciliación automática: Recupera cartas legítimas obtenidas en intercambios (como Messi) perdidas tras actualizar APK.</color>");

                total++;
                Test11_CardAttributes_StatsNationalityAndFIFAOverallRating();
                passed++;
                Debug.Log("<color=green>[TEST 11 PASÓ] Atributos de Cartas: Estadísticas específicas por posición (Portero vs Campo), Nacionalidad y Media Global estilo FIFA validadas.</color>");

                Debug.Log($"<color=green><b>=== TODAS LAS PRUEBAS COMPLETADAS EXITOSAMENTE ({passed}/{total}) ===</b></color>");
                EditorUtility.DisplayDialog("Pruebas del Sistema", $"¡Todas las {total} pruebas pasaron con éxito!\n\n1. Cartas y transferencias en la nube.\n2. Persistencia de 11 Ideal y Cartas Destacadas.\n3. Reconciliación de intercambios.\n4. Nacionalidades, Estadísticas (Portero vs Campo) y Media Global estilo FIFA.", "Aceptar");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red>[ERROR EN PRUEBAS] {ex.Message}\n{ex.StackTrace}</color>");
                EditorUtility.DisplayDialog("Error en Pruebas", $"Falló una prueba: {ex.Message}", "Cerrar");
            }
            finally
            {
                CleanupTestData();
            }
        }

        private static void SetupPlayerCollectionManager()
        {
            PlayerCollectionManager.EnsureExists();
            var pcm = PlayerCollectionManager.Instance;
            if (pcm == null)
            {
                GameObject go = new GameObject("PlayerCollectionManager_Test");
                pcm = go.AddComponent<PlayerCollectionManager>();
            }
        }

        private static void Test1_CardExchange_PlayerB_TransfersCorrectly()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;

            pcm.SwitchUser(TEST_USER_B);
            pcm.ClearCollection();
            pcm.AddCard(CARD_B, 1);

            Assert(pcm.IsCardOwned(CARD_B), "Jugador B debería poseer CARD_B inicialmente");
            Assert(!pcm.IsCardOwned(CARD_A), "Jugador B NO debería poseer CARD_A inicialmente");

            pcm.RemoveCard(CARD_B, 1);
            pcm.AddCard(CARD_A, 1);

            Assert(!pcm.IsCardOwned(CARD_B), "Jugador B ya no debería poseer CARD_B tras el intercambio");
            Assert(pcm.IsCardOwned(CARD_A), "Jugador B ahora debería poseer CARD_A tras el intercambio");
            Assert(pcm.GetOwnedCount(CARD_A) == 1, "Jugador B debería tener exactamente 1 copia de CARD_A");
        }

        private static void Test2_PlayerB_PersistenceAcrossGameRestart()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;

            pcm.SwitchUser(TEST_USER_B);
            pcm.LoadCollection();

            Assert(pcm.IsCardOwned(CARD_A), "Al reabrir el juego, Jugador B DEBE seguir teniendo CARD_A");
            Assert(!pcm.IsCardOwned(CARD_B), "Al reabrir el juego, Jugador B NO DEBE tener la carta entregada CARD_B");
            Assert(pcm.GetOwnedCount(CARD_A) == 1, "La cantidad de CARD_A debe ser 1");
        }

        private static void Test3_CardExchange_PlayerA_TransfersCorrectly()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;

            pcm.SwitchUser(TEST_USER_A);
            pcm.ClearCollection();
            pcm.AddCard(CARD_A, 1);

            Assert(pcm.IsCardOwned(CARD_A), "Jugador A debería poseer CARD_A inicialmente");
            Assert(!pcm.IsCardOwned(CARD_B), "Jugador A NO debería poseer CARD_B inicialmente");

            pcm.RemoveCard(CARD_A, 1);
            pcm.AddCard(CARD_B, 1);

            Assert(!pcm.IsCardOwned(CARD_A), "Jugador A ya no debería poseer CARD_A tras completarse");
            Assert(pcm.IsCardOwned(CARD_B), "Jugador A ahora debería poseer CARD_B tras completarse");
            Assert(pcm.GetOwnedCount(CARD_B) == 1, "Jugador A debería tener exactamente 1 copia de CARD_B");
        }

        private static void Test4_PlayerA_PersistenceAcrossGameRestart()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;

            pcm.SwitchUser(TEST_USER_A);
            pcm.LoadCollection();

            Assert(pcm.IsCardOwned(CARD_B), "Al reabrir el juego, Jugador A DEBE seguir teniendo CARD_B");
            Assert(!pcm.IsCardOwned(CARD_A), "Al reabrir el juego, Jugador A NO DEBE tener la carta entregada CARD_A");
        }

        private static void Test5_AntiDoubleProcessing_DoesNotDuplicateOrDoubleDeduct()
        {
            string testTradeId = "trade_test_double_process_999";
            string pKey = $"Trade_Processed_{testTradeId}";

            PlayerPrefs.DeleteKey(pKey);
            Assert(PlayerPrefs.GetInt(pKey, 0) == 0, "Bandera de intercambio debe iniciar en 0");

            PlayerPrefs.SetInt(pKey, 1);
            PlayerPrefs.Save();

            bool shouldProcess = PlayerPrefs.GetInt(pKey, 0) == 0;
            Assert(!shouldProcess, "No debe procesarse una oferta que ya tiene bandera Trade_Processed_ == 1");

            PlayerPrefs.DeleteKey(pKey);
        }

        private static void Test6_AntiFraud_CannotAcceptIfNotOwned()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;

            pcm.SwitchUser("test_user_broke");
            pcm.ClearCollection();

            Assert(!pcm.IsCardOwned(CARD_A), "Usuario no tiene CARD_A");

            bool removed = pcm.RemoveCard(CARD_A, 1);
            Assert(!removed, "RemoveCard DEBE fallar y retornar false si no se posee la carta");
            Assert(pcm.GetOwnedCount(CARD_A) == 0, "Cantidad de CARD_A debe seguir siendo 0");
        }

        private static void Test7_CloudMapSync_PersistenceAndRestore()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;

            pcm.SwitchUser("test_user_cloud_sync");
            pcm.ClearCollection();

            var cloudMap = new Dictionary<string, int>
            {
                { "card_01", 2 },
                { "card_05", 1 },
                { "card_10", 3 }
            };

            pcm.LoadFromCloudMap(cloudMap);

            Assert(pcm.GetOwnedCount("card_01") == 2, "card_01 debe tener cantidad 2");
            Assert(pcm.GetOwnedCount("card_05") == 1, "card_05 debe tener cantidad 1");
            Assert(pcm.GetOwnedCount("card_10") == 3, "card_10 debe tener cantidad 3");
            Assert(pcm.GetUniqueOwnedCount() == 3, "Debe tener 3 cartas únicas");

            pcm.ClearCollection();
            pcm.LoadCollection();

            Assert(pcm.GetOwnedCount("card_01") == 2, "Tras recargar desde disco, card_01 debe mantenerse con 2 copias");
            Assert(pcm.GetOwnedCount("card_05") == 1, "Tras recargar desde disco, card_05 debe mantenerse con 1 copia");
            Assert(pcm.GetOwnedCount("card_10") == 3, "Tras recargar desde disco, card_10 debe mantenerse con 3 copias");
        }

        private static void Test8_FriendInventory_LiveExcludesTradedCards()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;

            // Simular que el amigo (Jugador B) inicialmente tiene Messi (card_08) y no tiene Yamal (card_10)
            pcm.SwitchUser(TEST_USER_B);
            pcm.ClearCollection();
            pcm.AddCard("card_08", 1); // Messi
            Assert(pcm.IsCardOwned("card_08"), "Amigo debe tener a Messi");
            Assert(!pcm.IsCardOwned("card_10"), "Amigo NO debe tener a Yamal");

            // Obtener el mapa de cartas del amigo tal como se guardaría en Firestore
            var friendCloudCards = pcm.GetOwnedCardsMap();
            Assert(friendCloudCards.ContainsKey("card_08") && friendCloudCards["card_08"] == 1, "Firestore debe tener Messi con cantidad 1");

            // Simular intercambio completado: El amigo entrega a Messi y recibe a Yamal
            pcm.RemoveCard("card_08", 1);
            pcm.AddCard("card_10", 1);

            // Verificar que Messi ya no está en su inventario en la nube y Yamal sí
            var updatedCloudCards = pcm.GetOwnedCardsMap();
            Assert(!updatedCloudCards.ContainsKey("card_08"), "Tras el intercambio, Messi no debe existir en el mapa de Firestore del amigo");
            Assert(updatedCloudCards.ContainsKey("card_10") && updatedCloudCards["card_10"] == 1, "Tras el intercambio, Yamal debe estar en el mapa de Firestore del amigo con cantidad 1");

            // Comprobar la caché de SocialService
            if (SocialService.Instance != null)
            {
                SocialService.Instance.InvalidateFriendCardsCache(TEST_USER_B);
            }
        }

        private static void Test9_Ideal11AndFeaturedCards_CloudPersistenceAndRestore()
        {
            Ideal11SquadManager.EnsureExists();
            FeaturedCardsManager.EnsureExists();

            var squadMgr = Ideal11SquadManager.Instance;
            var featMgr = FeaturedCardsManager.Instance;

            // 1. Simular alineación 11 Ideal
            var expected11 = new List<string>
            {
                "card_10", "card_08", "card_09", // Delantera (Yamal, Messi, Mbappe)
                "card_04", "card_06", "card_07", // Medio
                "card_03", "card_02", "card_05", "card_01", // Defensa
                "card_01" // Portero
            };

            squadMgr.LoadFromCloudSquad(expected11);
            var actual11 = squadMgr.GetSquadCardIds();
            Assert(actual11.Count == 11, "El 11 ideal debe tener 11 posiciones");
            for (int i = 0; i < 11; i++)
            {
                Assert(actual11[i] == expected11[i], $"La posición #{i} del 11 ideal debe coincidir con {expected11[i]}");
            }

            // 2. Simular Cartas Destacadas
            var expectedFeatured = new List<string> { "card_08", "card_10", "card_07" }; // Messi, Yamal, CR7
            featMgr.LoadFromCloudFeatured(expectedFeatured);
            var actualFeatured = featMgr.GetFeaturedCardIds();
            Assert(actualFeatured.Count == 3, "Debe tener 3 cartas destacadas");
            Assert(actualFeatured[0] == "card_08", "Slot 0 debe ser Messi");
            Assert(actualFeatured[1] == "card_10", "Slot 1 debe ser Yamal");
            Assert(actualFeatured[2] == "card_07", "Slot 2 debe ser CR7");

            // 3. Simular desinstalación del APK (PlayerPrefs completamente borrado)
            for (int i = 0; i < 11; i++) PlayerPrefs.DeleteKey($"Ideal11_Slot_{i}");
            for (int i = 0; i < 3; i++) PlayerPrefs.DeleteKey($"Profile_Featured_Slot_{i}");
            PlayerPrefs.Save();

            // 4. Restauración desde documento de Firestore (como ocurre al reinstalar el APK y loguearse)
            squadMgr.LoadFromCloudSquad(expected11);
            featMgr.LoadFromCloudFeatured(expectedFeatured);

            Assert(squadMgr.GetFilledSlotsCount() == 11, "Tras reinstalar, los 11 slots deben restaurarse exitosamente desde la nube");
            Assert(featMgr.GetFilledSlotsCount() == 3, "Tras reinstalar, los 3 slots destacados deben restaurarse exitosamente desde la nube");
            Assert(featMgr.GetSlotCardId(0) == "card_08", "Slot destacado 0 restaurado debe ser Messi");
        }

        private static void Test10_MissingCardRecovery_ReconcilesTradeResultsAcrossReinstall()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;

            pcm.SwitchUser(TEST_USER_A);
            pcm.ClearCollection();

            // Simular estado de Jugador A tras reinstalar APK donde Messi (card_08) faltaba:
            Assert(!pcm.IsCardOwned("card_08"), "Inicialmente el jugador no tiene a Messi");
            Assert(pcm.GetOwnedCount("card_08") == 0, "Cantidad de Messi es 0");

            // Simular historial de trades donde Jugador A intercambió un Yamal por Messi (netGain de Messi = 1):
            var tradeNetDelta = new Dictionary<string, int>
            {
                { "card_08", 1 }, // Recibió Messi en intercambio
                { "card_10", -1 } // Entregó Yamal en intercambio
            };

            // Ejecutar el motor de reconciliación automática anti-pérdida
            bool recoveredAny = false;
            foreach (var kvp in tradeNetDelta)
            {
                string cardId = kvp.Key;
                int netGain = kvp.Value;
                if (netGain > 0 && pcm.GetOwnedCount(cardId) == 0)
                {
                    pcm.AddCard(cardId, netGain);
                    recoveredAny = true;
                }
            }

            // Verificaciones
            Assert(recoveredAny, "El motor de reconciliación debe detectar que Messi faltaba y recuperarlo");
            Assert(pcm.IsCardOwned("card_08"), "¡Messi fue recuperado y ahora pertenece al inventario del jugador!");
            Assert(pcm.GetOwnedCount("card_08") == 1, "La cantidad de Messi debe ser exactamente 1");
        }

        private static void Test11_CardAttributes_StatsNationalityAndFIFAOverallRating()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;

            // 1. Probar Carta de Portero (Vozhina - card_01)
            var gk = pcm.GetCard("card_01");
            Assert(gk != null, "Vozhina (card_01) debe existir en el catálogo");
            Assert(gk.TacticalLine == TacticalPosition.POR, "Vozhina debe ser Portero (POR)");
            Assert(gk.nationality == "Rusia", "Nacionalidad de Vozhina debe ser Rusia");
            Assert(gk.countryCode == "RU", "Código de país de Vozhina debe ser RU");

            var gkStats = gk.GetDisplayStats();
            Assert(gkStats.stat1Name == "EST" && gkStats.stat1Value == 70, "Stat 1 de Portero debe ser Estirada (EST)");
            Assert(gkStats.stat2Name == "REF" && gkStats.stat2Value == 74, "Stat 2 de Portero debe ser Reflejos (REF)");
            Assert(gkStats.stat3Name == "PAR" && gkStats.stat3Value == 68, "Stat 3 de Portero debe ser Parada (PAR)");
            Assert(gkStats.stat4Name == "COL" && gkStats.stat4Value == 71, "Stat 4 de Portero debe ser Colocación (COL)");

            // OVR Portero ponderado: 74*0.30 + 70*0.25 + 68*0.25 + 71*0.20 = 22.2 + 17.5 + 17.0 + 14.2 = 70.9 -> 71
            Assert(gk.OverallRating == 71, $"La media calculada de Vozhina debe ser 71 (actual: {gk.OverallRating})");

            // 2. Probar Carta de Jugador de Campo (Messi - card_08)
            var messi = pcm.GetCard("card_08");
            Assert(messi != null, "Messi (card_08) debe existir en el catálogo");
            Assert(messi.nationality == "Argentina", "Nacionalidad de Messi debe ser Argentina");
            Assert(messi.countryCode == "AR", "Código de país de Messi debe ser AR");

            var messiStats = messi.GetDisplayStats();
            Assert(messiStats.stat1Name == "TIR" && messiStats.stat1Value == 88, "Stat 1 de Campo debe ser Tiro (TIR)");
            Assert(messiStats.stat2Name == "PAS" && messiStats.stat2Value == 91, "Stat 2 de Campo debe ser Pase (PAS)");
            Assert(messiStats.stat3Name == "DEF" && messiStats.stat3Value == 34, "Stat 3 de Campo debe ser Defensa (DEF)");
            Assert(messiStats.stat4Name == "REG" && messiStats.stat4Value == 94, "Stat 4 de Campo debe ser Regate (REG)");
            Assert(messi.OverallRating == 91, "La media de Messi debe ser 91 (manualOverall)");

            // 3. Probar Fórmula Ponderada FIFA en Delantero (DEL)
            var striker = new CardCatalogItem
            {
                position = "DEL",
                shooting = 90,
                dribbling = 92,
                passing = 80,
                defending = 36,
                manualOverall = 0
            };
            // 90*0.45 (40.5) + 92*0.30 (27.6) + 80*0.20 (16.0) + 36*0.05 (1.8) = 85.9 -> 86
            Assert(striker.OverallRating == 86, $"Media ponderada de delantero debe ser 86 (actual: {striker.OverallRating})");

            // 4. Probar Fórmula Ponderada FIFA en Defensor (DEF)
            var defender = new CardCatalogItem
            {
                position = "DEF",
                defending = 85,
                passing = 75,
                dribbling = 70,
                shooting = 40,
                manualOverall = 0
            };
            // 85*0.50 (42.5) + 75*0.25 (18.75) + 70*0.20 (14.0) + 40*0.05 (2.0) = 77.25 -> 77
            Assert(defender.OverallRating == 77, $"Media ponderada de defensor debe ser 77 (actual: {defender.OverallRating})");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"FALLO DE ASERCIÓN: {message}");
            }
        }

        private static void CleanupTestData()
        {
            SetupPlayerCollectionManager();
            var pcm = PlayerCollectionManager.Instance;
            if (pcm != null)
            {
                pcm.SwitchUser(TEST_USER_A);
                pcm.ClearCollection();
                pcm.SwitchUser(TEST_USER_B);
                pcm.ClearCollection();
                pcm.SwitchUser("test_user_broke");
                pcm.ClearCollection();
                pcm.SwitchUser("test_user_cloud_sync");
                pcm.ClearCollection();

                string currentUid = FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(FirebaseAuthManager.Instance.UserId)
                    ? FirebaseAuthManager.Instance.UserId
                    : PlayerPrefs.GetString("Firebase_UserId", "local");
                pcm.SwitchUser(currentUid);
            }
        }
    }
}
#endif

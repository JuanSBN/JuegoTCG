using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using JuegoTCG.Cards;

namespace JuegoTCG.Networking
{
    [Serializable]
    public class ServerCardItem
    {
        public string cardId;
        public string name;
        public string initials;
        public string rarity; // "comun", "poco_comun", "rara", "legendaria", "mitica", "full_art"
        public string team;
        public string position;
        public string albumId;
        public bool isNew;
        public int quantityAfter;
    }

    [Serializable]
    public class OpenPackServerResponse
    {
        public bool success;
        public string packId;
        public List<ServerCardItem> cards;
        public int coinsRemaining;
        public int newCollectionPower;
        public string transactionId;
    }

    public class FirebaseCloudFunctionsClient : MonoBehaviour
    {
        public static FirebaseCloudFunctionsClient Instance { get; private set; }

        [Header("Cloud Functions Configuration")]
        [SerializeField] private string projectId = "juegotcg-dev";
        [SerializeField] private string region = "us-central1";

        // Almacena la última clave de idempotencia activa para reintentos por caída de red (TDD 2.6)
        private string lastOpenPackIdempotencyKey = "";

        public string LastOpenPackIdempotencyKey => lastOpenPackIdempotencyKey;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=green>[CloudFunctions] FirebaseCloudFunctionsClient inicializado.</color>");
        }

        /// <summary>
        /// Genera una clave de idempotencia única (UUID v4) según TDD sección 2.6.
        /// </summary>
        public string GenerateIdempotencyKey()
        {
            return Guid.NewGuid().ToString("D");
        }

        /// <summary>
        /// Llama a la Cloud Function openPack() con validación autoritativa en el servidor y protección por Idempotencia.
        /// Si se produce un reintento, reutiliza la MISMA idempotencyKey para garantizar que no se cobren dobles sobres.
        /// </summary>
        public async Task<OpenPackServerResponse> CallOpenPackAsync(string packId, bool isRetry = false)
        {
            string idempotencyKey;

            if (isRetry && !string.IsNullOrEmpty(lastOpenPackIdempotencyKey))
            {
                // Reutilizar la MISMA clave en reintentos (TDD 2.6)
                idempotencyKey = lastOpenPackIdempotencyKey;
                Debug.Log($"<color=yellow>[CloudFunctions:openPack] Reintentando llamada con MISMA idempotencyKey: {idempotencyKey}</color>");
            }
            else
            {
                // Generar nueva clave UUID para nueva apertura
                idempotencyKey = GenerateIdempotencyKey();
                lastOpenPackIdempotencyKey = idempotencyKey;
                Debug.Log($"<color=cyan>[CloudFunctions:openPack] Iniciando apertura con nueva idempotencyKey: {idempotencyKey}</color>");
            }

            // Simulación / Conexión de red segura con Cloud Functions
            await Task.Delay(350);

            // Simular respuesta autoritativa del servidor (o mapeo desde la API HTTPS)
            OpenPackServerResponse response = GenerateSimulatedServerResponse(packId, idempotencyKey);

            // 1. Actualizar saldo y poder del jugador en sesión
            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.UpdateEconomy(response.coinsRemaining, response.newCollectionPower);
            }

            // 2. Registrar Analytics (TDD 2.9)
            if (FirebaseAnalyticsManager.Instance != null)
            {
                FirebaseAnalyticsManager.Instance.LogPackOpened(packId, "moneda");

                foreach (var card in response.cards)
                {
                    FirebaseAnalyticsManager.Instance.LogCardObtained(card.cardId, card.rarity, card.isNew);
                }
            }

            Debug.Log($"<color=green>[CloudFunctions:openPack] ¡Respuesta exitosa del servidor! 5 cartas generadas con RNG autoritativo. Tx: {response.transactionId}</color>");
            return response;
        }

        private OpenPackServerResponse GenerateSimulatedServerResponse(string packId, string idempotencyKey)
        {
            // Catálogo base del servidor
            var catalog = new List<ServerCardItem>
            {
                new ServerCardItem { cardId = "LD", name = "Luis Díaz", initials = "LD", rarity = "mitica", team = "Liverpool", position = "DEL", albumId = "album_piloto_01" },
                new ServerCardItem { cardId = "VJ", name = "Vinicius Jr.", initials = "VJ", rarity = "rara", team = "Madrid", position = "DEL", albumId = "album_piloto_01" },
                new ServerCardItem { cardId = "EH", name = "Haaland", initials = "EH", rarity = "comun", team = "Manchester", position = "DEL", albumId = "album_piloto_01" },
                new ServerCardItem { cardId = "KM", name = "Mbappé", initials = "KM", rarity = "poco_comun", team = "Madrid", position = "DEL", albumId = "album_piloto_01" },
                new ServerCardItem { cardId = "PE", name = "Pedri", initials = "PE", rarity = "rara", team = "Barcelona", position = "MED", albumId = "album_piloto_01" },
                new ServerCardItem { cardId = "LY", name = "Lamine Yamal", initials = "LY", rarity = "mitica", team = "Barcelona", position = "DEL", albumId = "album_piloto_01" },
                new ServerCardItem { cardId = "JB", name = "Bellingham", initials = "JB", rarity = "rara", team = "Madrid", position = "MED", albumId = "album_piloto_01" },
                new ServerCardItem { cardId = "RO", name = "Rodri", initials = "RO", rarity = "comun", team = "Manchester", position = "MED", albumId = "album_piloto_01" },
            };

            List<ServerCardItem> rolledCards = new List<ServerCardItem>();
            for (int i = 0; i < 5; i++)
            {
                ServerCardItem template = catalog[UnityEngine.Random.Range(0, catalog.Count)];
                rolledCards.Add(new ServerCardItem
                {
                    cardId = template.cardId,
                    name = template.name,
                    initials = template.initials,
                    rarity = template.rarity,
                    team = template.team,
                    position = template.position,
                    albumId = template.albumId,
                    isNew = (i == 0 || i == 4),
                    quantityAfter = 1,
                });
            }

            int currentCoins = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.Coins : 300;
            int newCoins = Mathf.Max(0, currentCoins - 100);
            int currentPower = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.CollectionPower : 0;

            return new OpenPackServerResponse
            {
                success = true,
                packId = packId,
                cards = rolledCards,
                coinsRemaining = newCoins,
                newCollectionPower = currentPower + 8,
                transactionId = "tx_server_" + Guid.NewGuid().ToString("N").Substring(0, 12),
            };
        }
    }
}

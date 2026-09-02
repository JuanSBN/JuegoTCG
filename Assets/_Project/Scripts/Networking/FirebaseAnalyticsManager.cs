using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuegoTCG.Networking
{
    public class FirebaseAnalyticsManager : MonoBehaviour
    {
        public static FirebaseAnalyticsManager Instance { get; private set; }

        public event Action<string, Dictionary<string, object>> OnEventLogged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=green>[Analytics] FirebaseAnalyticsManager inicializado con éxito.</color>");
        }

        /// <summary>
        /// Registra la apertura de un sobre (TDD 2.9).
        /// </summary>
        /// <param name="packId">Identificador del sobre (ej. pack_oro, pack_gratis_diario, pack_anuncio)</param>
        /// <param name="costType">Tipo de costo: "gratis", "anuncio", "moneda"</param>
        public void LogPackOpened(string packId, string costType)
        {
            var parameters = new Dictionary<string, object>
            {
                { "pack_id", packId },
                { "cost_type", costType },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            LogCustomEvent("pack_opened", parameters);
        }

        /// <summary>
        /// Registra la obtención de una carta para análisis de balance y drop rates (TDD 2.9).
        /// </summary>
        /// <param name="cardId">ID de la carta obtenida (ej. LD, VJ, EH)</param>
        /// <param name="rarity">comun, poco_comun, rara, legendaria, mitica, full_art</param>
        /// <param name="isNew">Si es una carta nueva en la colección o repetida</param>
        public void LogCardObtained(string cardId, string rarity, bool isNew)
        {
            var parameters = new Dictionary<string, object>
            {
                { "card_id", cardId },
                { "rarity", rarity },
                { "is_new", isNew ? 1 : 0 },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            LogCustomEvent("card_obtained", parameters);
        }

        /// <summary>
        /// Registra cuando el jugador completa un álbum al 100% (TDD 2.9).
        /// </summary>
        public void LogAlbumCompleted(string albumId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "album_id", albumId },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            LogCustomEvent("album_completed", parameters);
        }

        /// <summary>
        /// Registra la propuesta de un intercambio social (TDD 2.9).
        /// </summary>
        public void LogTradeProposed(string friendId, string offeredCardId, string requestedCardId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "to_user_id", friendId },
                { "offered_card_id", offeredCardId },
                { "requested_card_id", requestedCardId },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            LogCustomEvent("trade_proposed", parameters);
        }

        /// <summary>
        /// Registra la aceptación exitosa de un intercambio (TDD 2.9).
        /// </summary>
        public void LogTradeAccepted(string tradeId, string cardReceivedId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "trade_id", tradeId },
                { "card_received_id", cardReceivedId },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            LogCustomEvent("trade_accepted", parameters);
        }

        /// <summary>
        /// Registra el reclamo de una recompensa de misión (TDD 2.9).
        /// </summary>
        public void LogMissionClaimed(string missionId, int coinsRewarded)
        {
            var parameters = new Dictionary<string, object>
            {
                { "mission_id", missionId },
                { "coins_rewarded", coinsRewarded },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            LogCustomEvent("mission_claimed", parameters);
        }

        private void LogCustomEvent(string eventName, Dictionary<string, object> parameters)
        {
            string paramsSummary = "";
            foreach (var kvp in parameters)
            {
                paramsSummary += $"{kvp.Key}={kvp.Value}, ";
            }
            paramsSummary = paramsSummary.TrimEnd(' ', ',');

            Debug.Log($"<color=magenta>[Analytics:EVENT] '{eventName}' ➔ ({paramsSummary})</color>");
            OnEventLogged?.Invoke(eventName, parameters);
        }
    }
}

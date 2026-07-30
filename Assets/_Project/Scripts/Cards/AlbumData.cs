using System.Collections.Generic;
using UnityEngine;

namespace JuegoTCG.Cards
{
    public enum AlbumType
    {
        Liga,
        Torneo,
        Seleccion,
        Evento
    }

    [CreateAssetMenu(fileName = "NewAlbumData", menuName = "JuegoTCG/Album Data")]
    public class AlbumData : ScriptableObject
    {
        [Header("Información del Álbum")]
        public string albumId;
        public string albumName;
        public AlbumType albumType;
        public bool active = true;

        [Header("Cartas del Álbum")]
        public List<CardData> cards = new List<CardData>();

        [Header("Recompensas")]
        public int rewardCoins = 100;
    }
}

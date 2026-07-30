using UnityEngine;

namespace JuegoTCG.Packs
{
    public enum CostType
    {
        GratisTiempo,
        GratisAnuncio,
        Moneda
    }

    [CreateAssetMenu(fileName = "NewPackData", menuName = "JuegoTCG/Pack Data")]
    public class PackData : ScriptableObject
    {
        [Header("Información del Sobre")]
        public string packId;
        public string packName;
        public string albumId;
        public int cardsPerPack = 5;

        [Header("Costo")]
        public CostType costType;
        public int costAmount;

        [Header("Pesos de Rareza (RNG % Ponderado)")]
        [Range(0f, 100f)] public float comunWeight = 60f;
        [Range(0f, 100f)] public float pocoComunWeight = 25f;
        [Range(0f, 100f)] public float raraWeight = 10f;
        [Range(0f, 100f)] public float superRaraWeight = 4f;
        [Range(0f, 100f)] public float holoWeight = 1f;
    }
}

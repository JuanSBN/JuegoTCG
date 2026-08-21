using System.Collections.Generic;
using UnityEngine;
using JuegoTCG.Cards;

namespace JuegoTCG.Packs
{
    public static class WeightedRNG
    {
        // Probability weights based on GDD Section 5.2 (Total: 10,000)
        // Comun: 68.4% (6840)
        // Especial: 20.0% (2000)
        // Epica: 8.0% (800)
        // Legendaria: 3.0% (300)
        // Mitica: 0.5% (50)
        // FullArt: 0.1% (10)
        private static readonly Dictionary<Rarity, int> RarityWeights = new Dictionary<Rarity, int>
        {
            { Rarity.Comun, 6840 },
            { Rarity.Especial, 2000 },
            { Rarity.Epica, 800 },
            { Rarity.Legendaria, 300 },
            { Rarity.Mitica, 50 },
            { Rarity.FullArt, 10 }
        };

        public static Rarity GetRandomRarity()
        {
            int totalWeight = 0;
            foreach (var kvp in RarityWeights)
            {
                totalWeight += kvp.Value;
            }

            int roll = Random.Range(0, totalWeight);
            int current = 0;

            foreach (var kvp in RarityWeights)
            {
                current += kvp.Value;
                if (roll < current)
                {
                    return kvp.Key;
                }
            }

            return Rarity.Comun;
        }

        public static CardData SelectRandomCardByRarity(Rarity rarity, List<CardData> catalog)
        {
            if (catalog == null || catalog.Count == 0) return null;

            List<CardData> matchingCards = catalog.FindAll(c => c != null && c.rarity == rarity);
            if (matchingCards.Count > 0)
            {
                return matchingCards[Random.Range(0, matchingCards.Count)];
            }

            // Fallback to any card in catalog if exact rarity has no cards loaded
            return catalog[Random.Range(0, catalog.Count)];
        }
    }
}

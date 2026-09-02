export type CardRarity = "comun" | "poco_comun" | "rara" | "legendaria" | "mitica" | "full_art";

export interface CardCatalogEntry {
  cardId: string;
  name: string;
  initials: string;
  rarity: CardRarity;
  team: string;
  position: string;
  albumId: string;
  powerValue?: number;
}

export interface RarityWeights {
  comun: number;
  poco_comun: number;
  rara: number;
  legendaria: number;
  mitica: number;
  full_art?: number;
}

export const DEFAULT_RARITY_WEIGHTS: RarityWeights = {
  comun: 55,       // ~55% Jugador de plantilla (GDD 5.2)
  poco_comun: 25,  // ~25% Titular destacado (Especial / Poco común)
  rara: 12,        // ~12% Figura de equipo top (Épica / Rara)
  legendaria: 5,   // ~5%  Leyenda
  mitica: 2,       // ~2%  Leyenda destacada (Holográfica)
  full_art: 1,     // ~1%  Carta especial de evento (Holográfica)
};

/**
 * Puntos de poder por rareza para el cálculo de poder de colección (GDD Sección 7.2)
 */
export const RARITY_POWER_POINTS: Record<CardRarity, number> = {
  comun: 1,
  poco_comun: 2,
  rara: 4,
  legendaria: 8,
  mitica: 15,
  full_art: 25,
};

/**
 * Selects a rarity based on the weighted probability table.
 */
export function rollRarity(weights: RarityWeights = DEFAULT_RARITY_WEIGHTS): CardRarity {
  const table: { rarity: CardRarity; weight: number }[] = [
    { rarity: "full_art", weight: weights.full_art || 0 },
    { rarity: "mitica", weight: weights.mitica },
    { rarity: "legendaria", weight: weights.legendaria },
    { rarity: "rara", weight: weights.rara },
    { rarity: "poco_comun", weight: weights.poco_comun },
    { rarity: "comun", weight: weights.comun },
  ];

  const totalWeight = table.reduce((sum, item) => sum + item.weight, 0);
  const randomRoll = Math.random() * totalWeight;

  let currentWeight = 0;
  for (const item of table) {
    currentWeight += item.weight;
    if (randomRoll <= currentWeight) {
      return item.rarity;
    }
  }

  return "comun";
}

/**
 * Generates 5 cards for a pack opening based on the catalog and rarity weights.
 */
export function generatePackCards(
  catalog: CardCatalogEntry[],
  count: number = 5,
  weights: RarityWeights = DEFAULT_RARITY_WEIGHTS
): CardCatalogEntry[] {
  if (!catalog || catalog.length === 0) {
    throw new Error("El catálogo de cartas está vacío. No se pueden generar cartas.");
  }

  const selectedCards: CardCatalogEntry[] = [];

  for (let i = 0; i < count; i++) {
    const targetRarity = rollRarity(weights);
    
    // Filter cards by target rarity
    let pool = catalog.filter((c) => c.rarity === targetRarity);
    
    // Fallback if no cards exist for that specific rarity
    if (pool.length === 0) {
      pool = catalog;
    }

    const randomIndex = Math.floor(Math.random() * pool.length);
    selectedCards.push(pool[randomIndex]);
  }

  return selectedCards;
}

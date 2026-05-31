using System.Collections.Generic;
using System.Linq;

namespace LaneSurvivor.Heroes
{
    public static class HeroCatalog
    {
        public const string FirstWinHeroId = "mira_vanguard";

        public const string HqLevelTwoHeroId = "dax_medic";

        private static readonly HeroDefinition[] definitions =
        {
            new()
            {
                id = FirstWinHeroId,
                displayName = "Mira Vanguard",
                rarity = HeroRarity.Common,
                startingSquadBonus = 2,
                damageBonus = 0.15f
            },
            new()
            {
                id = HqLevelTwoHeroId,
                displayName = "Dax Medic",
                rarity = HeroRarity.Rare,
                startingSquadBonus = 1,
                damageBonus = 0.35f
            }
        };

        public static IReadOnlyList<HeroDefinition> All => definitions;

        public static HeroDefinition GetById(string heroId)
        {
            // Empty ids cannot match a real catalog entry.
            if (string.IsNullOrWhiteSpace(heroId))
            {
                return null;
            }

            // A tiny in-code catalog is enough for the first hero slice and can move to assets later.
            return definitions.FirstOrDefault(definition => definition.id == heroId);
        }
    }
}

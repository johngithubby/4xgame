using System;

namespace LaneSurvivor.Heroes
{
    [Serializable]
    public sealed class HeroDefinition
    {
        public string id;

        public string displayName;

        public HeroRarity rarity;

        public int startingSquadBonus;

        public float damageBonus;
    }
}

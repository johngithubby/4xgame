using LaneSurvivor.Save;

namespace LaneSurvivor.Heroes
{
    public static class HeroRewardSystem
    {
        public static HeroDefinition TryGrantFirstWinHero(SaveGameData data)
        {
            // The first hero is earned from gameplay only: the first completed minigame win reward.
            bool granted = HeroInventory.GrantHero(data, HeroCatalog.FirstWinHeroId);
            if (!granted)
            {
                return null;
            }

            // Auto-equip the first earned hero so the player sees its gameplay effect immediately.
            HeroInventory.EquipHero(data, HeroCatalog.FirstWinHeroId);
            return HeroCatalog.GetById(HeroCatalog.FirstWinHeroId);
        }

        public static HeroDefinition TryGrantHqLevelTwoHero(SaveGameData data)
        {
            // This milestone reward is deterministic: HQ level 2 earns the second local hero.
            if (data == null || data.hqLevel < 2)
            {
                return null;
            }

            // GrantHero prevents duplicate rewards if this check runs on multiple scene loads.
            bool granted = HeroInventory.GrantHero(data, HeroCatalog.HqLevelTwoHeroId);
            return granted ? HeroCatalog.GetById(HeroCatalog.HqLevelTwoHeroId) : null;
        }
    }
}

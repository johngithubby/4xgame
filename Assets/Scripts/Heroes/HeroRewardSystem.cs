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
    }
}

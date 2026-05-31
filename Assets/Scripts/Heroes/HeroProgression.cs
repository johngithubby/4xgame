using LaneSurvivor.Save;
using UnityEngine;

namespace LaneSurvivor.Heroes
{
    public static class HeroProgression
    {
        public const int MinigameWinXp = 20;

        public const int MaxHeroLevel = 10;

        private const int BaseXpPerLevel = 40;

        private const float DamageBonusPerLevelAboveOne = 0.05f;

        public static HeroXpRewardResult TryGrantMinigameWinXp(SaveGameData data)
        {
            // Minigame wins feed the equipped hero only; inventory-wide XP waits for a fuller hero screen.
            return AddXpToEquippedHero(data, MinigameWinXp);
        }

        public static HeroXpRewardResult AddXpToEquippedHero(SaveGameData data, int xpAmount)
        {
            // Null saves, missing equipment, and non-positive rewards should become harmless no-ops.
            if (data == null || xpAmount <= 0)
            {
                return HeroXpRewardResult.Empty();
            }

            // Normalize first so older saves receive progress rows before XP is applied.
            data.Normalize();

            // XP is awarded to the currently equipped hero, which must also be owned.
            HeroDefinition equippedHero = HeroInventory.GetEquippedHero(data);
            if (equippedHero == null)
            {
                return HeroXpRewardResult.Empty();
            }

            // The progress row is created by save normalization when a hero is owned.
            HeroProgressData progress = HeroInventory.GetProgressForOwnedHero(data, equippedHero.id);
            if (progress == null)
            {
                return HeroXpRewardResult.Empty();
            }

            // Max-level heroes do not keep banking XP in this prototype.
            if (progress.level >= MaxHeroLevel)
            {
                progress.level = MaxHeroLevel;
                progress.xp = 0;
                return HeroXpRewardResult.Empty();
            }

            // Add XP before resolving level-ups so the result can report the original reward amount.
            progress.xp += xpAmount;
            int levelsGained = 0;

            // Resolve repeated level-ups in case a future reward grants more than one threshold.
            while (progress.level < MaxHeroLevel && progress.xp >= GetXpRequiredForNextLevel(progress.level))
            {
                progress.xp -= GetXpRequiredForNextLevel(progress.level);
                progress.level += 1;
                levelsGained += 1;
            }

            // Once max level is reached, clear leftover XP because there is no next threshold yet.
            if (progress.level >= MaxHeroLevel)
            {
                progress.level = MaxHeroLevel;
                progress.xp = 0;
            }

            return new HeroXpRewardResult(equippedHero, xpAmount, levelsGained, progress.level, progress.xp);
        }

        public static int GetXpRequiredForNextLevel(int currentLevel)
        {
            // The first threshold is intentionally low so the prototype level-up is easy to see.
            int safeLevel = Mathf.Max(1, currentLevel);
            return safeLevel >= MaxHeroLevel ? 0 : BaseXpPerLevel * safeLevel;
        }

        public static float GetDamageBonusForLevel(int heroLevel)
        {
            // Level one is the catalog baseline; each extra level adds a tiny visible damage bump.
            int clampedLevel = Mathf.Clamp(heroLevel, 1, MaxHeroLevel);
            return (clampedLevel - 1) * DamageBonusPerLevelAboveOne;
        }
    }

    public sealed class HeroXpRewardResult
    {
        public readonly HeroDefinition hero;

        public readonly int xpAdded;

        public readonly int levelsGained;

        public readonly int level;

        public readonly int xp;

        public bool HasReward => hero != null && xpAdded > 0;

        public HeroXpRewardResult(HeroDefinition hero, int xpAdded, int levelsGained, int level, int xp)
        {
            // Store the UI-facing result as immutable fields so callers cannot desync it from save data.
            this.hero = hero;
            this.xpAdded = xpAdded;
            this.levelsGained = levelsGained;
            this.level = level;
            this.xp = xp;
        }

        public static HeroXpRewardResult Empty()
        {
            // A shared shape for no-op XP attempts keeps reward text code simple.
            return new HeroXpRewardResult(null, 0, 0, 0, 0);
        }
    }
}

using System;
using System.IO;
using LaneSurvivor.Heroes;
using LaneSurvivor.Save;
using NUnit.Framework;

namespace LaneSurvivor.Tests.EditMode
{
    public sealed class PhaseThreeHeroTests
    {
        private string tempSavePath;

        [TearDown]
        public void TearDown()
        {
            // Reset the save manager so test paths never leak into other test fixtures.
            SaveGameManager.ClearCustomSavePathForTests();

            // Remove temp save files created by persistence tests.
            if (!string.IsNullOrEmpty(tempSavePath) && File.Exists(tempSavePath))
            {
                File.Delete(tempSavePath);
            }

            // Remove any temp write file left behind if a persistence assertion fails midway.
            if (!string.IsNullOrEmpty(tempSavePath) && File.Exists($"{tempSavePath}.tmp"))
            {
                File.Delete($"{tempSavePath}.tmp");
            }
        }

        [Test]
        public void FirstWinHeroReward_GrantsAndEquipsHero()
        {
            SaveGameData saveData = new();

            HeroDefinition grantedHero = HeroRewardSystem.TryGrantFirstWinHero(saveData);

            Assert.IsNotNull(grantedHero);
            Assert.AreEqual(HeroCatalog.FirstWinHeroId, grantedHero.id);
            Assert.IsTrue(HeroInventory.OwnsHero(saveData, HeroCatalog.FirstWinHeroId));
            Assert.AreEqual(HeroCatalog.FirstWinHeroId, saveData.equippedHeroId);
            Assert.AreEqual(1, HeroInventory.GetHeroLevel(saveData, HeroCatalog.FirstWinHeroId));
            Assert.AreEqual(0, HeroInventory.GetHeroXp(saveData, HeroCatalog.FirstWinHeroId));
        }

        [Test]
        public void FirstWinHeroReward_DoesNotDuplicateOwnedHero()
        {
            SaveGameData saveData = new();

            HeroRewardSystem.TryGrantFirstWinHero(saveData);
            HeroDefinition secondGrant = HeroRewardSystem.TryGrantFirstWinHero(saveData);

            Assert.IsNull(secondGrant);
            Assert.AreEqual(1, saveData.ownedHeroIds.Count);
            Assert.AreEqual(HeroCatalog.FirstWinHeroId, saveData.equippedHeroId);
        }

        [Test]
        public void EquippedHero_AddsVisibleMinigameStats()
        {
            SaveGameData saveData = new();

            HeroRewardSystem.TryGrantFirstWinHero(saveData);

            Assert.AreEqual(2, HeroInventory.GetStartingSquadBonus(saveData));
            Assert.AreEqual(0.15f, HeroInventory.GetDamageBonus(saveData));
        }

        [Test]
        public void HeroProgression_WinXpAddsToEquippedHero()
        {
            SaveGameData saveData = new();

            HeroRewardSystem.TryGrantFirstWinHero(saveData);
            HeroXpRewardResult xpReward = HeroProgression.TryGrantMinigameWinXp(saveData);

            Assert.IsTrue(xpReward.HasReward);
            Assert.AreEqual(HeroCatalog.FirstWinHeroId, xpReward.hero.id);
            Assert.AreEqual(HeroProgression.MinigameWinXp, HeroInventory.GetHeroXp(saveData, HeroCatalog.FirstWinHeroId));
            Assert.AreEqual(1, HeroInventory.GetHeroLevel(saveData, HeroCatalog.FirstWinHeroId));
        }

        [Test]
        public void HeroProgression_LevelsUpAtThreshold()
        {
            SaveGameData saveData = new();

            HeroRewardSystem.TryGrantFirstWinHero(saveData);
            HeroProgression.AddXpToEquippedHero(saveData, 40);

            Assert.AreEqual(2, HeroInventory.GetHeroLevel(saveData, HeroCatalog.FirstWinHeroId));
            Assert.AreEqual(0, HeroInventory.GetHeroXp(saveData, HeroCatalog.FirstWinHeroId));
        }

        [Test]
        public void HeroProgression_IgnoresMissingEquippedHero()
        {
            SaveGameData saveData = new();

            HeroXpRewardResult xpReward = HeroProgression.TryGrantMinigameWinXp(saveData);

            Assert.IsFalse(xpReward.HasReward);
            Assert.IsEmpty(saveData.ownedHeroIds);
            Assert.IsEmpty(saveData.heroProgress);
        }

        [Test]
        public void HeroProgression_LevelDamageBonusIsApplied()
        {
            SaveGameData saveData = new();

            HeroRewardSystem.TryGrantFirstWinHero(saveData);
            HeroProgression.AddXpToEquippedHero(saveData, 40);

            Assert.AreEqual(0.20f, HeroInventory.GetDamageBonus(saveData));
        }

        [Test]
        public void HeroProgression_ManualLevelUpSpendsCoinsAndLevelsEquippedHero()
        {
            SaveGameData saveData = new()
            {
                coins = HeroProgression.GetManualLevelUpCoinCost(1)
            };

            HeroRewardSystem.TryGrantFirstWinHero(saveData);
            HeroLevelUpResult result = HeroProgression.TryLevelUpEquippedHeroWithCoins(saveData);

            Assert.IsTrue(result.success);
            Assert.AreEqual(0, saveData.coins);
            Assert.AreEqual(2, HeroInventory.GetHeroLevel(saveData, HeroCatalog.FirstWinHeroId));
            Assert.AreEqual(0, HeroInventory.GetHeroXp(saveData, HeroCatalog.FirstWinHeroId));
        }

        [Test]
        public void HeroProgression_ManualLevelUpFailsWhenCoinsAreInsufficient()
        {
            SaveGameData saveData = new()
            {
                coins = HeroProgression.GetManualLevelUpCoinCost(1) - 1
            };

            HeroRewardSystem.TryGrantFirstWinHero(saveData);
            HeroLevelUpResult result = HeroProgression.TryLevelUpEquippedHeroWithCoins(saveData);

            Assert.IsFalse(result.success);
            Assert.AreEqual(HeroProgression.GetManualLevelUpCoinCost(1) - 1, saveData.coins);
            Assert.AreEqual(1, HeroInventory.GetHeroLevel(saveData, HeroCatalog.FirstWinHeroId));
        }

        [Test]
        public void Normalize_ClampsHeroLevelToProgressionMaximum()
        {
            SaveGameData saveData = new();

            HeroRewardSystem.TryGrantFirstWinHero(saveData);
            saveData.heroProgress[0].level = 999;
            saveData.Normalize();

            Assert.AreEqual(HeroProgression.MaxHeroLevel, HeroInventory.GetHeroLevel(saveData, HeroCatalog.FirstWinHeroId));
            Assert.AreEqual(0.60f, HeroInventory.GetDamageBonus(saveData));
        }

        [Test]
        public void ManualEquip_EquipsOwnedHero()
        {
            SaveGameData saveData = new();

            // Grant ownership without using the reward system so the manual equip path is isolated.
            HeroInventory.GrantHero(saveData, HeroCatalog.FirstWinHeroId);

            // Clear the equipped id to simulate an owned hero waiting in the Base hero panel.
            saveData.equippedHeroId = string.Empty;

            bool equipped = HeroInventory.EquipHero(saveData, HeroCatalog.FirstWinHeroId);

            Assert.IsTrue(equipped);
            Assert.AreEqual(HeroCatalog.FirstWinHeroId, saveData.equippedHeroId);
        }

        [Test]
        public void ManualEquip_RejectsUnownedHero()
        {
            SaveGameData saveData = new();

            bool equipped = HeroInventory.EquipHero(saveData, HeroCatalog.FirstWinHeroId);

            Assert.IsFalse(equipped);
            Assert.IsEmpty(saveData.equippedHeroId);
        }

        [Test]
        public void HqLevelTwoHeroReward_GrantsSecondHero()
        {
            SaveGameData saveData = new()
            {
                hqLevel = 2
            };

            HeroDefinition grantedHero = HeroRewardSystem.TryGrantHqLevelTwoHero(saveData);

            Assert.IsNotNull(grantedHero);
            Assert.AreEqual(HeroCatalog.HqLevelTwoHeroId, grantedHero.id);
            Assert.IsTrue(HeroInventory.OwnsHero(saveData, HeroCatalog.HqLevelTwoHeroId));
        }

        [Test]
        public void HqLevelTwoHeroReward_DoesNotGrantBeforeHqLevelTwo()
        {
            SaveGameData saveData = new()
            {
                hqLevel = 1
            };

            HeroDefinition grantedHero = HeroRewardSystem.TryGrantHqLevelTwoHero(saveData);

            Assert.IsNull(grantedHero);
            Assert.IsFalse(HeroInventory.OwnsHero(saveData, HeroCatalog.HqLevelTwoHeroId));
        }

        [Test]
        public void NextOwnedHeroToEquip_CyclesBetweenOwnedHeroes()
        {
            SaveGameData saveData = new();

            HeroInventory.GrantHero(saveData, HeroCatalog.FirstWinHeroId);
            HeroInventory.GrantHero(saveData, HeroCatalog.HqLevelTwoHeroId);
            HeroInventory.EquipHero(saveData, HeroCatalog.FirstWinHeroId);

            HeroDefinition nextHero = HeroInventory.GetNextOwnedHeroToEquip(saveData);

            Assert.IsNotNull(nextHero);
            Assert.AreEqual(HeroCatalog.HqLevelTwoHeroId, nextHero.id);
        }

        [Test]
        public void SecondHeroStats_AppliesWhenEquipped()
        {
            SaveGameData saveData = new();

            HeroInventory.GrantHero(saveData, HeroCatalog.HqLevelTwoHeroId);
            HeroInventory.EquipHero(saveData, HeroCatalog.HqLevelTwoHeroId);

            Assert.AreEqual(1, HeroInventory.GetStartingSquadBonus(saveData));
            Assert.AreEqual(0.35f, HeroInventory.GetDamageBonus(saveData));
            Assert.AreEqual(1, HeroInventory.GetHeroLevel(saveData, HeroCatalog.HqLevelTwoHeroId));
        }

        [Test]
        public void SaveGameManager_PreservesHeroInventory()
        {
            // Use an isolated path so hero persistence tests never touch the real prototype save.
            tempSavePath = Path.Combine(Path.GetTempPath(), $"lane-survivor-hero-save-{Guid.NewGuid():N}.json");
            SaveGameManager.UseCustomSavePathForTests(tempSavePath);
            SaveGameData saveData = new();

            // Grant and save the first hero through the same path gameplay uses.
            HeroRewardSystem.TryGrantFirstWinHero(saveData);
            HeroProgression.AddXpToEquippedHero(saveData, 40);
            SaveGameManager.Save(saveData);

            // Reload from disk so this proves JSON persistence keeps ownership and equipment.
            SaveGameData loadedData = SaveGameManager.Load();

            Assert.IsTrue(HeroInventory.OwnsHero(loadedData, HeroCatalog.FirstWinHeroId));
            Assert.AreEqual(HeroCatalog.FirstWinHeroId, loadedData.equippedHeroId);
            Assert.AreEqual(2, HeroInventory.GetStartingSquadBonus(loadedData));
            Assert.AreEqual(2, HeroInventory.GetHeroLevel(loadedData, HeroCatalog.FirstWinHeroId));
            Assert.AreEqual(0, HeroInventory.GetHeroXp(loadedData, HeroCatalog.FirstWinHeroId));
        }

        [Test]
        public void Normalize_ClearsUnownedEquippedHero()
        {
            SaveGameData saveData = new()
            {
                equippedHeroId = HeroCatalog.FirstWinHeroId
            };

            saveData.Normalize();

            Assert.IsEmpty(saveData.equippedHeroId);
            Assert.IsEmpty(saveData.ownedHeroIds);
            Assert.IsEmpty(saveData.heroProgress);
        }
    }
}

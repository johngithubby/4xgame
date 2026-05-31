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
        public void SaveGameManager_PreservesHeroInventory()
        {
            // Use an isolated path so hero persistence tests never touch the real prototype save.
            tempSavePath = Path.Combine(Path.GetTempPath(), $"lane-survivor-hero-save-{Guid.NewGuid():N}.json");
            SaveGameManager.UseCustomSavePathForTests(tempSavePath);
            SaveGameData saveData = new();

            // Grant and save the first hero through the same path gameplay uses.
            HeroRewardSystem.TryGrantFirstWinHero(saveData);
            SaveGameManager.Save(saveData);

            // Reload from disk so this proves JSON persistence keeps ownership and equipment.
            SaveGameData loadedData = SaveGameManager.Load();

            Assert.IsTrue(HeroInventory.OwnsHero(loadedData, HeroCatalog.FirstWinHeroId));
            Assert.AreEqual(HeroCatalog.FirstWinHeroId, loadedData.equippedHeroId);
            Assert.AreEqual(2, HeroInventory.GetStartingSquadBonus(loadedData));
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
        }
    }
}

using System;
using System.Collections.Generic;
using LaneSurvivor.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LaneSurvivor.Heroes
{
    public sealed class HeroHudController : MonoBehaviour
    {
        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text coinsText;

        [SerializeField]
        private Text equippedHeroText;

        [SerializeField]
        private Text heroListText;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private Button equipNextButton;

        [SerializeField]
        private Button levelUpButton;

        [SerializeField]
        private Button backButton;

        public void Configure(Text title, Text coins, Text equippedHero, Text heroList, Text status, Button equipNext, Button levelUp, Button back)
        {
            titleText = title;
            coinsText = coins;
            equippedHeroText = equippedHero;
            heroListText = heroList;
            statusText = status;
            equipNextButton = equipNext;
            levelUpButton = levelUp;
            backButton = back;
        }

        public void Initialize(Action equipNextAction, Action levelUpAction, Action backAction)
        {
            // Clear listeners first so runtime scene rebuilds cannot duplicate button callbacks.
            equipNextButton.onClick.RemoveAllListeners();
            levelUpButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();

            // Keep the controller passive; the bootstrap owns all save mutations.
            equipNextButton.onClick.AddListener(() => equipNextAction?.Invoke());
            levelUpButton.onClick.AddListener(() => levelUpAction?.Invoke());
            backButton.onClick.AddListener(() => backAction?.Invoke());
        }

        public void UpdateView(SaveGameData saveData, string statusOverride)
        {
            // Normalize once so level, XP, and owned hero lists are safe to display.
            saveData?.Normalize();

            // Read the minimal screen state from local save data.
            int coins = Mathf.Max(0, saveData?.coins ?? 0);
            HeroDefinition equippedHero = HeroInventory.GetEquippedHero(saveData);
            IReadOnlyList<HeroDefinition> ownedHeroes = HeroInventory.GetOwnedHeroes(saveData);
            int equippedLevel = equippedHero != null ? HeroInventory.GetHeroLevel(saveData, equippedHero.id) : 0;
            int levelUpCost = equippedHero != null ? HeroProgression.GetManualLevelUpCoinCost(equippedLevel) : 0;
            bool heroCanGainLevels = equippedHero != null && equippedLevel < HeroProgression.MaxHeroLevel;
            bool canLevelUp = heroCanGainLevels && coins >= levelUpCost;

            // Header text mirrors the dedicated screen role, not the Base scene compact panel.
            titleText.text = "Heroes";
            coinsText.text = $"Coins: {coins}";
            equippedHeroText.text = BuildEquippedHeroText(equippedHero, equippedLevel, levelUpCost, heroCanGainLevels);
            heroListText.text = BuildHeroList(saveData, ownedHeroes, equippedHero);
            statusText.text = string.IsNullOrWhiteSpace(statusOverride)
                ? "Earn heroes through gameplay"
                : statusOverride;

            // Equipment cycles when there is a choice, and level-up requires an affordable equipped hero.
            equipNextButton.interactable = ownedHeroes.Count > 0;
            levelUpButton.interactable = canLevelUp;
        }

        private static string BuildHeroList(SaveGameData saveData, IReadOnlyList<HeroDefinition> ownedHeroes, HeroDefinition equippedHero)
        {
            // Empty state is useful before the first minigame win.
            if (ownedHeroes == null || ownedHeroes.Count == 0)
            {
                return "No heroes owned yet";
            }

            // Each row shows ownership, equipment, level, XP, and current visible bonuses.
            List<string> rows = new();
            foreach (HeroDefinition hero in ownedHeroes)
            {
                int level = HeroInventory.GetHeroLevel(saveData, hero.id);
                int xp = HeroInventory.GetHeroXp(saveData, hero.id);
                int xpToNext = HeroProgression.GetXpRequiredForNextLevel(level);
                string equippedLabel = equippedHero != null && equippedHero.id == hero.id ? " *" : string.Empty;
                string xpLabel = xpToNext > 0 ? $"{xp}/{xpToNext} XP" : "MAX";
                float damageBonus = Mathf.Max(0f, hero.damageBonus + HeroProgression.GetDamageBonusForLevel(level));
                rows.Add($"{hero.displayName}{equippedLabel}\nLv {level} {hero.rarity}  Squad +{hero.startingSquadBonus}  Damage +{damageBonus:0.##}  {xpLabel}");
            }

            return string.Join("\n\n", rows);
        }

        private static string BuildEquippedHeroText(HeroDefinition equippedHero, int equippedLevel, int levelUpCost, bool heroCanGainLevels)
        {
            // Empty equipment should stay short so it fits in the same two-line header area.
            if (equippedHero == null)
            {
                return "Equipped: None";
            }

            // Split the equipped summary from the level-up cost so long hero names cannot collide with the list.
            string levelCostText = heroCanGainLevels ? $"Level cost: {levelUpCost} coins" : "Max level";
            return $"Equipped: {equippedHero.displayName} Lv {equippedLevel}\n{levelCostText}";
        }
    }
}

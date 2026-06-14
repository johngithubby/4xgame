using System;
using System.Collections.Generic;
using LaneSurvivor.Heroes;
using LaneSurvivor.Progression;
using LaneSurvivor.Retention;
using LaneSurvivor.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LaneSurvivor.Base
{
    public sealed class BaseHudController : MonoBehaviour
    {
        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Button creditsButton;

        [SerializeField]
        private Text creditsButtonText;

        [SerializeField]
        private GameObject creditsDetailPanel;

        [SerializeField]
        private Text creditsDetailText;

        [SerializeField]
        private bool creditsExpanded;

        [SerializeField]
        private Text heroPanelTitleText;

        [SerializeField]
        private Text heroPanelText;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private Text playHintText;

        [SerializeField]
        private Button collectButton;

        [SerializeField]
        private Button upgradeButton;

        [SerializeField]
        private Button playButton;

        [SerializeField]
        private Button claimObjectiveButton;

        [SerializeField]
        private Button[] missionButtons = Array.Empty<Button>();

        [SerializeField]
        private Button resetButton;

        [SerializeField]
        private Button equipHeroButton;

        [SerializeField]
        private Button heroesButton;

        [SerializeField]
        private Button zoomInButton;

        [SerializeField]
        private Button zoomOutButton;

        public void Configure(
            Text title,
            Button credits,
            Text creditsLabel,
            GameObject creditsDetails,
            Text creditsDetailsText,
            Text heroPanelTitle,
            Text heroPanel,
            Text status,
            Text playHint,
            Button collect,
            Button upgrade,
            Button play,
            Button claimObjective,
            Button[] missionSelectionButtons,
            Button reset,
            Button equipHero,
            Button heroes,
            Button zoomIn,
            Button zoomOut)
        {
            titleText = title;
            creditsButton = credits;
            creditsButtonText = creditsLabel;
            creditsDetailPanel = creditsDetails;
            creditsDetailText = creditsDetailsText;
            heroPanelTitleText = heroPanelTitle;
            heroPanelText = heroPanel;
            statusText = status;
            playHintText = playHint;
            collectButton = collect;
            upgradeButton = upgrade;
            playButton = play;
            claimObjectiveButton = claimObjective;
            missionButtons = missionSelectionButtons ?? Array.Empty<Button>();
            resetButton = reset;
            equipHeroButton = equipHero;
            heroesButton = heroes;
            zoomInButton = zoomIn;
            zoomOutButton = zoomOut;
        }

        public void Initialize(Action collectAction, Action upgradeAction, Action playAction, Action claimObjectiveAction, Action<int> selectMissionAction, Action resetAction, Action equipHeroAction, Action heroesAction, Action zoomInAction, Action zoomOutAction, Action creditsToggleAction = null)
        {
            // Replace listeners so scene rebuilds or test setup cannot accidentally duplicate clicks.
            creditsButton.onClick.RemoveAllListeners();
            collectButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.RemoveAllListeners();
            playButton.onClick.RemoveAllListeners();
            claimObjectiveButton.onClick.RemoveAllListeners();
            foreach (Button missionButton in missionButtons)
            {
                // Null guards keep generated HUD tests readable if a future layout omits a mission button.
                missionButton?.onClick.RemoveAllListeners();
            }
            resetButton.onClick.RemoveAllListeners();
            equipHeroButton.onClick.RemoveAllListeners();
            heroesButton.onClick.RemoveAllListeners();
            zoomInButton.onClick.RemoveAllListeners();
            zoomOutButton.onClick.RemoveAllListeners();

            // Credits is a HUD action too, so it gets the same outside-click hook before expanding details.
            creditsButton.onClick.AddListener(() =>
            {
                creditsToggleAction?.Invoke();
                ToggleCreditsPanel();
            });
            collectButton.onClick.AddListener(() => collectAction?.Invoke());
            upgradeButton.onClick.AddListener(() => upgradeAction?.Invoke());
            playButton.onClick.AddListener(() => playAction?.Invoke());
            claimObjectiveButton.onClick.AddListener(() => claimObjectiveAction?.Invoke());
            for (int index = 0; index < missionButtons.Length; index += 1)
            {
                // Capture the authored mission level so every generated button selects its own row.
                int missionLevel = index + 1;
                missionButtons[index]?.onClick.AddListener(() => selectMissionAction?.Invoke(missionLevel));
            }
            resetButton.onClick.AddListener(() => resetAction?.Invoke());
            equipHeroButton.onClick.AddListener(() => equipHeroAction?.Invoke());
            heroesButton.onClick.AddListener(() => heroesAction?.Invoke());
            zoomInButton.onClick.AddListener(() => zoomInAction?.Invoke());
            zoomOutButton.onClick.AddListener(() => zoomOutAction?.Invoke());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Keep the reset affordance available in editor and development builds for fast iteration.
            resetButton.gameObject.SetActive(true);
#else
            // Hide reset from release builds so it remains a debug-only helper.
            resetButton.gameObject.SetActive(false);
#endif
        }

        public void UpdateView(SaveGameData saveData, int remainingSeconds, int bioLabRemainingSeconds, int hangarRemainingSeconds, int trainingRemainingSeconds, string statusOverride)
        {
            // Normalize the value read by UI so null data still shows a sensible first-launch state.
            int hqLevel = Mathf.Max(1, saveData?.hqLevel ?? 1);
            int bioLabLevel = Mathf.Max(1, saveData?.bioLabLevel ?? 1);
            int hangarLevel = Mathf.Max(1, saveData?.hangarLevel ?? 1);
            int trainingFacilityLevel = Mathf.Max(1, saveData?.trainingFacilityLevel ?? 1);
            int coins = Mathf.Max(0, saveData?.coins ?? 0);
            int upgradeCost = PlayerProgression.GetHqUpgradeCost(hqLevel);
            bool upgradeRunning = saveData != null && saveData.hqUpgradeInProgress;
            bool bioLabUpgradeRunning = saveData != null && saveData.bioLabUpgradeInProgress;
            bool hangarUpgradeRunning = saveData != null && saveData.hangarUpgradeInProgress;
            bool trainingUpgradeRunning = saveData != null && saveData.trainingFacilityUpgradeInProgress;
            bool canAffordUpgrade = coins >= upgradeCost;
            int selectedMissionLevel = PlayerProgression.GetSelectedMissionLevel(saveData);
            HeroDefinition equippedHero = HeroInventory.GetEquippedHero(saveData);
            IReadOnlyList<HeroDefinition> ownedHeroes = HeroInventory.GetOwnedHeroes(saveData);
            bool firstHeroOwned = HeroInventory.OwnsHero(saveData, HeroCatalog.FirstWinHeroId);
            DailyObjectiveStatus objectiveStatus = DailyObjectiveProgression.GetStatus(saveData, DateTime.UtcNow);

            // Build the default status separately so action feedback can override it cleanly.
            string fallbackStatus = upgradeRunning
                ? "HQ upgrade in progress"
                : bioLabUpgradeRunning
                    ? "Bio lab upgrade in progress"
                    : hangarUpgradeRunning
                        ? "Hangar upgrade in progress"
                        : trainingUpgradeRunning
                            ? "Training upgrade in progress"
                            : $"Next HQ upgrade: {upgradeCost} coins";

            titleText.text = "Base";
            string upgradeStatus = upgradeRunning ? $"HQ Upgrade: {remainingSeconds}s" : "HQ Upgrade: Ready";
            string bioLabStatus = bioLabUpgradeRunning ? $"Bio Upgrade: {bioLabRemainingSeconds}s" : "Bio Upgrade: Ready";
            string hangarStatus = hangarUpgradeRunning ? $"Hangar Upgrade: {hangarRemainingSeconds}s" : "Hangar Upgrade: Ready";
            string trainingStatus = trainingUpgradeRunning ? $"Training Upgrade: {trainingRemainingSeconds}s" : "Training Upgrade: Ready";
            creditsButtonText.text = $"Credits: {coins}";
            creditsDetailText.text = BuildCreditsDetailText(coins, hqLevel, bioLabLevel, hangarLevel, trainingFacilityLevel, upgradeStatus, bioLabStatus, hangarStatus, trainingStatus);
            creditsDetailPanel.SetActive(creditsExpanded);
            heroPanelTitleText.text = "Owned Heroes";
            heroPanelText.text = BuildHeroPanelText(saveData, ownedHeroes, equippedHero);
            statusText.text = !string.IsNullOrWhiteSpace(statusOverride)
                ? statusOverride
                : fallbackStatus;

            // Before the first hero is owned, the play hint tells the player a hero can be earned.
            string missionUnlockHint = PlayerProgression.WouldUnlockNextMission(saveData) ? " + mission" : string.Empty;
            playHintText.text = firstHeroOwned
                ? $"Win reward: +{PlayerProgression.MinigameWinCoins} coins{missionUnlockHint}"
                : $"Win reward: +{PlayerProgression.MinigameWinCoins} coins + hero{missionUnlockHint}";

            // The collect button remains available in this prototype so the loop can be tested quickly.
            collectButton.interactable = true;

            // Prevent starting a second timer or spending coins that are not available.
            upgradeButton.interactable = !upgradeRunning && canAffordUpgrade;
            playButton.interactable = true;
            claimObjectiveButton.interactable = objectiveStatus.canClaimReward;
            for (int index = 0; index < missionButtons.Length; index += 1)
            {
                // Generated mission buttons map one-to-one to authored local mission ids.
                ConfigureMissionButton(missionButtons[index], saveData, index + 1, selectedMissionLevel);
            }
            equipHeroButton.interactable = HasUnequippedOwnedHero(ownedHeroes, equippedHero);
            heroesButton.interactable = true;
            zoomInButton.interactable = true;
            zoomOutButton.interactable = true;
        }

        private void ToggleCreditsPanel()
        {
            // Store the expanded state locally because the bootstrap may refresh HUD text every frame during upgrades.
            creditsExpanded = !creditsExpanded;

            // Apply the visibility immediately so a tap responds even when no save-backed value changed.
            creditsDetailPanel.SetActive(creditsExpanded);
        }

        private static string BuildCreditsDetailText(int coins, int hqLevel, int bioLabLevel, int hangarLevel, int trainingFacilityLevel, string upgradeStatus, string bioLabStatus, string hangarStatus, string trainingStatus)
        {
            // Keep the expanded panel short so it replaces the old left text stack without becoming another wall.
            return $"Coins: {coins}\nHQ Level: {hqLevel}\nBio Lab: {bioLabLevel}\nHangar: {hangarLevel}\nTraining: {trainingFacilityLevel}\n{upgradeStatus}\n{bioLabStatus}\n{hangarStatus}\n{trainingStatus}";
        }

        private static void ConfigureMissionButton(Button missionButton, SaveGameData saveData, int missionLevel, int selectedMissionLevel)
        {
            // Defensive null handling keeps tests focused on layout generation instead of crashing in update.
            if (missionButton == null)
            {
                return;
            }

            // Locked mission buttons are visible as goals but cannot be tapped in normal UI interaction.
            bool isMissionUnlocked = PlayerProgression.IsMissionUnlocked(saveData, missionLevel);
            missionButton.interactable = isMissionUnlocked;

            // Prefixing the selected button gives touch users a quick target check above the text panel.
            Text label = missionButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = missionLevel == selectedMissionLevel ? $">M{missionLevel}" : $"M{missionLevel}";
            }
        }

        private static string BuildHeroPanelText(SaveGameData saveData, IReadOnlyList<HeroDefinition> ownedHeroes, HeroDefinition equippedHero)
        {
            // Empty ownership should be explicit because the panel exists before the first win reward.
            if (ownedHeroes == null || ownedHeroes.Count == 0)
            {
                return "None earned yet";
            }

            // Keep the panel compact enough for the placeholder mobile HUD.
            List<string> heroLines = new();
            foreach (HeroDefinition hero in ownedHeroes)
            {
                // Each row shows the current local progression state for that owned hero.
                int heroLevel = HeroInventory.GetHeroLevel(saveData, hero.id);
                int heroXp = HeroInventory.GetHeroXp(saveData, hero.id);
                int xpToNextLevel = HeroProgression.GetXpRequiredForNextLevel(heroLevel);
                string equippedLabel = equippedHero != null && equippedHero.id == hero.id ? " EQ" : string.Empty;
                string xpLabel = xpToNextLevel > 0 ? $"{heroXp}/{xpToNextLevel}XP" : "MAX";
                heroLines.Add($"{hero.displayName} Lv{heroLevel} [{hero.rarity}]{equippedLabel} {xpLabel}");
            }

            return string.Join("\n", heroLines);
        }

        private static bool HasUnequippedOwnedHero(IReadOnlyList<HeroDefinition> ownedHeroes, HeroDefinition equippedHero)
        {
            // Without owned heroes there is nothing for the Base panel button to equip.
            if (ownedHeroes == null || ownedHeroes.Count == 0)
            {
                return false;
            }

            // One owned but unequipped hero can be equipped; multiple heroes can cycle selection.
            return equippedHero == null || ownedHeroes.Count > 1;
        }
    }
}

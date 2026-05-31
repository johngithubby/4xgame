using System;
using UnityEngine;
using UnityEngine.UI;

namespace LaneSurvivor.UI
{
    public sealed class EndScreenController : MonoBehaviour
    {
        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private Text resultText;

        [SerializeField]
        private Text rewardText;

        [SerializeField]
        private Button restartButton;

        [SerializeField]
        private Button baseButton;

        public void Configure(GameObject resultPanel, Text resultLabel, Button restartLevelButton, Button returnToBaseButton = null, Text rewardLabel = null)
        {
            panel = resultPanel;
            resultText = resultLabel;
            restartButton = restartLevelButton;
            baseButton = returnToBaseButton;
            rewardText = rewardLabel;
        }

        public void Initialize(Action restartAction, Action baseAction = null)
        {
            // Replace listeners so rebuilt runtime UI cannot accumulate duplicate callbacks.
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => restartAction?.Invoke());

            // The Base button is optional so older tests and minimal scenes can keep using restart only.
            if (baseButton != null)
            {
                baseButton.onClick.RemoveAllListeners();
                baseButton.onClick.AddListener(() => baseAction?.Invoke());
                baseButton.gameObject.SetActive(baseAction != null);
            }

            Hide();
        }

        public void Show(string result, string reward = "")
        {
            // The result line is always visible for both win and loss outcomes.
            resultText.text = result;

            // The reward line is optional so losses and older scenes can show only the result.
            if (rewardText != null)
            {
                rewardText.text = reward;
                rewardText.gameObject.SetActive(!string.IsNullOrWhiteSpace(reward));
            }

            panel.SetActive(true);
        }

        public void Hide()
        {
            // Hide the whole panel between runs and while the minigame is active.
            panel.SetActive(false);
        }
    }
}

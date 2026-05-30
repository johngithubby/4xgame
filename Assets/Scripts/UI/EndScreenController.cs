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
        private Button restartButton;

        [SerializeField]
        private Button baseButton;

        public void Configure(GameObject resultPanel, Text resultLabel, Button restartLevelButton, Button returnToBaseButton = null)
        {
            panel = resultPanel;
            resultText = resultLabel;
            restartButton = restartLevelButton;
            baseButton = returnToBaseButton;
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

        public void Show(string result)
        {
            resultText.text = result;
            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);
        }
    }
}

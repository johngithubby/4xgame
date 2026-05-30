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

        public void Configure(GameObject resultPanel, Text resultLabel, Button restartLevelButton)
        {
            panel = resultPanel;
            resultText = resultLabel;
            restartButton = restartLevelButton;
        }

        public void Initialize(Action restartAction)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => restartAction?.Invoke());
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

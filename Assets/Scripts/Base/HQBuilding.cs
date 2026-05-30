using LaneSurvivor.Save;
using UnityEngine;

namespace LaneSurvivor.Base
{
    public sealed class HQBuilding : MonoBehaviour
    {
        [SerializeField]
        private TextMesh levelLabel;

        public int Level { get; private set; } = 1;

        public void Configure(TextMesh label)
        {
            // Keep the label reference optional so tests or placeholder scenes can use the component without UI.
            levelLabel = label;
        }

        public void ApplySaveData(SaveGameData saveData)
        {
            // Mirror the saved HQ level onto the scene component for display and future scene interactions.
            Level = Mathf.Max(1, saveData?.hqLevel ?? 1);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            // The world label makes the placeholder cube readable without needing imported art.
            if (levelLabel != null)
            {
                levelLabel.text = $"HQ\nLv {Level}";
            }
        }
    }
}

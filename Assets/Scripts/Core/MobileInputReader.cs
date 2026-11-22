using UnityEngine;

namespace FrogRunner.Core
{
    /// <summary>
    /// Feeds the on-screen touch pads into MobileInput once per frame. Only active during play,
    /// so menu clicks never leak into the runner.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class MobileInputReader : MonoBehaviour
    {
        void Update()
        {
            bool active = GameManager.Instance != null
                && GameManager.Instance.State == GameState.Playing
                && GraphicsSettingsManager.Instance != null
                && GraphicsSettingsManager.Instance.TouchControlsEnabled;
            MobileInput.Tick(active);
        }

        void OnDisable()
        {
            MobileInput.Clear();
        }
    }
}

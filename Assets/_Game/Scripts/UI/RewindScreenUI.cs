using UnityEngine;
using UnityEngine.UI;
using TMPro;

// UI overlay displayed during the Prince of Persia-style Rewind Time sequence.
public class RewindScreenUI : MonoBehaviour
{
    [Header("UI Visuals")]
    [SerializeField] private TextMeshProUGUI rewindTitleText;
    [SerializeField] private Image rewindRadialProgress;

    private void Update()
    {
        // Pulse title text with neon effect
        if (rewindTitleText != null)
        {
            float scale = 1f + (Mathf.Sin(Time.unscaledTime * 8f) * 0.08f);
            rewindTitleText.transform.localScale = Vector3.one * scale;
        }

        // Rotate radial icon counter-clockwise to emphasize winding backward
        if (rewindRadialProgress != null)
        {
            rewindRadialProgress.transform.Rotate(0f, 0f, 360f * Time.unscaledDeltaTime);
        }
    }
}

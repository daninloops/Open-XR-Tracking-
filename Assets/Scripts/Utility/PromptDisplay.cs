using TMPro;
using UnityEngine;

/// <summary>
/// World-space 3D text for step instructions and correction warnings.
/// Attach to your World Space Canvas.
/// </summary>
public class PromptDisplay : MonoBehaviour
{
    public TextMeshProUGUI promptText;

    [Header("Optional: separate text for 'Step X of Y'")]
    public TextMeshProUGUI stepCounterText;

    public void ShowPrompt(string message)
    {
        if (promptText) promptText.text = message;
        gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptText) promptText.text = "";
        if (stepCounterText) stepCounterText.text = "";
        gameObject.SetActive(false);
    }

    /// <summary>Updates the "Step X of Y" counter. stepIndex is 0-based.</summary>
    public void ShowStepCounter(int stepIndex, int totalSteps)
    {
        if (!stepCounterText) return;
        stepCounterText.text = $"Step {stepIndex + 1} of {totalSteps}";
    }
}
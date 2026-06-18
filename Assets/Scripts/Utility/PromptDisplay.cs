using TMPro;
using UnityEngine;

/// <summary>
/// World-space 3D text for step instructions and correction warnings.
/// Attach to your World Space Canvas.
/// </summary>
public class PromptDisplay : MonoBehaviour
{
    public TextMeshProUGUI promptText;

    public void ShowPrompt(string message)
    {
        if (promptText) promptText.text = message;
        gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptText) promptText.text = "";
        gameObject.SetActive(false);
    }
}
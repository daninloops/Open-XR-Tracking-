// SelectionUI.cs
// Draws a UI list of O1, O2, O3, O4 buttons on screen.
// When a button is clicked, tells ArrowSelector which object was selected.

using UnityEngine;
using System.Collections.Generic;

public class SelectionUI : MonoBehaviour
{
    [SerializeField] private ArrowSelector arrowSelector;

    // List of object names shown in the UI — populated at runtime
    private List<string> objectNames = new List<string>();

    // Which index is currently selected (-1 = none)
    private int selectedIndex = -1;

    // Called by AnchorManager when a new object is spawned
    public void RegisterObject(string name, int index)
    {
        // Ensure list is big enough
        while (objectNames.Count <= index)
            objectNames.Add("");

        objectNames[index] = name;
        Debug.Log($"[SelectionUI] Registered {name} at index {index}");
    }

    void OnGUI()
    {
        if (objectNames.Count == 0) return;

        // Draw a panel background on the left side of screen
        GUI.Box(new Rect(10, 10, 120, objectNames.Count * 45 + 20), "Objects");

        for (int i = 0; i < objectNames.Count; i++)
        {
            // Highlight selected button in cyan
            GUI.backgroundColor = (i == selectedIndex) ? Color.cyan : Color.white;

            // Draw button for each object
            if (GUI.Button(new Rect(20, 40 + i * 45, 100, 35), objectNames[i]))
            {
                selectedIndex = i;
                arrowSelector.SelectObject(i);
                Debug.Log($"[SelectionUI] Selected {objectNames[i]}");
            }
        }

        // Reset color
        GUI.backgroundColor = Color.white;
    }
}
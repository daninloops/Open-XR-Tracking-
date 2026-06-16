using UnityEngine;
using System.Collections.Generic;

public class SelectionUI : MonoBehaviour
{
    [SerializeField] private ArrowSelector arrowSelector;

    private List<string> objectNames = new List<string>();
    private int selectedIndex = -1;

    public void RegisterObject(string name, int index)
    {
        while (objectNames.Count <= index)
            objectNames.Add("");

        objectNames[index] = name;
        Debug.Log($"[SelectionUI] Registered {name} at index {index}");
    }

    void OnGUI()
    {
        if (objectNames.Count == 0)
        {
            GUI.Box(new Rect(10, 10, 140, 50), "No objects yet");
            return;
        }

        float panelWidth = 140f;
        float panelX = Screen.width - panelWidth - 10f;
        float panelHeight = objectNames.Count * 45 + 20;

        GUI.Box(new Rect(panelX, 10, panelWidth, panelHeight), "Objects");

        for (int i = 0; i < objectNames.Count; i++)
        {
            GUI.backgroundColor = (i == selectedIndex) ? Color.cyan : Color.white;

            if (GUI.Button(new Rect(panelX + 10, 40 + i * 45, 120, 35), objectNames[i]))
            {
                selectedIndex = i;
                Debug.Log($"[SelectionUI] Button clicked: {objectNames[i]}");

                if (arrowSelector == null)
                {
                    Debug.LogError("[SelectionUI] ArrowSelector is NULL — drag SelectionSystem into the Arrow Selector slot!");
                    return;
                }

                arrowSelector.SelectObject(i);
            }
        }

        GUI.backgroundColor = Color.white;
    }
}
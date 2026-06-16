using UnityEngine;
using System.Collections.Generic;

public class ArrowSelector : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;

    private Dictionary<int, Transform> anchorMap = new Dictionary<int, Transform>();
    private GameObject arrowInstance;

    // Currently selected anchor index (-1 = none)
    private int selectedIndex = -1;

    void Start()
    {
        arrowInstance = Instantiate(arrowPrefab);
        arrowInstance.SetActive(false);
    }

    void Update()
    {
        // Rotate 90 degrees so arrow faces up correctly on the Quad
arrowInstance.transform.LookAt(Camera.main.transform);
arrowInstance.transform.Rotate(0, 0, 90f);
        // Every frame — if something is selected, keep arrow on top of its anchor
        if (selectedIndex >= 0 && anchorMap.ContainsKey(selectedIndex))
        {
            Transform anchor = anchorMap[selectedIndex];

            // Continuously update arrow position above the anchor
            arrowInstance.transform.position = anchor.position + Vector3.up * 0.8f;

            // Keep arrow facing the camera
            arrowInstance.transform.LookAt(Camera.main.transform);
        }
    }

    public void RegisterAnchor(int index, Transform anchorTransform)
    {
        anchorMap[index] = anchorTransform;
        Debug.Log($"[ArrowSelector] Registered anchor for O{index + 1}");
    }

    public void SelectObject(int index)
    {
        if (!anchorMap.ContainsKey(index))
        {
            Debug.LogWarning($"[ArrowSelector] No anchor registered for index {index}");
            return;
        }

        selectedIndex = index;
        arrowInstance.SetActive(true);

        Debug.Log($"[ArrowSelector] Now tracking A{index + 1}");
    }
}
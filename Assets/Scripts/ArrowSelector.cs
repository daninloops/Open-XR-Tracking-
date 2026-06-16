// ArrowSelector.cs
// Manages the arrow GameObject that points to the selected anchor.
// When SelectionUI tells us which object index is selected,
// we move the arrow to that object's anchor position.

using UnityEngine;
using System.Collections.Generic;

public class ArrowSelector : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;

    // Holds the live anchor positions — populated by RegisterAnchor()
    // Key: object index (0=O1, 1=O2, etc.)
    // Value: the Transform of that object's anchor
    private Dictionary<int, Transform> anchorMap = new Dictionary<int, Transform>();

    // The single arrow instance in the scene
    private GameObject arrowInstance;

    void Start()
    {
        // Spawn the arrow once — hidden until a selection is made
        arrowInstance = Instantiate(arrowPrefab);
        arrowInstance.SetActive(false);
    }

    // Called by AnchorManager when an anchor is created for an object
    // index: 0-based index matching the object list (O1=0, O2=1...)
    // anchorTransform: the Transform of the spawned anchor cube
    public void RegisterAnchor(int index, Transform anchorTransform)
    {
        anchorMap[index] = anchorTransform;
        Debug.Log($"[ArrowSelector] Registered anchor for O{index + 1}");
    }

    // Called by SelectionUI when the user clicks an object button
    public void SelectObject(int index)
    {
        if (!anchorMap.ContainsKey(index))
        {
            Debug.LogWarning($"[ArrowSelector] No anchor registered for index {index}");
            return;
        }

        Transform anchor = anchorMap[index];

        // Position arrow slightly above the anchor so it's visible
        arrowInstance.transform.position = anchor.position + Vector3.up * 0.5f;

        // Make arrow face the camera
        arrowInstance.transform.LookAt(Camera.main.transform);
        arrowInstance.transform.Rotate(0, 180f, 0);

        arrowInstance.SetActive(true);

        Debug.Log($"[ArrowSelector] Arrow moved to A{index + 1} at {anchor.position}");
    }
}
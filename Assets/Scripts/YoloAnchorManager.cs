using UnityEngine;
using System.Collections.Generic;

public class YoloAnchorManager : MonoBehaviour
{
    [SerializeField] private AnchorTracker anchorTracker;
    [SerializeField] private ArrowSelector arrowSelector;
    [SerializeField] private SelectionUI selectionUI;

    private Dictionary<string, AnchorTracker.IAnchorMarker> anchorMap
        = new Dictionary<string, AnchorTracker.IAnchorMarker>();

    // Tracks label → index so SelectionUI knows which button maps to which anchor
    private Dictionary<string, int> indexMap = new Dictionary<string, int>();

    private int objectCount = 0;

    public void ProcessDetection(Detection detection, Vector3 worldPosition)
    {
        if (detection.confidence < 0.5f) return;

        if (!anchorMap.ContainsKey(detection.label))
        {
            objectCount++;

            // CreateAnchor now returns the index via out parameter
            int index;
            AnchorTracker.IAnchorMarker marker = anchorTracker.CreateAnchor(
                detection.label, worldPosition, out index
            );

            anchorMap[detection.label] = marker;
            indexMap[detection.label] = index;

            // Register with UI and ArrowSelector
            selectionUI.RegisterObject($"O{objectCount}", index);
            arrowSelector.RegisterAnchor(index, marker.GetTransform());

            Debug.Log($"[YoloAnchorManager] New label '{detection.label}' → anchor created.");
        }
        else
        {
            anchorTracker.MoveAnchor(anchorMap[detection.label], worldPosition);
            Debug.Log($"[YoloAnchorManager] Known label '{detection.label}' → anchor moved.");
        }
    }
}
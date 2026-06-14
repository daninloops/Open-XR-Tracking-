// YoloAnchorManager.cs
// This is the brain of the pipeline.
// It receives Detection objects and decides:
//   → Is this label new?  Call CreateAnchor()
//   → Have we seen it before?  Call MoveAnchor()
// It also owns the label dictionary so each label maps to exactly one anchor.

using UnityEngine;
using System.Collections.Generic;

public class YoloAnchorManager : MonoBehaviour
{
    [SerializeField] private AnchorTracker anchorTracker;

    // Maps each label string to its live anchor marker
    // e.g. "cup" → the IAnchorMarker managing the cup's anchor
    private Dictionary<string, AnchorTracker.IAnchorMarker> anchorMap
        = new Dictionary<string, AnchorTracker.IAnchorMarker>();

    // Called every frame (or every detection cycle) with the latest Detection
    // worldPosition is computed by the projection pipeline (Part C, Day 2)
    public void ProcessDetection(Detection detection, Vector3 worldPosition)
    {
        // Ignore low-confidence detections (below 50%)
        if (detection.confidence < 0.5f)
        {
            Debug.Log($"[YoloAnchorManager] Skipping '{detection.label}' — low confidence ({detection.confidence})");
            return;
        }

        if (!anchorMap.ContainsKey(detection.label))
        {
            // First time seeing this label → create a new anchor
            AnchorTracker.IAnchorMarker marker = anchorTracker.CreateAnchor(detection.label, worldPosition);

            // Store it so we can update it on future detections
            anchorMap[detection.label] = marker;

            Debug.Log($"[YoloAnchorManager] New label '{detection.label}' → anchor created.");
        }
        else
        {
            // Label seen before → update the existing anchor's position
            anchorTracker.MoveAnchor(anchorMap[detection.label], worldPosition);

            Debug.Log($"[YoloAnchorManager] Known label '{detection.label}' → anchor moved.");
        }
    }
}
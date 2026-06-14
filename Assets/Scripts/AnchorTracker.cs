// AnchorTracker.cs
// Responsible for the LIFECYCLE of one anchor:
// - CreateAnchor(): first time we see an object → spawn + fly + attach
// - MoveAnchor(): object seen again at a new position → smoothly move marker

using UnityEngine;
using System.Collections;

public class AnchorTracker : MonoBehaviour
{
    [SerializeField] private GameObject anchorPrefab;

    // How close a new detection must be to the existing anchor
    // before we treat it as the same object (in world units / metres)
    [SerializeField] private float followThreshold = 0.5f;

    private int anchorCount = 0;

    // IAnchorMarker is a tiny interface so we can swap
    // this simple marker for OVRSpatialAnchor later without
    // touching AnchorTracker or YoloAnchorManager
    public interface IAnchorMarker
    {
        void MoveTo(Vector3 worldPosition);
        Vector3 GetPosition();
    }

    // The concrete marker that wraps a plain GameObject
    private class SimpleMarker : IAnchorMarker
    {
        private GameObject go;

        public SimpleMarker(GameObject go) { this.go = go; }

        // Moves the marker to a new world position
        public void MoveTo(Vector3 pos) { go.transform.position = pos; }

        public Vector3 GetPosition() { return go.transform.position; }
    }

    // Called the FIRST time a label is detected
    // worldPosition comes from the raycast in YoloAnchorManager
    public IAnchorMarker CreateAnchor(string label, Vector3 worldPosition)
    {
        anchorCount++;

        GameObject anchor = Instantiate(anchorPrefab, worldPosition + Vector3.up, Quaternion.identity);
        anchor.name = $"A{anchorCount}";

        CreateLabel(anchor, $"A{anchorCount}\n{label}", Color.cyan);

        Debug.Log($"[AnchorTracker] Created {anchor.name} for '{label}' at {worldPosition}");

        // Wrap in SimpleMarker and return so YoloAnchorManager can hold a reference
        return new SimpleMarker(anchor);
    }

    // Called when the SAME label is detected again at a new position
    // Smoothly slides the existing marker to the new computed position
    public void MoveAnchor(IAnchorMarker marker, Vector3 newWorldPosition)
    {
        Vector3 target = newWorldPosition + Vector3.up;

        // Only move if the new position is meaningfully different
        // Avoids jitter from tiny detection wobble
        if (Vector3.Distance(marker.GetPosition(), target) > followThreshold)
        {
            StartCoroutine(SmoothMove(marker, target));
            Debug.Log($"[AnchorTracker] Moving anchor to {newWorldPosition}");
        }
    }

    // Smoothly interpolates marker from current to target position over 0.4 seconds
    private IEnumerator SmoothMove(IAnchorMarker marker, Vector3 target)
    {
        Vector3 start = marker.GetPosition();
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            marker.MoveTo(Vector3.Lerp(start, target, t));
            yield return null;
        }

        marker.MoveTo(target);
    }

    private void CreateLabel(GameObject target, string text, Color color)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(target.transform);
        labelObj.transform.localPosition = new Vector3(0, 1.2f, 0);

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 24;
        tm.color = color;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        labelObj.transform.localScale = Vector3.one * 0.1f;
    }
}
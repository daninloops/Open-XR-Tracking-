using UnityEngine;
using System.Collections;

public class AnchorTracker : MonoBehaviour
{
    [SerializeField] private GameObject anchorPrefab;
    [SerializeField] private float followThreshold = 0.5f;

    private int anchorCount = 0;

    public interface IAnchorMarker
    {
        void MoveTo(Vector3 worldPosition);
        Vector3 GetPosition();
        // Added: so ArrowSelector can read the anchor's Transform
        Transform GetTransform();
    }

    private class SimpleMarker : IAnchorMarker
    {
        private GameObject go;

        public SimpleMarker(GameObject go) { this.go = go; }

        public void MoveTo(Vector3 pos) { go.transform.position = pos; }
        public Vector3 GetPosition() { return go.transform.position; }

        // Returns the anchor GameObject's Transform
        public Transform GetTransform() { return go.transform; }
    }

    // Now returns both the marker AND the anchor index
    // so AnchorManager can pass the index to ArrowSelector
    public IAnchorMarker CreateAnchor(string label, Vector3 worldPosition, out int index)
    {
        anchorCount++;
        index = anchorCount - 1; // 0-based index: A1=0, A2=1...

        GameObject anchor = Instantiate(anchorPrefab, worldPosition + Vector3.up, Quaternion.identity);
        anchor.name = $"A{anchorCount}";

        // Scale down so anchor is a tiny cube on top of the object
        anchor.transform.localScale = Vector3.one * 0.2f;

        CreateLabel(anchor, $"A{anchorCount}\n{label}", Color.cyan);

        Debug.Log($"[AnchorTracker] Created {anchor.name} for '{label}' at {worldPosition}");

        return new SimpleMarker(anchor);
    }

    public void MoveAnchor(IAnchorMarker marker, Vector3 newWorldPosition)
    {
        Vector3 target = newWorldPosition + Vector3.up;

        if (Vector3.Distance(marker.GetPosition(), target) > followThreshold)
        {
            StartCoroutine(SmoothMove(marker, target));
            Debug.Log($"[AnchorTracker] Moving anchor to {newWorldPosition}");
        }
    }

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
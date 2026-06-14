using UnityEngine;

public class BoundingBoxProjector : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private Camera feedCamera;
    [SerializeField] private YoloAnchorManager yoloAnchorManager;

    [Header("Settings")]
    [SerializeField] private string detectionLabel = "cup";

    private float xMin, yMin, xMax, yMax;
    private Rect screenBox;
    private bool hasValidBox = false;

    void Update()
    {
        if (targetObject == null || feedCamera == null) return;

        // ===================== PART B — 2D BOUNDING BOX =====================

        Bounds bounds = targetObject.GetComponent<Renderer>().bounds;

        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

        xMin = float.MaxValue; yMin = float.MaxValue;
        xMax = float.MinValue; yMax = float.MinValue;

        foreach (Vector3 corner in corners)
        {
            Vector3 screenPoint = feedCamera.WorldToScreenPoint(corner);
            if (screenPoint.z < 0) continue;

            if (screenPoint.x < xMin) xMin = screenPoint.x;
            if (screenPoint.y < yMin) yMin = screenPoint.y;
            if (screenPoint.x > xMax) xMax = screenPoint.x;
            if (screenPoint.y > yMax) yMax = screenPoint.y;
        }

        if (xMin == float.MaxValue) { hasValidBox = false; return; }
        hasValidBox = true;

        float scaleX = 1f, scaleY = 1f;
        if (feedCamera.targetTexture != null)
        {
            scaleX = (float)Screen.width  / feedCamera.targetTexture.width;
            scaleY = (float)Screen.height / feedCamera.targetTexture.height;
        }

        screenBox = new Rect(
            xMin * scaleX,
            Screen.height - (yMax * scaleY),
            (xMax - xMin) * scaleX,
            (yMax - yMin) * scaleY
        );

        Detection detection = new Detection(detectionLabel, xMin, yMin, xMax, yMax, 1.0f);

        // ===================== PART C — PROJECT BOX TO 3D POINT =====================

        // Use the 3D centre of the object's bounds directly to build the ray
        // This is more reliable than using the 2D pixel centre
        Vector3 objectCentre = bounds.center;
        Vector3 screenCentre = feedCamera.WorldToScreenPoint(objectCentre);
        Ray ray = feedCamera.ScreenPointToRay(screenCentre);

        Debug.DrawRay(ray.origin, ray.direction * 20f, Color.red, 0.1f);

        // RaycastAll checks every object hit — finds TargetObject even if floor is hit first
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"[Projector] Ray hit '{hit.collider.name}'");
            if (hit.collider.gameObject == targetObject)
            {
                Vector3 worldPosition = hit.point;
                Debug.Log($"[Projector] Confirmed TargetObject at {worldPosition}");

                // ===================== PART D — FEED THE TRACKER =====================
                yoloAnchorManager.ProcessDetection(detection, worldPosition);
                break;
            }
        }
    }

    // ===================== PART B — DRAW BOX ON SCREEN =====================

    void OnGUI()
    {
        if (!hasValidBox) return;

        float thickness = 2f;
        GUI.color = Color.cyan;
        GUI.DrawTexture(new Rect(screenBox.x, screenBox.y, screenBox.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(screenBox.x, screenBox.y + screenBox.height, screenBox.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(screenBox.x, screenBox.y, thickness, screenBox.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(screenBox.x + screenBox.width, screenBox.y, thickness, screenBox.height), Texture2D.whiteTexture);

        GUI.color = Color.yellow;
        GUI.Label(new Rect(screenBox.x, screenBox.y - 20f, 200f, 20f), $"{detectionLabel} [simulated]");
    }
}
using UnityEngine;

public class BoundingBoxProjector : MonoBehaviour // Unity script attached to a GameObject
{
    [Header("Scene References")]

    [SerializeField] private GameObject targetObject;
    // The object we want to detect and generate a bounding box around

    [SerializeField] private Camera feedCamera;
    // Camera used to view the object and convert 3D positions into screen coordinates

    [SerializeField] private YoloAnchorManager yoloAnchorManager;
    // Receives the simulated YOLO detection and creates/updates anchors

    [Header("Settings")]

    [SerializeField] private string detectionLabel = "cup";
    // Label assigned to the simulated detection

    private float xMin, yMin, xMax, yMax;
    // Stores the minimum and maximum screen coordinates of the bounding box

    private Rect screenBox;
    // Stores the final 2D rectangle drawn on the screen

    private bool hasValidBox = false;
    // Indicates whether a valid bounding box was successfully calculated

    void Update()
    {
        // Stop execution if required references are missing
        if (targetObject == null || feedCamera == null) return;

        // ===================== PART B — 2D BOUNDING BOX =====================

        Bounds bounds = targetObject.GetComponent<Renderer>().bounds;
        // Get the object's 3D bounding box from its renderer

        Vector3[] corners = new Vector3[8];
        // Store all 8 corners of the object's bounding box

        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        // Bottom-left-back corner

        corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        // Bottom-left-front corner

        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        // Top-left-back corner

        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        // Top-left-front corner

        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        // Bottom-right-back corner

        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        // Bottom-right-front corner

        corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        // Top-right-back corner

        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
        // Top-right-front corner

        xMin = float.MaxValue;
        yMin = float.MaxValue;
        xMax = float.MinValue;
        yMax = float.MinValue;
        // Initialize extreme values so we can find the true min/max coordinates

        foreach (Vector3 corner in corners)
        {
            Vector3 screenPoint = feedCamera.WorldToScreenPoint(corner);
            // Convert each 3D corner into a 2D screen coordinate

            if (screenPoint.z < 0) continue;
            // Ignore points located behind the camera

            if (screenPoint.x < xMin) xMin = screenPoint.x;
            // Find leftmost screen coordinate

            if (screenPoint.y < yMin) yMin = screenPoint.y;
            // Find bottommost screen coordinate

            if (screenPoint.x > xMax) xMax = screenPoint.x;
            // Find rightmost screen coordinate

            if (screenPoint.y > yMax) yMax = screenPoint.y;
            // Find topmost screen coordinate
        }

        if (xMin == float.MaxValue)
        {
            hasValidBox = false;
            return;
        }
        // If no visible corners were found, the object is not visible

        hasValidBox = true;
        // Bounding box was successfully calculated

        float scaleX = 1f, scaleY = 1f;
        // Default scaling values

        if (feedCamera.targetTexture != null)
        {
            scaleX = (float)Screen.width / feedCamera.targetTexture.width;
            scaleY = (float)Screen.height / feedCamera.targetTexture.height;
        }
        // Scale coordinates if the camera is rendering to a texture

        screenBox = new Rect(
            xMin * scaleX,
            Screen.height - (yMax * scaleY),
            (xMax - xMin) * scaleX,
            (yMax - yMin) * scaleY
        );
        // Create the final screen-space rectangle used for visualization

        Detection detection = new Detection(
            detectionLabel,
            xMin,
            yMin,
            xMax,
            yMax,
            1.0f
        );
        // Create a simulated YOLO detection with confidence 100%

        // ===================== PART C — PROJECT BOX TO 3D POINT =====================

        Vector3 objectCentre = bounds.center;
        // Get the center point of the object's bounding box

        Vector3 screenCentre = feedCamera.WorldToScreenPoint(objectCentre);
        // Convert the center point into screen coordinates

        Ray ray = feedCamera.ScreenPointToRay(screenCentre);
        // Create a ray from the camera through the center pixel

        Debug.DrawRay(ray.origin, ray.direction * 20f, Color.red, 0.1f);
        // Draw the ray in Scene View for debugging

        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        // Check every object intersected by the ray

        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"[Projector] Ray hit '{hit.collider.name}'");
            // Display all hit objects in the console

            if (hit.collider.gameObject == targetObject)
            {
                Vector3 worldPosition = hit.point;
                // Store the exact world position where the ray hit the target object

                Debug.Log($"[Projector] Confirmed TargetObject at {worldPosition}");

                yoloAnchorManager.ProcessDetection(detection, worldPosition);
                // Send the detection and 3D position to the anchor manager

                break;
                // Stop searching after finding the target object
            }
        }
    }

    void OnGUI()
    {
        if (!hasValidBox) return;
        // Don't draw anything if no valid bounding box exists

        float thickness = 2f;
        // Thickness of bounding box border

        GUI.color = Color.cyan;
        // Set bounding box color

        GUI.DrawTexture(
            new Rect(screenBox.x, screenBox.y, screenBox.width, thickness),
            Texture2D.whiteTexture
        );
        // Draw top border

        GUI.DrawTexture(
            new Rect(screenBox.x, screenBox.y + screenBox.height, screenBox.width, thickness),
            Texture2D.whiteTexture
        );
        // Draw bottom border

        GUI.DrawTexture(
            new Rect(screenBox.x, screenBox.y, thickness, screenBox.height),
            Texture2D.whiteTexture
        );
        // Draw left border

        GUI.DrawTexture(
            new Rect(screenBox.x + screenBox.width, screenBox.y, thickness, screenBox.height),
            Texture2D.whiteTexture
        );
        // Draw right border

        GUI.color = Color.yellow;
        // Set label color

        GUI.Label(
            new Rect(screenBox.x, screenBox.y - 20f, 200f, 20f),
            $"{detectionLabel} [simulated]"
        );
        // Display the detection label above the bounding box
    }
}
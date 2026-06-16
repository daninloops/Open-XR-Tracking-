using UnityEngine;
using System.Collections.Generic;

public class BoundingBoxProjector : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Camera feedCamera;
    [SerializeField] private YoloAnchorManager yoloAnchorManager;

    [Header("Target Objects")]
    // Drag all 4 spheres here in the Inspector
    [SerializeField] private GameObject[] targetObjects;

    // Stores one screenBox per object for drawing
    private Rect[] screenBoxes;
    private bool[] hasValidBox;
    private string[] labels;

    void Start()
    {
        screenBoxes = new Rect[targetObjects.Length];
        hasValidBox = new bool[targetObjects.Length];
        labels = new string[targetObjects.Length];

        // Use each object's name as its label: O1, O2, O3, O4
        for (int i = 0; i < targetObjects.Length; i++)
            labels[i] = targetObjects[i].name;
    }

    void Update()
    {
        if (feedCamera == null) return;

        // Process each object separately
        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] == null) continue;
            ProcessObject(i);
        }
    }

    void ProcessObject(int i)
    {
        GameObject obj = targetObjects[i];
        Bounds bounds = obj.GetComponent<Renderer>().bounds;

        // Build 8 corners
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

        float xMin = float.MaxValue, yMin = float.MaxValue;
        float xMax = float.MinValue, yMax = float.MinValue;

        foreach (Vector3 corner in corners)
        {
            Vector3 sp = feedCamera.WorldToScreenPoint(corner);
            if (sp.z < 0) continue;

            if (sp.x < xMin) xMin = sp.x;
            if (sp.y < yMin) yMin = sp.y;
            if (sp.x > xMax) xMax = sp.x;
            if (sp.y > yMax) yMax = sp.y;
        }

        if (xMin == float.MaxValue)
        {
            hasValidBox[i] = false;
            return;
        }

        hasValidBox[i] = true;

        // Y flip for GUI drawing
        screenBoxes[i] = new Rect(
            xMin,
            Screen.height - yMax,
            xMax - xMin,
            yMax - yMin
        );

        // Wrap in Detection format
        Detection detection = new Detection(labels[i], xMin, yMin, xMax, yMax, 1.0f);

        // Cast ray from box centre
        float centerX = (xMin + xMax) / 2f;
        float centerY = (yMin + yMax) / 2f;

        Ray ray = feedCamera.ScreenPointToRay(new Vector3(centerX, centerY, 0));
        Debug.DrawRay(ray.origin, ray.direction * 20f, Color.red, 0.1f);

        // Use nearest hit
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.Log($"[Projector] {labels[i]} ray hit '{hit.collider.name}'");

            if (hit.collider.gameObject == obj)
            {
                yoloAnchorManager.ProcessDetection(detection, hit.point);
            }
        }
    }

    void OnGUI()
    {
        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (!hasValidBox[i]) continue;

            // Different color per object
            Color[] colors = { Color.cyan, Color.green, Color.yellow, Color.magenta };
            GUI.color = colors[i % colors.Length];

            Rect box = screenBoxes[i];
            float t = 2f;
            GUI.DrawTexture(new Rect(box.x, box.y, box.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.y + box.height, box.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.y, t, box.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x + box.width, box.y, t, box.height), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(box.x, box.y - 20f, 100f, 20f), labels[i]);
        }

        GUI.color = Color.white;
    }
}
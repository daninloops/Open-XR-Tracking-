using UnityEngine;

/// <summary>
/// Attach to a child empty GameObject on each item to mark a specific feature.
/// e.g. "LogoMarker", "SeamMarker", "EquatorMarker"
///
/// The LOCAL forward (Z+) of this transform = the direction the feature "faces".
/// The LOCAL up (Y+) of this transform = the "up" of the feature (used for seam checks).
///
/// In the Scene view, use the gizmo arrow to confirm the marker is pointing
/// in the right direction before pressing Play.
/// </summary>
public class OrientationMarker : MonoBehaviour
{
    [Header("Gizmo display")]
    public Color gizmoColor = Color.cyan;
    public float gizmoLength = 0.2f;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        // Forward = facing direction
        Gizmos.DrawRay(transform.position, transform.forward * gizmoLength);
        // Up = vertical reference
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.up * (gizmoLength * 0.5f));
    }
}
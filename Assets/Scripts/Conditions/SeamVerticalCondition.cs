using UnityEngine;

/// <summary>
/// Item3: Met when the SeamMarker child's UP direction aligns with the camera's UP direction.
/// This means the seam runs as a vertical line in the user's view.
/// Uses dot product between seam's up and camera's up.
/// dot close to 1.0 = seam is vertical in the user's view.
/// </summary>
public class SeamVerticalCondition : ICondition
{
    private readonly Transform _seamMarker;  // the white vertical quad child
    private readonly Transform _camera;
    private readonly float     _threshold;   // how precise (0.95 = within ~18°)

    public SeamVerticalCondition(Transform seamMarker, Transform camera, float threshold = 0.95f)
    {
        _seamMarker = seamMarker;
        _camera     = camera;
        _threshold  = threshold;
    }

    public void OnStepBegin() { }
    public void OnStepEnd()   { }

    public bool Check()
    {
        if (_seamMarker == null || _camera == null) return false;

        // Which way the seam is currently pointing (its local UP axis)
        Vector3 seamUp = _seamMarker.up;

        // Which way is UP in the camera's view
        Vector3 cameraUp = _camera.up;

        // Mathf.Abs because seam pointing up OR down both count as vertical
        float dot = Mathf.Abs(Vector3.Dot(seamUp, cameraUp));

        return dot >= _threshold;
    }
}

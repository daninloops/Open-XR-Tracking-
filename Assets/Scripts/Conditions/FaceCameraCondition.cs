using UnityEngine;

/// <summary>
/// Item1: Met when the LogoMarker child is pointing squarely at the camera.
/// Uses dot product between the marker's forward direction and the 
/// direction from the object to the camera.
/// dot close to 1.0 = logo facing camera directly.
/// </summary>
public class FaceCameraCondition : ICondition
{
    private readonly Transform _logoMarker;   // the green quad child
    private readonly Transform _camera;
    private readonly float     _threshold;    // how precise (0.95 = within ~18°)

    public FaceCameraCondition(Transform logoMarker, Transform camera, float threshold = 0.95f)
    {
        _logoMarker = logoMarker;
        _camera     = camera;
        _threshold  = threshold;
    }

    public void OnStepBegin() { }
    public void OnStepEnd()   { }

    public bool Check()
    {
        if (_logoMarker == null || _camera == null) return false;

        // Direction the logo is currently pointing
        Vector3 logoForward = _logoMarker.forward;

        // Direction from the item toward the camera
        Vector3 toCamera = (_camera.position - _logoMarker.position).normalized;

        // Dot product: 1.0 = perfectly aligned, 0 = perpendicular
        float dot = Vector3.Dot(logoForward, toCamera);

        return dot >= _threshold;
    }
}

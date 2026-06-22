using UnityEngine;

/// <summary>
/// Condition: the OrientationMarker's UP vector must align with the camera's UP vector,
/// meaning the seam/line runs vertically down the center of the user's view.
///
/// Use for:
///   - Item 3: turn anticlockwise until the white seam runs as a vertical line.
///
/// How it works:
///   We project both the marker's up and the camera's up onto the plane perpendicular
///   to the camera's forward, then measure the angle between them.
///   When angle < tolerance, the seam is vertical in the user's view.
///
/// JSON condition name: "verticalseam"
/// </summary>
public class VerticalAlignCondition : ICondition
{
    private readonly OrientationMarker _marker;
    private readonly Transform         _camera;
    private readonly float             _angleTolerance;
    private readonly float             _holdDuration;

    private float _heldTime;
    private bool  _isMet;

    public VerticalAlignCondition(OrientationMarker marker, Transform camera,
                                  float angleTolerance = 15f, float holdDuration = 1f)
    {
        _marker         = marker;
        _camera         = camera;
        _angleTolerance = angleTolerance;
        _holdDuration   = holdDuration;
    }

    public void OnStepBegin() { _heldTime = 0f; _isMet = false; }
    public void OnStepEnd()   { }

    public bool Check()
    {
        if (_marker == null || _camera == null) return false;

        // Project marker's up and camera's up onto screen plane
        Vector3 camForward   = _camera.forward;
        Vector3 markerUp     = Vector3.ProjectOnPlane(_marker.transform.up, camForward).normalized;
        Vector3 cameraUp     = Vector3.ProjectOnPlane(_camera.up, camForward).normalized;

        float angle = Vector3.Angle(markerUp, cameraUp);

        if (angle <= _angleTolerance)
        {
            _heldTime += Time.deltaTime;
            if (_heldTime >= _holdDuration)
                _isMet = true;
        }
        else
        {
            _heldTime = 0f;
        }

        return _isMet;
    }
}
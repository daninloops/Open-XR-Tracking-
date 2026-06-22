using UnityEngine;

/// <summary>
/// Condition: the OrientationMarker's forward direction must point toward the
/// user's camera within 'angleTolerance' degrees.
///
/// Use for:
///   - Item 1: logo stamp faces user squarely at eye level
///   - Item 4 (first part): equator seam ring faces the user
///
/// JSON condition name: "faceuser"
/// </summary>
public class FaceUserCondition : ICondition
{
    private readonly OrientationMarker _marker;
    private readonly Transform         _camera;
    private readonly float             _angleTolerance;
    private readonly float             _holdDuration;   // seconds to hold the pose

    private float _heldTime;
    private bool  _isMet;

    /// <param name="marker">The LogoMarker / EquatorMarker child transform.</param>
    /// <param name="camera">Main camera transform (user's eye).</param>
    /// <param name="angleTolerance">How many degrees off-center is still "facing". Default 20°.</param>
    /// <param name="holdDuration">How many seconds the pose must be held. Default 1s.</param>
    public FaceUserCondition(OrientationMarker marker, Transform camera,
                             float angleTolerance = 20f, float holdDuration = 1f)
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

        // Direction from marker to camera
        Vector3 toCamera = (_camera.position - _marker.transform.position).normalized;

        // Angle between marker's forward and the direction to camera
        float angle = Vector3.Angle(_marker.transform.forward, toCamera);

        if (angle <= _angleTolerance)
        {
            _heldTime += Time.deltaTime;
            if (_heldTime >= _holdDuration)
            {
                _isMet = true;
            }
        }
        else
        {
            // Reset hold timer if they move away
            _heldTime = 0f;
        }

        return _isMet;
    }
}
using UnityEngine;

/// <summary>
/// Condition: TWO checks must both be true simultaneously and held:
///   1. The equator marker's forward faces the user (like FaceUserCondition).
///   2. The object is tilted upward by 'targetTiltDegrees' on the camera's X axis.
///
/// Use for:
///   - Item 4: position so equator seam faces user, then tilt 45° upward.
///
/// JSON condition name: "faceandtilt"
/// </summary>
public class FaceAndTiltCondition : ICondition
{
    private readonly OrientationMarker _equatorMarker;
    private readonly Transform         _objectTransform;
    private readonly Transform         _camera;
    private readonly float             _targetTilt;       // degrees upward, e.g. 45
    private readonly float             _faceTolerance;    // degrees, facing check
    private readonly float             _tiltTolerance;    // degrees, tilt check
    private readonly float             _holdDuration;

    private float _heldTime;
    private bool  _isMet;

    public FaceAndTiltCondition(OrientationMarker equatorMarker, Transform objectTransform,
                                Transform camera, float targetTilt = 45f,
                                float faceTolerance = 20f, float tiltTolerance = 10f,
                                float holdDuration = 1f)
    {
        _equatorMarker   = equatorMarker;
        _objectTransform = objectTransform;
        _camera          = camera;
        _targetTilt      = targetTilt;
        _faceTolerance   = faceTolerance;
        _tiltTolerance   = tiltTolerance;
        _holdDuration    = holdDuration;
    }

    public void OnStepBegin() { _heldTime = 0f; _isMet = false; }
    public void OnStepEnd()   { }

    public bool Check()
    {
        if (_equatorMarker == null || _camera == null || _objectTransform == null)
            return false;

        // --- Check 1: equator faces user ---
        Vector3 toCamera   = (_camera.position - _equatorMarker.transform.position).normalized;
        float   faceAngle  = Vector3.Angle(_equatorMarker.transform.forward, toCamera);
        bool    facesMet   = faceAngle <= _faceTolerance;

        // --- Check 2: object tilted upward by targetTilt degrees ---
        // We measure the angle between the object's up and the world horizontal plane.
        // Tilt = how far the object's local up has rotated toward the camera's forward.
        Vector3 objectUp    = _objectTransform.up;
        Vector3 camForward  = _camera.forward;

        // Dot product tells us how much the object up aligns with cam forward
        // When perfectly tilted toward user: objectUp dot camForward = sin(tiltAngle)
        float   sinTilt     = Vector3.Dot(objectUp, camForward);
        float   measuredTilt = Mathf.Asin(Mathf.Clamp(sinTilt, -1f, 1f)) * Mathf.Rad2Deg;
        bool    tiltMet     = Mathf.Abs(measuredTilt - _targetTilt) <= _tiltTolerance;

        if (facesMet && tiltMet)
        {
            _heldTime += Time.deltaTime;
            if (_heldTime >= _holdDuration)
                _isMet = true;
        }
        else
        {
            _heldTime = 0f;
        }

        // Debug feedback
        Debug.Log($"[FaceAndTilt] FaceAngle:{faceAngle:F1}° (need<{_faceTolerance}) | " +
                  $"Tilt:{measuredTilt:F1}° (need~{_targetTilt}°) | " +
                  $"Held:{_heldTime:F1}s");

        return _isMet;
    }
}
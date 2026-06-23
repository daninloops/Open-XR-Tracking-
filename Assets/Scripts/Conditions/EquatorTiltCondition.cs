using UnityEngine;

/// <summary>
/// Item4: Two checks must BOTH pass:
/// 1. The EquatorSeam ring's normal (its UP axis) faces the camera.
/// 2. The item is tilted ~45° upward (angle between item's up and world up = 45°).
/// Both must be true simultaneously on the same frame.
/// </summary>
public class EquatorTiltCondition : ICondition
{
    private readonly Transform _equatorSeam;   // the ring child object
    private readonly Transform _item;          // the sphere itself
    private readonly Transform _camera;
    private readonly float     _targetTilt;    // degrees (45)
    private readonly float     _tiltTolerance; // how close to 45° is acceptable
    private readonly float     _facingThreshold; // dot product threshold for facing check

    public EquatorTiltCondition(
    Transform equatorSeam,
    Transform item,
    Transform camera,
    float targetTilt       = 45f,
    float tiltTolerance    = 20f,    // wider tolerance
    float facingThreshold  = 0.7f)   // less strict facing
    {
        _equatorSeam      = equatorSeam;
        _item             = item;
        _camera           = camera;
        _targetTilt       = targetTilt;
        _tiltTolerance    = tiltTolerance;
        _facingThreshold  = facingThreshold;
    }

    public void OnStepBegin() { }
    public void OnStepEnd()   { }

    public bool Check()
{
    if (_equatorSeam == null || _item == null || _camera == null) return false;

    Vector3 ringNormal = _equatorSeam.up;
    Vector3 toCamera   = (_camera.position - _item.position).normalized;
    float   facingDot  = Vector3.Dot(ringNormal, toCamera);
    bool    facesPassed = facingDot >= _facingThreshold;

    float tiltAngle  = Vector3.Angle(_item.up, Vector3.up);
    bool  tiltPassed = Mathf.Abs(tiltAngle - _targetTilt) <= _tiltTolerance;

    // ADD THIS — shows live values in Console every 60 frames
    if (Time.frameCount % 60 == 0)
        Debug.Log($"[EquatorTilt] facing={facingDot:F2} (need {_facingThreshold}), tilt={tiltAngle:F1}° (need {_targetTilt}±{_tiltTolerance})  faces={facesPassed} tilt={tiltPassed}");

    return facesPassed && tiltPassed;
}
}

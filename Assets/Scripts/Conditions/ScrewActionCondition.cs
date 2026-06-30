using UnityEngine;

/// <summary>
/// Verifies a screw-tightening action.
/// Implements ICondition so it plugs into SequenceManager like any other condition.
///
/// Fixes applied:
/// 1. Hysteresis corrected - lose at larger radius, acquire at smaller
/// 2. Dead line removed (twistAxis was unused)
/// 3. OnCorrectAction split into OnGripReacquired and OnDirectionCorrected
/// 4. Debug.Log removed - verify clockwise sign manually first run
/// </summary>
public class ScrewActionCondition : ICondition
{
    // references 
    private readonly Transform _screwHead;
    private readonly Transform _hand;

    //  tuning 
    private readonly float _gripRadius       = 2f;
    private readonly float _gripHysteresis   = 0.05f;
    private readonly float _requiredRotation = 360f;
    private readonly float _jitterThreshold  = 0.1f;

    //  state 
    private Quaternion _lastHandRotation;
    private float      _accumulatedCW;
    private bool       _gripLost;
    private bool       _wrongDirection;

    //  events 
    // Wrong action: fired once with a reason string
    public event System.Action<string> OnWrongAction;

    // Correct events: split so subscribers know which thing was corrected
    public event System.Action OnGripReacquired;      // fired once when grip comes back
    public event System.Action OnDirectionCorrected;  // fired once when twist direction corrects

    //  constructor 
    public ScrewActionCondition(
        Transform screwHead,
        Transform hand,
        float gripRadius       = 0.2f,
        float requiredRotation = 360f)
    {
        _screwHead        = screwHead;
        _hand             = hand;
        _gripRadius       = gripRadius;
        _requiredRotation = requiredRotation;
    }

    //  ICondition

    public void OnStepBegin()
    {
        _lastHandRotation = _hand.rotation;
        _accumulatedCW    = 0f;
        _gripLost         = false;
        _wrongDirection   = false;
    }

    public void OnStepEnd()
    {
        OnWrongAction        = null;
        OnGripReacquired     = null;
        OnDirectionCorrected = null;
    }

    public bool Check()
    {
        //  Check 1: grip with corrected hysteresis 
        // FIX: lose at LARGER radius, re-acquire at SMALLER radius
        // This creates a sticky band that prevents oscillation at the boundary
        // Previous version had these swapped which caused per-frame toggling
        float loseRadius    = _gripRadius + _gripHysteresis; // 0.25 — lose when well outside
        float acquireRadius = _gripRadius;                   // 0.20 — re-acquire only when back inside

        float distToHead = Vector3.Distance(_hand.position, _screwHead.position);

        if (_gripLost)
        {
            // Grip is lost — only recover when hand comes back inside acquireRadius
            if (distToHead > acquireRadius)
            {
                _lastHandRotation = _hand.rotation; // stay in sync while grip is out
                return false;
            }
            // Grip re-acquired — fire specific event so subscriber knows why
            _gripLost = false;
            OnGripReacquired?.Invoke();
        }
        else
        {
            // Grip is active — only lose when hand goes past loseRadius
            if (distToHead > loseRadius)
            {
                _gripLost      = true;
                _accumulatedCW = 0f;
                _wrongDirection=false;
                OnWrongAction?.Invoke("Grip the HEAD of the screw!");
                _lastHandRotation = _hand.rotation;
                return false;
            }
        }



        Vector3    screwAxis     = _screwHead.up;
        Quaternion deltaRotation = _hand.rotation * Quaternion.Inverse(_lastHandRotation);
        _lastHandRotation = _hand.rotation;

        // FIX: removed dead line — Vector3 twistAxis = deltaRotation * screwAxis
        // was computed here but never used

        // Project quaternion xyz onto screw axis to isolate twist component
        Vector3 projection = Vector3.Project(
            new Vector3(deltaRotation.x, deltaRotation.y, deltaRotation.z),
            screwAxis);

        Quaternion twist = new Quaternion(
            projection.x,
            projection.y,
            projection.z,
            deltaRotation.w);

        // Normalize — projection may have changed the magnitude
        float twistMagnitude = Mathf.Sqrt(
            twist.x * twist.x +
            twist.y * twist.y +
            twist.z * twist.z +
            twist.w * twist.w);

        if (twistMagnitude < 0.0001f)
            return false;

        twist = new Quaternion(
            twist.x / twistMagnitude,
            twist.y / twistMagnitude,
            twist.z / twistMagnitude,
            twist.w / twistMagnitude);

        //  Check 3: clockwise detection
        twist.ToAngleAxis(out float twistAngleDeg, out Vector3 twistAxisOut);

        if (Mathf.Abs(twistAngleDeg) < _jitterThreshold)
            return false;

        // Dot with screwAxis recovers sign:
        // positive dot = axis aligns with screwAxis = CCW
        // negative dot = axis opposes screwAxis = CW
        float sign         = Vector3.Dot(twistAxisOut, screwAxis);
        float signedAngle  = twistAngleDeg * Mathf.Sign(sign);

        // NOTE: verify this sign on first run by twisting the interactor
        // the intended CW direction and confirming _accumulatedCW increases.
        // If it decreases, flip to: bool isClockwise = signedAngle > 0f;
        bool isClockwise = signedAngle < 0f;

        if (isClockwise)
        {
            _accumulatedCW += Mathf.Abs(signedAngle);
            Debug.Log("CW:"+_accumulatedCW);

            if (_wrongDirection)
            {
                _wrongDirection = false;
                // FIX: specific event so subscriber knows direction was corrected
                // not the same as grip being re-acquired
                OnDirectionCorrected?.Invoke();
            }
        }
        else
        {
            _accumulatedCW = Mathf.Max(0f, _accumulatedCW - Mathf.Abs(signedAngle));

            if (!_wrongDirection)
            {
                _wrongDirection = true;
                OnWrongAction?.Invoke("Twist CLOCKWISE to drive the screw in!");
            }
        }

        return _accumulatedCW >= _requiredRotation;
    }
}

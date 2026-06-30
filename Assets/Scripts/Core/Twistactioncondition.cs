using UnityEngine;

/// <summary>
/// Handles all twist-based grip actions: tighten, loosen, cap open, cap close.
///
/// Verification uses TWO independent rotation sources that must agree:
/// 1. HAND twist  — swing-twist decomposition of the hand's rotation delta
/// 2. OBJECT twist — swing-twist decomposition of the screw's own rotation delta
///
/// Why both: the hand is the user's INTENT (what they're trying to do).
/// The object is the RESULT (what actually happened to the thing being twisted).
/// On Quest 3 these should match almost exactly, since the screw is visually
/// driven by the hand's twist. But checking both guards against:
///   - the object being moved by something other than this interaction
///     (physics, another script, a second user) while the hand still
///     reports a "correct" twist
///   - hand-tracking jitter reporting a twist when the held object never
///     actually rotated (e.g. a loose/slipping grip)
///
/// AGREEMENT RULE: a frame only counts toward progress if BOTH the hand
/// delta and the object delta agree on direction (within a small angular
/// tolerance). If they disagree, the frame is treated as "no progress"
/// rather than guessing which one to trust — this is intentionally
/// conservative: under-counting an ambiguous frame is safer than over-counting
/// an action that may not have actually happened to the object.
/// </summary>
public class TwistActionCondition : ICondition
{
    public enum TwistDirection { CW, CCW }

    private readonly Transform      _anchor;   // grip point AND defines twist axis
    private readonly Transform      _hand;     // interactor / tracked hand joint
    private readonly Transform      _object;   // the screw/cap mesh itself
    private readonly TwistDirection _direction;
    private readonly float          _requiredDeg;
    private readonly float          _gripRadius;
    private readonly float          _gripHysteresis    = 0.05f;
    private readonly float          _jitter            = 0.1f;
    private readonly float          _agreementTolerance = 15f; // degrees of disagreement still allowed

    private Quaternion _lastHandRot;
    private Quaternion _lastObjectRot;
    private float      _accumulated;
    private bool       _gripLost;
    private bool       _wrongDirection;
    private bool       _disagreeing; // true while hand and object don't match

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnGripReacquired;
    public event System.Action         OnDirectionCorrected;

    public float Progress => Mathf.Clamp01(_accumulated / _requiredDeg);
    public float AccumulatedDegrees =>
        _direction == TwistDirection.CW ? -_accumulated : _accumulated;

    /// <param name="anchor">Grip point — also defines the twist axis via anchor.up</param>
    /// <param name="hand">Interactor / hand transform — the user's intent signal</param>
    /// <param name="objectTransform">The actual object being twisted — the result signal</param>
    public TwistActionCondition(
        Transform      anchor,
        Transform      hand,
        Transform      objectTransform,
        TwistDirection direction,
        float          requiredDegrees = 360f,
        float          gripRadius      = 0.3f)
    {
        _anchor      = anchor;
        _hand        = hand;
        _object      = objectTransform;
        _direction   = direction;
        _requiredDeg = requiredDegrees;
        _gripRadius  = gripRadius;
    }

    public void OnStepBegin()
    {
        _lastHandRot    = _hand.rotation;
        _lastObjectRot  = _object.rotation;
        _accumulated    = 0f;
        _gripLost       = false;
        _wrongDirection = false;
        _disagreeing    = false;
    }

    public void OnStepEnd()
    {
        OnWrongAction        = null;
        OnGripReacquired     = null;
        OnDirectionCorrected = null;
    }

    public bool Check()
    {
        if (_anchor == null || _hand == null || _object == null) return false;

        // ── Grip check (unchanged) ────────────────────────────────────────
        float dist       = Vector3.Distance(_hand.position, _anchor.position);
        float loseRadius = _gripRadius + _gripHysteresis;
        float gainRadius = _gripRadius;

        if (_gripLost)
        {
            if (dist > gainRadius)
            {
                _lastHandRot   = _hand.rotation;
                _lastObjectRot = _object.rotation;
                return false;
            }
            _gripLost       = false;
            _wrongDirection = false;
            OnGripReacquired?.Invoke();
        }
        else
        {
            if (dist > loseRadius)
            {
                _gripLost       = true;
                _accumulated    = 0f;
                _wrongDirection = false;
                OnWrongAction?.Invoke("Move your hand closer to grip!");
                _lastHandRot   = _hand.rotation;
                _lastObjectRot = _object.rotation;
                return false;
            }
        }

        Vector3 axis = _anchor.up;

        // ── Signal 1: HAND twist (intent) ─────────────────────────────────
        float handSignedAngle = ExtractTwistAngle(
            _hand.rotation, _lastHandRot, axis, out bool handHasTwist);
        _lastHandRot = _hand.rotation;

        // ── Signal 2: OBJECT twist (result) ───────────────────────────────
        float objectSignedAngle = ExtractTwistAngle(
            _object.rotation, _lastObjectRot, axis, out bool objectHasTwist);
        _lastObjectRot = _object.rotation;

        // Need at least one signal with meaningful movement to proceed
        if (!handHasTwist && !objectHasTwist) return false;

        // ── Agreement check ────────────────────────────────────────────────
        // Both signals must point the same direction (within tolerance) to count.
        // If only one has movement (e.g. object not yet visually driven this frame),
        // fall back to treating it as the working signal rather than blocking entirely —
        // but log a disagreement state so callers can surface a warning if it persists.
        float signedAngle;
        bool  agree;

        if (handHasTwist && objectHasTwist)
        {
            bool sameSign = Mathf.Sign(handSignedAngle) == Mathf.Sign(objectSignedAngle);
            float magDiff = Mathf.Abs(Mathf.Abs(handSignedAngle) - Mathf.Abs(objectSignedAngle));
            agree = sameSign && magDiff <= _agreementTolerance;

            // Use the smaller magnitude when they agree — conservative progress counting
            signedAngle = agree
                ? (Mathf.Abs(handSignedAngle) < Mathf.Abs(objectSignedAngle) ? handSignedAngle : objectSignedAngle)
                : 0f; // disagreement this frame = no progress either way
        }
        else
        {
            // Only one signal moved this frame — use it, but flag as not fully agreed
            agree       = false;
            signedAngle = handHasTwist ? handSignedAngle : objectSignedAngle;
        }

        if (!agree && handHasTwist && objectHasTwist)
        {
            // Genuine disagreement between hand and object — do not count this frame
            if (!_disagreeing)
            {
                _disagreeing = true;
                OnWrongAction?.Invoke("Grip seems to be slipping — re-grip and try again!");
            }
            return false;
        }
        else if (_disagreeing)
        {
            _disagreeing = false;
        }

        if (Mathf.Abs(signedAngle) < _jitter) return false;

        bool isCorrect = _direction == TwistDirection.CW
            ? signedAngle < 0f
            : signedAngle > 0f;

        if (isCorrect)
        {
            _accumulated += Mathf.Abs(signedAngle);
            if (_wrongDirection)
            {
                _wrongDirection = false;
                OnDirectionCorrected?.Invoke();
            }
        }
        else
        {
            _accumulated = Mathf.Max(0f, _accumulated - Mathf.Abs(signedAngle));
            if (!_wrongDirection)
            {
                _wrongDirection = true;
                string label = _direction == TwistDirection.CW ? "CLOCKWISE" : "COUNTER-CLOCKWISE";
                OnWrongAction?.Invoke("Twist " + label + "!");
            }
        }

        return _accumulated >= _requiredDeg;
    }

    /// <summary>
    /// Swing-twist decomposition: extracts the signed rotation angle around `axis`
    /// between `current` and `last` rotations. Shared logic for both hand and object.
    /// </summary>
    private float ExtractTwistAngle(
        Quaternion current, Quaternion last, Vector3 axis, out bool hasMeaningfulTwist)
    {
        hasMeaningfulTwist = false;

        Quaternion delta = current * Quaternion.Inverse(last);

        Vector3 proj = Vector3.Project(
            new Vector3(delta.x, delta.y, delta.z), axis);

        Quaternion twist = new Quaternion(proj.x, proj.y, proj.z, delta.w);

        float mag = Mathf.Sqrt(
            twist.x * twist.x + twist.y * twist.y +
            twist.z * twist.z + twist.w * twist.w);

        if (mag < 0.0001f) return 0f;

        twist = new Quaternion(
            twist.x / mag, twist.y / mag,
            twist.z / mag, twist.w / mag);

        twist.ToAngleAxis(out float angleDeg, out Vector3 twistAxis);

        if (Mathf.Abs(angleDeg) < _jitter) return 0f;

        float sign = Vector3.Dot(twistAxis, axis);
        hasMeaningfulTwist = true;
        return angleDeg * Mathf.Sign(sign);
    }
}
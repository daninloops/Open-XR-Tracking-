using UnityEngine;

/// <summary>
/// Condition: "slot insertion"
/// Two checks must pass simultaneously:
/// 1. Object is correctly oriented (forward axis aligns with slot's forward axis)
/// 2. Object is moved forward into the slot (close enough to slot target position)
///
/// This simulates inserting a key into a lock, a plug into a socket, etc.
/// The slot is represented by a child empty GameObject on the target named "SlotTarget".
/// </summary>
public class SlotInsertCondition : ICondition
{
    private readonly Transform _object;          // the thing being inserted
    private readonly Transform _slotTarget;      // where it should end up
    private readonly Transform _interactor;      // hand driving the object
    private readonly float     _positionRadius;  // how close counts as inserted
    private readonly float     _alignThreshold;  // dot product for correct orientation

    private bool _wrongOrientation;
    private bool _wrongPosition;

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;

    public SlotInsertCondition(
        Transform object_,
        Transform slotTarget,
        Transform interactor,
        float     positionRadius  = 0.15f,
        float     alignThreshold  = 0.95f)
    {
        _object          = object_;
        _slotTarget      = slotTarget;
        _interactor      = interactor;
        _positionRadius  = positionRadius;
        _alignThreshold  = alignThreshold;
    }

    public void OnStepBegin()
    {
        _wrongOrientation = false;
        _wrongPosition    = false;
    }

    public void OnStepEnd()
    {
        OnWrongAction  = null;
        OnCorrectAction = null;
    }

    public bool Check()
    {
        if (_object == null || _slotTarget == null) return false;

        // Check 1: orientation — object's forward must align with slot's forward
        float alignDot    = Vector3.Dot(_object.forward, _slotTarget.forward);
        bool  correctAlign = alignDot >= _alignThreshold;

        if (!correctAlign)
        {
            if (!_wrongOrientation)
            {
                _wrongOrientation = true;
                OnWrongAction?.Invoke("Align the object with the slot before inserting!");
            }
            return false;
        }
        else if (_wrongOrientation)
        {
            _wrongOrientation = false;
            OnCorrectAction?.Invoke();
        }

        // Check 2: position — object must be close to the slot target position
        float dist         = Vector3.Distance(_object.position, _slotTarget.position);
        bool  correctPos   = dist <= _positionRadius;

        if (!correctPos)
        {
            if (!_wrongPosition)
            {
                _wrongPosition = true;
                OnWrongAction?.Invoke("Now slide the object into the slot!");
            }
            return false;
        }
        else if (_wrongPosition)
        {
            _wrongPosition = false;
            OnCorrectAction?.Invoke();
        }

        return true;
    }
}
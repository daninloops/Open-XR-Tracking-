using UnityEngine;

/// <summary>
/// FIXED: checks INTERACTOR position/orientation vs SlotTarget anchor.
/// Inspector changes to the target object no longer trigger this.
/// Check 1: interactor forward aligns with slot forward (Q/E to rotate interactor)
/// Check 2: interactor is within insertRadius of SlotTarget (WASD to move)
/// </summary>
public class SlotInsertCondition : ICondition
{
    private readonly Transform _slotTarget;
    private readonly Transform _interactor;
    private readonly float     _insertRadius;
    private readonly float     _alignThreshold;

    private bool _wrongAlign;
    private bool _wrongPosition;

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;

    public SlotInsertCondition(
        Transform slotTarget,
        Transform interactor,
        float     insertRadius   = 0.3f,
        float     alignThreshold = 0.85f)
    {
        _slotTarget     = slotTarget;
        _interactor     = interactor;
        _insertRadius   = insertRadius;
        _alignThreshold = alignThreshold;
    }

    public void OnStepBegin() { _wrongAlign = false; _wrongPosition = false; }
    public void OnStepEnd()   { OnWrongAction = null; OnCorrectAction = null; }

    public bool Check()
    {
        if (_slotTarget == null || _interactor == null) return false;

        // Check 1: interactor approaching from correct direction
        float alignDot = Vector3.Dot(_interactor.forward, _slotTarget.forward);
        bool  alignOk  = alignDot >= _alignThreshold;

        if (!alignOk)
        {
            if (!_wrongAlign)
            {
                _wrongAlign = true;
                OnWrongAction?.Invoke("Align your hand with the slot direction  [Q/E to rotate interactor]");
            }
            return false;
        }
        else if (_wrongAlign) { _wrongAlign = false; OnCorrectAction?.Invoke(); }

        // Check 2: interactor close to slot position
        float dist  = Vector3.Distance(_interactor.position, _slotTarget.position);
        bool  posOk = dist <= _insertRadius;

        if (!posOk)
        {
            if (!_wrongPosition)
            {
                _wrongPosition = true;
                OnWrongAction?.Invoke("Slide your hand into the slot  [WASD to move]");
            }
            return false;
        }
        else if (_wrongPosition) { _wrongPosition = false; OnCorrectAction?.Invoke(); }

        return true;
    }
}
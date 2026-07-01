using UnityEngine;

/// <summary>
/// Condition: "shelf placement"
/// Met when the object is positioned within a 3D box region (the shelf zone)
/// AND the object is upright (label facing up).
/// 
/// The shelf zone is defined by a child empty GameObject on the target 
/// named "ShelfZone". The object must be within `zoneRadius` of that point
/// in all three axes — think of it as a 3D proximity check with an
/// additional orientation requirement.
/// </summary>
public class ShelfPlacementCondition : ICondition
{
    private readonly Transform _object;
    private readonly Transform _shelfZone;   // target position on the shelf
    private readonly float     _zoneRadius;
    private readonly float     _uprightThreshold;

    private bool _wrongPosition;
    private bool _wrongOrientation;

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;

    public ShelfPlacementCondition(
        Transform object_,
        Transform shelfZone,
        float     zoneRadius       = 0.2f,
        float     uprightThreshold = 0.9f)
    {
        _object           = object_;
        _shelfZone        = shelfZone;
        _zoneRadius       = zoneRadius;
        _uprightThreshold = uprightThreshold;
    }

    public void OnStepBegin()
    {
        _wrongPosition    = false;
        _wrongOrientation = false;
    }

    public void OnStepEnd()
    {
        OnWrongAction  = null;
        OnCorrectAction = null;
    }

    public bool Check()
    {
        if (_object == null || _shelfZone == null) return false;

        // Check 1: is the object in the right place on the shelf?
        float dist       = Vector3.Distance(_object.position, _shelfZone.position);
        bool  inZone     = dist <= _zoneRadius;

        if (!inZone)
        {
            if (!_wrongPosition)
            {
                _wrongPosition = true;
                OnWrongAction?.Invoke("Place the object on the shelf!");
            }
            return false;
        }
        else if (_wrongPosition)
        {
            _wrongPosition = false;
            OnCorrectAction?.Invoke();
        }

        // Check 2: is the object upright (not lying sideways on the shelf)?
        float uprightDot = Vector3.Dot(_object.up, Vector3.up);
        bool  upright    = uprightDot >= _uprightThreshold;

        if (!upright)
        {
            if (!_wrongOrientation)
            {
                _wrongOrientation = true;
                OnWrongAction?.Invoke("Stand the object upright on the shelf!");
            }
            return false;
        }
        else if (_wrongOrientation)
        {
            _wrongOrientation = false;
            OnCorrectAction?.Invoke();
        }

        return true;
    }
}
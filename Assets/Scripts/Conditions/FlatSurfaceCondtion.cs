using UnityEngine;

/// <summary>
/// Condition: "flat surface align"
/// Met when the object's DOWN axis aligns with world DOWN (Vector3.down).
/// Simulates placing an object flat — its base parallel to the floor.
/// 
/// Slightly different from LabelUp:
/// LabelUp checks object.up vs world up (upright object)
/// FlatSurface checks object.down vs world down (base facing floor)
/// These are the same math but semantically different — a flat object 
/// lying on its side would pass FlatSurface but not LabelUp.
/// </summary>
public class FlatSurfaceCondition : ICondition
{
    private readonly Transform _target;
    private readonly float     _threshold;

    private bool _wrongOrientation;

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;

    public FlatSurfaceCondition(Transform target, float threshold = 0.95f)
    {
        _target    = target;
        _threshold = threshold;
    }

    public void OnStepBegin()
    {
        _wrongOrientation=false;
    }

    public void OnStepEnd()
    {
        OnWrongAction  = null;
        OnCorrectAction = null;
    }

    public bool Check()
    {
        if (_target == null) return false;

        // object.down should align with world down = base is facing the floor
        float dot    = Vector3.Dot(-_target.up, Vector3.down);
        bool  correct = dot >= _threshold;

        if (!correct)
        {
            if (!_wrongOrientation)
            {
                _wrongOrientation = true;
                OnWrongAction?.Invoke("Tilt " + _target.name + " so its base faces flat down!");
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
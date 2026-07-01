using UnityEngine;
 
/// <summary>
/// Condition: "label up"
/// Met when the object's UP axis aligns with world UP (Vector3.up).
/// Simulates orienting a bottle/jar/box so the label faces the ceiling.
/// 
/// Uses dot product between object.up and Vector3.up.
/// dot = 1.0 means perfectly upright, 0 = sideways, -1 = upside down.
/// threshold of 0.95 = within ~18 degrees of perfectly upright.
/// </summary>
public class LabelUpCondition : ICondition
{
    private readonly Transform _target;
    private readonly float     _threshold;
 
    private bool _wrongOrientation;
 
    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;
 
    public LabelUpCondition(Transform target, float threshold = 0.95f)
    {
        _target    = target;
        _threshold = threshold;
    }
 
    public void OnStepBegin()
    {
        _wrongOrientation = false;
    }
 
    public void OnStepEnd()
    {
        OnWrongAction  = null;
        OnCorrectAction = null;
    }
 
    public bool Check()
    {
        if (_target == null) return false;
 
        // How closely does the object's up axis align with world up?
        float dot = Vector3.Dot(_target.up, Vector3.up);
        bool  correct = dot >= _threshold;
 
        if (!correct)
        {
            if (!_wrongOrientation)
            {
                _wrongOrientation = true;
                OnWrongAction?.Invoke("Rotate " + _target.name + " so the label faces upward!");
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
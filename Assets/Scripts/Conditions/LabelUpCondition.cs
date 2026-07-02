using UnityEngine;

public class LabelUpCondition : ICondition
{
    private readonly Transform _target;
    private readonly float     _threshold;
    private bool               _wrongOrientation;

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;

    public LabelUpCondition(Transform target, float threshold = 0.95f)
    {
        _target    = target;
        _threshold = threshold;
    }

    public void OnStepBegin()  => _wrongOrientation = false;
    public void OnStepEnd()    { OnWrongAction = null; OnCorrectAction = null; }

    public bool Check()
    {
        if (_target == null) return false;
        float dot     = Vector3.Dot(_target.up, Vector3.up);
        bool  correct = dot >= _threshold;
        if (!correct)
        {
            if (!_wrongOrientation)
            {
                _wrongOrientation = true;
                OnWrongAction?.Invoke("Rotate " + _target.name + " so the label faces UP  [Q/E/Z/X to rotate]");
            }
            return false;
        }
        if (_wrongOrientation) { _wrongOrientation = false; OnCorrectAction?.Invoke(); }
        return true;
    }
}
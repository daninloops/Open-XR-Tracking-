using UnityEngine;

public class FlatSurfaceCondition : ICondition
{
    private readonly Transform _target;
    private readonly float     _threshold;
    private bool               _wrongOrientation;

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;

    public FlatSurfaceCondition(Transform target, float threshold = 0.95f)
    {
        _target    = target;
        _threshold = threshold;
    }

    public void OnStepBegin()  => _wrongOrientation = false;
    public void OnStepEnd()    { OnWrongAction = null; OnCorrectAction = null; }

    public bool Check()
    {
        if (_target == null) return false;
        float dot     = Vector3.Dot(-_target.up, Vector3.down);
        bool  correct = dot >= _threshold;
        if (!correct)
        {
            if (!_wrongOrientation)
            {
                _wrongOrientation = true;
                OnWrongAction?.Invoke("Tilt " + _target.name + " flat — base facing down  [Q/E/Z/X]");
            }
            return false;
        }
        if (_wrongOrientation) { _wrongOrientation = false; OnCorrectAction?.Invoke(); }
        return true;
    }
}
using UnityEngine;
using UnityEngine;

/// <summary>
/// Met the frame the interactor comes within 'radius' of the anchor.
/// Per-frame cost: one Vector3.Distance call.
/// </summary>
public class ProximityCondition : ICondition
{
    private readonly Transform       _anchor;
    private readonly IInteractorInput _input;
    private readonly float            _radius;

    public ProximityCondition(Transform anchor, IInteractorInput input, float radius = 0.6f)
    {
        _anchor = anchor;
        _input  = input;
        _radius = radius;
    }

    public void OnStepBegin() { }
    public void OnStepEnd()   { }

    public bool Check()
    {
        if (_anchor == null) return false;
        return Vector3.Distance(_input.InteractorPosition, _anchor.position) <= _radius;
    }
}
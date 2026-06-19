using UnityEngine;

public class ProximityCondition : ICondition
{
    private readonly Transform        _anchor;
    private readonly IInteractorInput _input;
    private readonly float            _radius;

    public ProximityCondition(Transform anchor, IInteractorInput input, float radius = 3.0f)
    {
        _anchor = anchor;
        _input  = input;
        _radius = radius;
    }

    public void OnStepBegin() { }
    public void OnStepEnd()   { }

    public bool Check()
    {
        if (_anchor == null)
        {
            Debug.LogError("ProximityCondition: anchor is NULL");
            return false;
        }

        float dist = Vector3.Distance(_input.InteractorPosition, _anchor.position);
        Debug.Log($"Interactor: {_input.InteractorPosition}  |  Anchor: {_anchor.position}  |  Dist: {dist}");
        return dist <= _radius;
    }
}
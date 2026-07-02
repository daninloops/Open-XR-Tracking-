using UnityEngine;

/// <summary>
/// FIXED: checks INTERACTOR position vs ShelfZone anchor.
/// Inspector changes to the target object no longer trigger this.
/// Move interactor to the ShelfZone position with WASD.
/// </summary>
public class ShelfPlacementCondition : ICondition
{
    private readonly Transform _shelfZone;
    private readonly Transform _interactor;
    private readonly float     _zoneRadius;

    private bool _wrongPosition;

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;

    public ShelfPlacementCondition(
        Transform shelfZone,
        Transform interactor,
        float     zoneRadius = 0.3f)
    {
        _shelfZone  = shelfZone;
        _interactor = interactor;
        _zoneRadius = zoneRadius;
    }

    public void OnStepBegin()  => _wrongPosition = false;
    public void OnStepEnd()    { OnWrongAction = null; OnCorrectAction = null; }

    public bool Check()
    {
        if (_shelfZone == null || _interactor == null) return false;

        float dist   = Vector3.Distance(_interactor.position, _shelfZone.position);
        bool  inZone = dist <= _zoneRadius;

        if (!inZone)
        {
            if (!_wrongPosition)
            {
                _wrongPosition = true;
                OnWrongAction?.Invoke("Move to the shelf zone to place the object  [WASD]");
            }
            return false;
        }
        if (_wrongPosition) { _wrongPosition = false; OnCorrectAction?.Invoke(); }
        return true;
    }
}
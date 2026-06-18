using UnityEngine;

/// <summary>
/// Always-on per-frame check.
/// If the interactor gets close to ANY shape that is NOT the current target,
/// that shape flashes red immediately, independent of step progress.
/// Add this component to the SequenceManager GameObject.
/// </summary>
public class GuardMonitor : MonoBehaviour
{
    [Tooltip("Distance that counts as 'engaging' a wrong shape")]
    public float guardRadius = 0.6f;

    private ITargetProvider  _provider;
    private IInteractorInput _input;
    private ShapeTarget      _currentTarget;

    public void Init(ITargetProvider provider, IInteractorInput input)
    {
        _provider = provider;
        _input    = input;
    }

    public void SetCurrentTarget(ShapeTarget t) => _currentTarget = t;

    void Update()
    {
        if (_provider == null || _input == null) return;

        Vector3 pos = _input.InteractorPosition;
        foreach (var t in _provider.GetAllTargets())
        {
            if (t == _currentTarget || t.anchor == null) continue;
            if (Vector3.Distance(pos, t.anchor.position) <= guardRadius)
                t.FlashError();
        }
    }
}

using UnityEngine;

/// <summary>
/// Runs the active ICondition every Update.
/// Fires OnConditionMet when it returns true, then stops itself.
/// Add this component to the SequenceManager GameObject.
/// </summary>
public class ConditionMonitor : MonoBehaviour
{
    public event System.Action OnConditionMet;

    private ICondition _condition;
    private bool       _running;

    public void StartMonitoring(ICondition condition)
    {
        _condition = condition;
        _running   = true;
        _condition.OnStepBegin();
    }

    public void StopMonitoring()
    {
        _condition?.OnStepEnd();
        _running   = false;
        _condition = null;
    }

    void Update()
    {
        if (!_running || _condition == null) return;
        if (_condition.Check())
        {
            _running = false;           // stop before firing so re-entry is safe
            OnConditionMet?.Invoke();
        }
    }
}

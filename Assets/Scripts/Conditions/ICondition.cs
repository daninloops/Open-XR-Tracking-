/// <summary>
/// Implemented by ProximityCondition, RotateCondition, ConfirmCondition.
/// ConditionMonitor calls Check() every frame.
/// </summary>
public interface ICondition
{
    bool Check();        // called every Update – keep cheap
    void OnStepBegin();  // called once when step activates
    void OnStepEnd();    // called once when step ends
}

/// <summary>
/// Met on the frame the confirm button is pressed.
/// This is the ONLY condition that is NOT a per-frame geometry check.
/// </summary>
public class ConfirmCondition : ICondition
{
    private readonly IInteractorInput _input;
    private bool _met;

    public ConfirmCondition(IInteractorInput input) => _input = input;

    public void OnStepBegin() => _met = false;
    public void OnStepEnd()   => _met = false;

    public bool Check()
    {
        if (!_met && _input.ConfirmPressed) _met = true;
        return _met;
    }
}

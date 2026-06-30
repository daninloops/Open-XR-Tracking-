using UnityEngine;

public class GripHoldCondition : ICondition
{
    private readonly Transform _anchor;
    private readonly IInteractorInput _input;

    private readonly float _radius;
    private readonly float _holdTime;

    private float _timer;

    private bool _wrongState = false;

    public event System.Action<string> OnWrongAction;
    public event System.Action<float> OnHolding;
    public event System.Action OnCorrectAction;

    public GripHoldCondition(
        Transform anchor,
        IInteractorInput input,
        float radius = 0.5f,
        float holdTime = 1f)
    {
        _anchor = anchor;
        _input = input;
        _radius = radius;
        _holdTime = holdTime;
    }

    public void OnStepBegin()
    {
        _timer = 0f;
        _wrongState = false;
    }

    public void OnStepEnd()
    {
        OnWrongAction = null;
        OnHolding = null;
        OnCorrectAction = null;
    }

    public bool Check()
    {
        if (_anchor == null)
            return false;

        float distance = Vector3.Distance(
            _input.InteractorPosition,
            _anchor.position);
Debug.Log("Anchor Name: " + _anchor.name);
Debug.Log("Anchor Position: " + _anchor.position);
Debug.Log("Interactor Position: " + _input.InteractorPosition);
Debug.Log("Distance: " + distance);

        // ---------- Too far ----------
        if (distance > _radius)
        {
            _timer = 0f;

            if (!_wrongState)
            {
                _wrongState = true;
                OnWrongAction?.Invoke("Move closer to grip.");
            }

            return false;
        }

        // ---------- Not gripping ----------
        if (!_input.GripPressed)
        {
            _timer = 0f;

            if (!_wrongState)
            {
                _wrongState = true;
                OnWrongAction?.Invoke("Grip the object.");
            }

            return false;
        }

        // ---------- Correct state ----------
        if (_wrongState)
        {
            _wrongState = false;
            OnCorrectAction?.Invoke();
        }

        _timer += Time.deltaTime;

        float progress = Mathf.Clamp01(_timer / _holdTime);

        OnHolding?.Invoke(progress);

        if (_timer >= _holdTime)
        {
            Debug.Log("Grip Hold Complete!");
            return true;
        }

        return false;
    }
}
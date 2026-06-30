using UnityEngine;

/// <summary>
/// Handshake grip verification.
/// Checks:
/// 1. Hand approaches from the front of the target
/// 2. Hand is within grip radius
/// 3. Hand orientation is correct (palm facing target)
/// Editor: simulated with interactor position and forward direction
/// Quest 3: replace _hand.forward with real palm normal from hand tracking
/// </summary>
public class HandshakeCondition : ICondition
{
    private readonly Transform _target;
    private readonly Transform _hand;
    private readonly float     _approachRadius;
    private readonly float     _alignThreshold;

    private bool _wrongApproach;
    private bool _wrongOrientation;

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;

    public HandshakeCondition(
        Transform target,
        Transform hand,
        float     approachRadius  = 0.4f,
        float     alignThreshold  = 0.7f)
    {
        _target         = target;
        _hand           = hand;
        _approachRadius = approachRadius;
        _alignThreshold = alignThreshold;
    }

    public void OnStepBegin()
    {
        _wrongApproach     = false;
        _wrongOrientation  = false;
    }

    public void OnStepEnd()
    {
        OnWrongAction  = null;
        OnCorrectAction = null;
    }

    public bool Check()
    {
        if (_target == null || _hand == null) return false;

        // Check 1: hand must approach from the front
        Vector3 toHand      = (_hand.position - _target.position).normalized;
        float   approachDot = Vector3.Dot(toHand, _target.forward);
        bool    correctSide = approachDot > 0.3f;

        if (!correctSide)
        {
            if (!_wrongApproach)
            {
                _wrongApproach = true;
                OnWrongAction?.Invoke("Approach from the FRONT for a handshake!");
            }
            return false;
        }
        else if (_wrongApproach)
        {
            _wrongApproach = false;
            OnCorrectAction?.Invoke();
        }

        // Check 2: hand must be close enough
        float dist = Vector3.Distance(_hand.position, _target.position);
        if (dist > _approachRadius)
        {
            OnWrongAction?.Invoke("Move closer to complete the handshake!");
            return false;
        }

        // Check 3: hand orientation — palm facing target
        // Editor: checks interactor forward faces toward target
        // Quest 3: replace _hand.forward with palm normal vector
        float orientDot   = Vector3.Dot(_hand.forward, -toHand);
        bool  correctOrient = orientDot > _alignThreshold;

        if (!correctOrient)
        {
            if (!_wrongOrientation)
            {
                _wrongOrientation = true;
                OnWrongAction?.Invoke("Rotate your hand — palm should face the target!");
            }
            return false;
        }
        else if (_wrongOrientation)
        {
            _wrongOrientation = false;
            OnCorrectAction?.Invoke();
        }

        return true;
    }
}
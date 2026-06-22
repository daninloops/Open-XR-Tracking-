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

    [Header("Optional audio feedback")]
    public AudioManager audioManager;
    [Tooltip("Minimum seconds between guard error sounds, so it doesn't spam")]
    public float soundCooldown = 1f;

    [Tooltip("Grace period after a step completes before guard checks again")]
    public float stepCompletionGrace = 2f;

    private ITargetProvider  _provider;
    private IInteractorInput _input;
    private ShapeTarget      _currentTarget;
    private float            _lastSoundTime   = -999f;
    private float            _graceUntil      = -999f;

    public void Init(ITargetProvider provider, IInteractorInput input)
    {
        _provider = provider;
        _input    = input;
    }

    public void SetCurrentTarget(ShapeTarget t)
    {
        _currentTarget = t;
    }

    /// <summary>Call this before advancing to the next step to silence guard sounds.</summary>
    public void TriggerGrace()
    {
        _graceUntil    = Time.time + stepCompletionGrace;
        _lastSoundTime = Time.time + stepCompletionGrace; // also reset sound cooldown
        Debug.Log($"[GuardMonitor] Grace period started until {_graceUntil}");
    }

    void Update()
    {
        if (_provider == null || _input == null) return;

        // Don't check during grace period
        if (Time.time < _graceUntil) return;

        Vector3 pos = _input.InteractorPosition;
        foreach (var t in _provider.GetAllTargets())
        {
            if (t == _currentTarget || t.anchor == null) continue;
            if (Vector3.Distance(pos, t.anchor.position) <= guardRadius)
            {
                t.FlashError();

                if (audioManager != null && Time.time - _lastSoundTime >= soundCooldown)
                {
                    audioManager.PlayGuardError();
                    _lastSoundTime = Time.time;
                }
            }
        }
    }
}
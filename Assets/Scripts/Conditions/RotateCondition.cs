using UnityEngine;

/// <summary>
/// Met when the target has been rotated by 'targetDegrees' in the correct direction.
/// Rotating the wrong way fires CorrectionNeeded(true) on the same frame and
/// subtracts from the accumulated total (live correction).
/// Per-frame cost: one Mathf.DeltaAngle call.
/// </summary>
public class RotateCondition : ICondition
{
    public enum Direction { CW, CCW }

    /// <summary>true = correction active, false = correction cleared.</summary>
    public event System.Action<bool> CorrectionNeeded;

    private readonly Transform _target;
    private readonly Direction _dir;
    private readonly float     _targetDegrees;

    private float _accumulated;
    private float _lastY;
    private bool  _correctionActive;

    public RotateCondition(Transform target, Direction dir, float targetDegrees)
    {
        _target        = target;
        _dir           = dir;
        _targetDegrees = Mathf.Abs(targetDegrees);
    }

    public void OnStepBegin()
    {
        _accumulated      = 0f;
        _lastY            = _target.eulerAngles.y;
        _correctionActive = false;
    }

    public void OnStepEnd()
    {
        if (_correctionActive) CorrectionNeeded?.Invoke(false);
        _correctionActive = false;
    }

    public bool Check()
    {
        if (_target == null) return false;

        float currentY = _target.eulerAngles.y;
        float delta    = Mathf.DeltaAngle(_lastY, currentY);
        _lastY = currentY;
      


        if (Mathf.Abs(delta) < 0.01f) return false; // no movement

        // CCW → positive delta in Unity's left-hand Y axis
        bool correctDir = _dir == Direction.CCW ? delta < 0 : delta > 0;

        if (correctDir)
        {
            _accumulated += Mathf.Abs(delta);
            if (_correctionActive)
            {
                _correctionActive = false;
                CorrectionNeeded?.Invoke(false);
            }
        }
        else
        {
            // Wrong direction – live correct
            _accumulated = Mathf.Max(0f, _accumulated - Mathf.Abs(delta));
            if (!_correctionActive)
            {
                _correctionActive = true;
                CorrectionNeeded?.Invoke(true);
            }
        }

        return _accumulated >= _targetDegrees;
    }
}
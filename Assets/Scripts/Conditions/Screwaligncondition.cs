using UnityEngine;

/// <summary>
/// Condition: "screwalign"
/// The user must orient the screw head so the slot marker aligns with
/// a reference direction before twisting is allowed.
///
/// For a flathead screw: slot should be horizontal = SlotMarker.right aligns with Vector3.right
/// For a Phillips screw: slot arms should align with X and Z axes simultaneously
///
/// Uses dot product — same pattern as FaceCameraCondition and SeamVertical.
/// User rotates the screw with Q/E to spin it around its Y axis until
/// the slot snaps into the correct alignment.
///
/// Why this matters: in real life you must align the screwdriver with the
/// slot BEFORE you can drive the screw — checking this enforces correct technique.
/// </summary>
public class ScrewAlignCondition : ICondition
{
    public enum SlotType { Flathead, Phillips }

    private readonly Transform _slotMarker;    // child empty on screw head
    private readonly SlotType  _slotType;
    private readonly float     _threshold;     // dot product threshold (0.95 = ~18 degrees)

    private bool _wrongAlign;

    public event System.Action<string> OnWrongAction;
    public event System.Action         OnCorrectAction;

    public ScrewAlignCondition(
        Transform slotMarker,
        SlotType  slotType  = SlotType.Flathead,
        float     threshold = 0.95f)
    {
        _slotMarker = slotMarker;
        _slotType   = slotType;
        _threshold  = threshold;
    }

    public void OnStepBegin() => _wrongAlign = false;

    public void OnStepEnd()
    {
        OnWrongAction  = null;
        OnCorrectAction = null;
    }

    public bool Check()
    {
        if (_slotMarker == null) return false;

        bool aligned = false;

        if (_slotType == SlotType.Flathead)
        {
            // Flathead: the slot runs left-right
            // Check that SlotMarker.right aligns with world right OR world left
            // (Mathf.Abs because the slot looks the same flipped 180 degrees)
            float dot = Mathf.Abs(Vector3.Dot(_slotMarker.right, Vector3.right));
            aligned   = dot >= _threshold;
        }
        else
        {
            // Phillips: slot has two arms at 90 degrees to each other
            // Both arms must align with world axes simultaneously
            // Arm 1: SlotMarker.right aligns with world right
            // Arm 2: SlotMarker.forward aligns with world forward
            float dot1 = Mathf.Abs(Vector3.Dot(_slotMarker.right,   Vector3.right));
            float dot2 = Mathf.Abs(Vector3.Dot(_slotMarker.forward, Vector3.forward));
            aligned    = dot1 >= _threshold && dot2 >= _threshold;
        }

        if (!aligned)
        {
            if (!_wrongAlign)
            {
                _wrongAlign = true;
                OnWrongAction?.Invoke("Align the screw slot horizontally before twisting  [Q/E to rotate]");
            }
            return false;
        }

        if (_wrongAlign)
        {
            _wrongAlign = false;
            OnCorrectAction?.Invoke();
        }

        return true;
    }
}
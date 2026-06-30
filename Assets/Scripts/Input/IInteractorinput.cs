using UnityEngine;

/// <summary>
/// Swap EditorInteractorDriver (Editor) for XRInteractorAdapter (Quest 3)
/// by changing which MonoBehaviour you drag into SequenceManager.
/// </summary>
public interface IInteractorInput
{
    Vector3 InteractorPosition { get; }
    Vector3 InteractorVelocity{get;}
    bool    ConfirmPressed     { get; }
    bool GripPressed{get;}
}


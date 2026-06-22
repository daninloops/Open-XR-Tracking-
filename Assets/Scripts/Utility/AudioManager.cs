using UnityEngine;

/// <summary>
/// Plays short feedback sounds for step completion and guard errors.
/// Attach to the SequenceManager GameObject alongside an AudioSource component.
/// Assign two AudioClips in the Inspector — any short .wav/.mp3 works.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip stepCompleteClip;
    public AudioClip guardErrorClip;
    public AudioClip sequenceCompleteClip;

    private AudioSource _source;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void PlayStepComplete()
    {
        if (stepCompleteClip) _source.PlayOneShot(stepCompleteClip);
    }

    public void PlayGuardError()
    {
        if (guardErrorClip) _source.PlayOneShot(guardErrorClip);
    }

    public void PlaySequenceComplete()
    {
        if (sequenceCompleteClip) _source.PlayOneShot(sequenceCompleteClip);
    }
}
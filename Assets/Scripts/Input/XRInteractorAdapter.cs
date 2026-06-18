using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Quest 3 adapter.  Attach to the Right Controller GameObject inside XR Origin.
/// In the Inspector swap the SequenceManager's "Interactor Input Mono" to this.
/// </summary>
public class XRInteractorAdapter : MonoBehaviour, IInteractorInput
{
    public Vector3 InteractorPosition => transform.position;

    public bool ConfirmPressed
    {
        get
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller |
                InputDeviceCharacteristics.Right, devices);

            foreach (var d in devices)
                if (d.TryGetFeatureValue(CommonUsages.primaryButton, out bool v) && v)
                    return true;
            return false;
        }
    }
}
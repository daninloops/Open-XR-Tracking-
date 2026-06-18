using UnityEngine;

/// <summary>
/// Attach to CubeB to test the rotate condition in the Editor.
/// Left Arrow  = CCW (correct direction per the JSON)
/// Right Arrow = CW  (wrong direction → triggers correction cue)
/// Remove or disable before building for Quest.
/// </summary>
public class DebugRotator : MonoBehaviour
{
    public float speed = 60f; // degrees per second

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.Rotate(0,  speed * Time.deltaTime, 0); // CCW
        if (Input.GetKey(KeyCode.RightArrow))
            transform.Rotate(0, -speed * Time.deltaTime, 0); // CW  (wrong way)
    }
}
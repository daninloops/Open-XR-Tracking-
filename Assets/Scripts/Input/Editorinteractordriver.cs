using UnityEngine;

/// <summary>
/// Moves the stand-in interactor with WASD + Q/E.
/// Space = confirm.
/// Attach this to your Interactor GameObject in the scene.
/// </summary>
public class EditorInteractorDriver : MonoBehaviour, IInteractorInput
{
    [Header("Speed")]
    public float moveSpeed     = 2f;
    public float verticalSpeed = 1f;

    public Vector3 InteractorPosition => transform.position;
    public bool    ConfirmPressed     => Input.GetKeyDown(KeyCode.Space);

    void Update()
    {
        float h  = Input.GetAxis("Horizontal");          // A / D
        float v  = Input.GetAxis("Vertical");            // W / S
        float up = Input.GetKey(KeyCode.E) ?  1f :
                   Input.GetKey(KeyCode.Q) ? -1f : 0f;

        Vector3 move = new Vector3(h, up * verticalSpeed, v)
                       * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }
}

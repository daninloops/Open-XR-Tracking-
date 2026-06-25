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
    private Vector3 _lastPosition;//stores previous frame position 
    private Vector3 _velocity;//calculated each frame 

    public Vector3 InteractorPosition => transform.position;
    public bool    ConfirmPressed     => Input.GetKeyDown(KeyCode.Space);
    public Vecotr3 InteractorVelocity=>_velocity;

    void Update()
    {
        Vector3 currentPosition=transform.position;
        _velocity=(currentPosition-_lastPosition)/Time.deltaTime;
        _lastPosition=currentPosition;
    

    
        float h  = Input.GetAxis("Horizontal");          // A / D
        float v  = Input.GetAxis("Vertical");            // W / S
        float up = Input.GetKey(KeyCode.E) ?  1f :
                   Input.GetKey(KeyCode.Q) ? -1f : 0f;


        Vector3 move = new Vector3(h, up * verticalSpeed, v)
                       * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }

}
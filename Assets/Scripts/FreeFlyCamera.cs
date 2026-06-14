//FreeFlyCamer.cs
//Attach to Feedcamera. lets you fly around the scene in play mode 
using UnityEngine;
public class FreeFlyCamera : MonoBehaviour
{
    [SerializeField] private float moveSpeed =3f;
    [SerializeField] private float lookSpeed=2f; 

    private float yaw= 0f;// leftright rotation
    private float pitch =0f;// up down rotation 

    void Update()
    {
        // Hold right mouse button to look around
        if (Input.GetMouseButton(1))
        {
            // MouseX and MouseY give how much the mouse moved this frame
            yaw   += Input.GetAxis("Mouse X") * lookSpeed;
            pitch -= Input.GetAxis("Mouse Y") * lookSpeed;

            // Clamp pitch so you can't flip the camera upside down
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }

        // WASD movement relative to where the camera is facing
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        // transform.forward and transform.right move relative to camera's own rotation
        Vector3 move = (transform.forward * v + transform.right * h) * moveSpeed * Time.deltaTime;
        transform.position += move;
    }
}
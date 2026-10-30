using UnityEngine;
using UnityEngine.InputSystem;

// Two camera modes: a fixed "lab" camera (for before/after screenshots) and a follow camera (Tab).
// In follow mode, right mouse button + mouse move orbits the camera around the character.
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 7f;
    public float height = 1.6f;
    public float pitch = 18f;
    public bool follow;

    Vector3 labPosition;
    Quaternion labRotation;
    float yaw;

    void Start() { labPosition = transform.position; labRotation = transform.rotation; }

    public void Toggle() { follow = !follow; if (follow) yaw = 0; }

    void LateUpdate()
    {
        if (!follow || target == null) { transform.SetPositionAndRotation(labPosition, labRotation); return; }
        var mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.isPressed) yaw += mouse.delta.x.ReadValue() * 0.2f;
        var rot = Quaternion.Euler(pitch, yaw, 0);
        var focus = target.position + Vector3.up * height;
        transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
    }
}

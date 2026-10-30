using UnityEngine;
using UnityEngine.InputSystem;

// Character control: WASD moves relative to the camera, Shift runs, Space jumps.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Transform cameraTransform;
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;
    public float pushSpeed = 4f;

    /// <summary>Speed multiplier set by ground decals (1 = normal, lower = oil).</summary>
    [HideInInspector] public float speedMultiplier = 1f;

    CharacterController cc;
    float verticalVelocity;

    void Awake() { cc = GetComponent<CharacterController>(); }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool locked = GameManager.Instance != null && GameManager.Instance.Finished;
        Vector2 input = Vector2.zero;
        if (!locked)
        {
            input.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
            input.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
        }

        Vector3 fwd = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        fwd.y = 0; fwd.Normalize();
        Vector3 right = new Vector3(fwd.z, 0, -fwd.x);
        Vector3 move = (fwd * input.y + right * input.x);
        if (move.sqrMagnitude > 1f) move.Normalize();

        float speed = (kb.leftShiftKey.isPressed ? runSpeed : walkSpeed) * speedMultiplier;

        if (cc.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        if (!locked && cc.isGrounded && kb.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;

        cc.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (move.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 12f * Time.deltaTime);

        if (transform.position.y < -5f) GameManager.Instance?.Respawn();
    }

    // Crates can be pushed; barrels and buildings cannot (they have no Rigidbody).
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic || hit.moveDirection.y < -0.3f) return;
        var dir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        rb.linearVelocity = dir * pushSpeed;
    }
}

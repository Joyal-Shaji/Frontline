using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference lookAction;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraPitchPivot;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
    [SerializeField, Range(0f, 89f)] private float pitchLimit = 85f;
    [SerializeField] private bool invertY;
    [SerializeField] private bool lockCursor = true;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 5f;
    [SerializeField, Min(0f)] private float sprintSpeed = 10f;
    [SerializeField, Min(0f)] private float acceleration = 20f;
    [SerializeField, Min(0f)] private float deceleration = 12f;
    [SerializeField] private Transform viewTransform;
    [SerializeField] private bool rotateTowardsMovement;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;

    [Header("Jump and Gravity")]
    [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    private CharacterController characterController;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float yaw;
    private float pitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        yaw = transform.eulerAngles.y;

        Transform pitchTarget = GetPitchTarget();
        if (pitchTarget != null)
        {
            pitch = Mathf.DeltaAngle(0f, pitchTarget.localEulerAngles.x);
        }
    }

    private void OnEnable()
    {
        EnableAction(moveAction);
        EnableAction(jumpAction);
        EnableAction(sprintAction);
        EnableAction(lookAction);

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        DisableAction(moveAction);
        DisableAction(jumpAction);
        DisableAction(sprintAction);
        DisableAction(lookAction);

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        ApplyMouseLook();

        Vector2 input = moveAction != null
            ? Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f)
            : ReadKeyboardMove();

        Transform movementFrame = viewTransform != null ? viewTransform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(movementFrame.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(movementFrame.right, Vector3.up).normalized;
        Vector3 moveDirection = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);

        bool isSprinting = sprintAction != null
            ? sprintAction.action.IsPressed()
            : IsKeyboardSprintHeld();
        float speed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = moveDirection * speed;
        float rate = moveDirection.sqrMagnitude > 0f ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            rate * Time.deltaTime);

        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = groundedGravity;
            }

            bool jumpHeld = jumpAction != null
                ? jumpAction.action.IsPressed()
                : IsKeyboardJumpHeld();

            if (jumpHeld)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (rotateTowardsMovement && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }

    private static void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference.action.Enable();
        }
    }

    private static void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference.action.Disable();
        }
    }

    private void ApplyMouseLook()
    {
        Vector2 lookDelta = lookAction != null
            ? lookAction.action.ReadValue<Vector2>()
            : Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        yaw += lookDelta.x * mouseSensitivity;
        pitch += lookDelta.y * mouseSensitivity * (invertY ? 1f : -1f);
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        Transform pitchTarget = GetPitchTarget();
        if (pitchTarget != null)
        {
            pitchTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private Transform GetPitchTarget()
    {
        if (cameraPitchPivot != null)
        {
            return cameraPitchPivot;
        }

        return viewTransform != transform ? viewTransform : null;
    }

    private static Vector2 ReadKeyboardMove()
    {
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        float horizontal = (Keyboard.current.dKey.isPressed ? 1f : 0f)
            - (Keyboard.current.aKey.isPressed ? 1f : 0f);
        float vertical = (Keyboard.current.wKey.isPressed ? 1f : 0f)
            - (Keyboard.current.sKey.isPressed ? 1f : 0f);

        return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
    }

    private static bool IsKeyboardJumpHeld()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
    }

    private static bool IsKeyboardSprintHeld()
    {
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }
}

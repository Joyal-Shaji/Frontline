using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class EditorViewModeMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;

    [Header("World-Space Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 5f;
    [SerializeField, Min(0f)] private float sprintSpeed = 10f;
    [SerializeField, Min(0f)] private float acceleration = 20f;
    [SerializeField, Min(0f)] private float deceleration = 12f;
    [SerializeField] private Transform viewTransform;

    [Header("Orbit and Zoom")]
    [SerializeField] private Transform focalPoint;
    [SerializeField, Min(0.1f)] private float initialFocalDistance = 10f;
    [SerializeField, Min(0f)] private float orbitSensitivity = 0.2f;
    [SerializeField, Min(0f)] private float zoomSpeed = 5f;
    [SerializeField, Min(0.1f)] private float minimumZoomDistance = 2f;
    [SerializeField, Min(0.1f)] private float maximumZoomDistance = 100f;

    [Header("Vertical Movement")]
    [SerializeField] private float gravity = 0f;
    [SerializeField] private float groundedGravity = 0f;

    private CharacterController characterController;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float yaw;
    private float initialYaw;
    private Quaternion initialRotation;
    private Vector3 runtimeFocalPoint;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (viewTransform == null)
        {
            SwitchCameraMode cameraMode = FindFirstObjectByType<SwitchCameraMode>();
            if (cameraMode != null && cameraMode.topDownCamera != null)
            {
                viewTransform = cameraMode.topDownCamera.transform;
            }
        }

        yaw = transform.eulerAngles.y;
        initialYaw = yaw;
        initialRotation = transform.rotation;
        runtimeFocalPoint = focalPoint != null
            ? focalPoint.position
            : GetViewTransform().position + GetViewTransform().forward * initialFocalDistance;
    }

    private void OnEnable()
    {
        EnableAction(moveAction);
        EnableAction(sprintAction);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDisable()
    {
        DisableAction(moveAction);
        DisableAction(sprintAction);
    }

    private void Update()
    {
        HandleOrbitAndZoom();

        Vector2 input = moveAction != null
            ? Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f)
            : ReadKeyboardMove();

        // Pan relative to the camera's facing direction, projected onto the world ground plane.
        Transform movementFrame = GetViewTransform();
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
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 previousPosition = transform.position;
        characterController.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        if (focalPoint == null)
        {
            runtimeFocalPoint += transform.position - previousPosition;
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

    private void HandleOrbitAndZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector3 focus = focalPoint != null ? focalPoint.position : runtimeFocalPoint;
        if (mouse.middleButton.isPressed)
        {
            // Only horizontal mouse motion is used; vertical drag never changes camera pitch.
            float yawDelta = mouse.delta.ReadValue().x * orbitSensitivity;
            if (!Mathf.Approximately(yawDelta, 0f))
            {
                yaw += yawDelta;
                transform.position = focus + Quaternion.AngleAxis(yawDelta, Vector3.up)
                    * (transform.position - focus);
                transform.rotation = Quaternion.AngleAxis(yaw - initialYaw, Vector3.up) * initialRotation;
            }
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (!Mathf.Approximately(scroll, 0f))
        {
            // Normalize the 120-unit wheel reports used by many desktop mice, while also
            // supporting devices that report scroll in smaller step values.
            float wheelSteps = Mathf.Abs(scroll) >= 10f ? scroll / 120f : scroll;
            Vector3 offset = transform.position - focus;
            float distance = offset.magnitude;
            float targetDistance = Mathf.Clamp(
                distance - wheelSteps * zoomSpeed,
                minimumZoomDistance,
                maximumZoomDistance);

            if (distance > Mathf.Epsilon)
            {
                transform.position = focus + offset * (targetDistance / distance);
            }
        }
    }

    private Transform GetViewTransform()
    {
        return viewTransform != null ? viewTransform : transform;
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

    private static bool IsKeyboardSprintHeld()
    {
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }
}

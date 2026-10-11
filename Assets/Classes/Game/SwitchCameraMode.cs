using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchCameraMode : MonoBehaviour
{
    // Assign the regular player camera and the editor/top-down camera.
    public Camera playerCamera;
    public Camera topDownCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private EditorViewModeMovement editorViewMovement;

    private void Awake()
    {
        // Stop this component if either camera reference is missing.
        if (playerCamera == null || topDownCamera == null)
        {
            Debug.LogError("Assign both the Main Camera and EditorViewModeCamera in the Inspector.", this);
            enabled = false;
            return;
        }

        // Try to find movement on the player that owns the camera.
        if (playerMovement == null)
        {
            playerMovement = playerCamera.GetComponentInParent<PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = FindFirstObjectByType<PlayerMovement>();
            }
        }

        // Find the editor controls on the editor camera or its parent rig.
        if (editorViewMovement == null)
        {
            editorViewMovement = topDownCamera.GetComponent<EditorViewModeMovement>();
            if (editorViewMovement == null)
            {
                editorViewMovement = topDownCamera.GetComponentInParent<EditorViewModeMovement>();
            }
            if (editorViewMovement == null)
            {
                editorViewMovement = FindFirstObjectByType<EditorViewModeMovement>();
            }
        }

        // Start in normal player-camera mode.
        SetEditorMode(false);
    }

    private void Update()
    {
        // Press E to switch between player and editor views.
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            SwitchCamera();
        }
    }

    public void SwitchCamera()
    {
        // The player camera is enabled when editor mode is off.
        SetEditorMode(playerCamera.enabled);
        if (playerCamera.enabled)
        {
            Debug.Log("Switched to Player Camera");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Debug.Log("Switched to Editor Camera");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void SetEditorMode(bool editorMode)
    {
        // Enable only the camera for the selected mode.
        playerCamera.enabled = !editorMode;
        topDownCamera.enabled = editorMode;

        // Disable player movement while using the editor camera.
        if (playerMovement != null)
        {
            playerMovement.enabled = !editorMode;
        }
        else
        {
            Debug.LogWarning("PlayerMovement was not found. Assign it in the SwitchCameraMode Inspector.", this);
        }

        // Keep the editor controller off in player mode and on in editor mode.
        if (editorViewMovement != null)
        {
            editorViewMovement.enabled = editorMode;
        }
        else
        {
            Debug.LogWarning("EditorViewModeMovement was not found. Assign it in the SwitchCameraMode Inspector.", this);
        }

        // Keep only the active camera's audio listener enabled.
        AudioListener playerAudioListener = playerCamera.GetComponent<AudioListener>();
        AudioListener editorAudioListener = topDownCamera.GetComponent<AudioListener>();
        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = !editorMode;
        }
        if (editorAudioListener != null)
        {
            editorAudioListener.enabled = editorMode;
        }
    }
}

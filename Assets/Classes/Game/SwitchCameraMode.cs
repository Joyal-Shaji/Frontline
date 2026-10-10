using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchCameraMode : MonoBehaviour
{
    // Assign the regular player camera and the editor/top-down camera.
    public Camera playerCamera;
    public Camera topDownCamera;
    [SerializeField] private PlayerMovement playerMovement;

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

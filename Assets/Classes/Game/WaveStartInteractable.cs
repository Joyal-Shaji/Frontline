using UnityEngine;
using UnityEngine.InputSystem;

public sealed class WaveStartInteractable : MonoBehaviour
{
    [SerializeField, Min(1f)] private float interactionDistance = 20f;

    private GameLoopManager gameLoopManager;
    private Camera interactionCamera;
    private Renderer buttonRenderer;
    private Color originalColor = Color.white;
    private bool isAvailable;
    private bool availabilityInitialized;

    public void Initialize(GameLoopManager manager)
    {
        gameLoopManager = manager;
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
        {
            Material material = buttonRenderer.material;
            if (material.HasProperty("_BaseColor"))
            {
                originalColor = material.GetColor("_BaseColor");
            }
            else if (material.HasProperty("_Color"))
            {
                originalColor = material.color;
            }
        }

        interactionCamera = FindInteractionCamera();
        SetAvailable(manager != null && manager.CanStartNextWave);
    }

    private void Update()
    {
        if (gameLoopManager == null)
        {
            return;
        }

        SetAvailable(gameLoopManager.CanStartNextWave);
        if (!isAvailable || Time.timeScale <= 0f)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool keyboardPressed = keyboard != null && keyboard.fKey.wasPressedThisFrame;
        bool mousePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        if (!keyboardPressed && !mousePressed)
        {
            return;
        }

        interactionCamera = FindInteractionCamera();
        if (interactionCamera == null)
        {
            return;
        }

        Vector2 screenPosition = keyboardPressed || (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            : mouse.position.ReadValue();
        Ray ray = interactionCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance) &&
            (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
        {
            gameLoopManager.StartNextWave();
        }
    }

    public void SetAvailable(bool available)
    {
        if (availabilityInitialized && isAvailable == available)
        {
            return;
        }

        isAvailable = available;
        availabilityInitialized = true;
        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponent<Renderer>();
        }
        if (buttonRenderer == null)
        {
            return;
        }

        Material material = buttonRenderer.material;
        Color color = available ? new Color(0.25f, 0.9f, 0.55f, 1f) : new Color(0.35f, 0.35f, 0.35f, 1f);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.color = color;
        }
    }

    private Camera FindInteractionCamera()
    {
        SwitchCameraMode cameraMode = FindFirstObjectByType<SwitchCameraMode>();
        if (cameraMode != null)
        {
            if (cameraMode.playerCamera != null && cameraMode.playerCamera.enabled)
            {
                return cameraMode.playerCamera;
            }
            if (cameraMode.topDownCamera != null && cameraMode.topDownCamera.enabled)
            {
                return cameraMode.topDownCamera;
            }
        }

        return Camera.main;
    }

    private void OnDestroy()
    {
        if (buttonRenderer != null && buttonRenderer.material != null)
        {
            Material material = buttonRenderer.material;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", originalColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.color = originalColor;
            }
        }
    }
}

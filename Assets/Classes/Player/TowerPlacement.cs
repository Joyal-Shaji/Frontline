using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class TowerPlacement : MonoBehaviour
{
    [SerializeField] private Camera editorCamera;
    [SerializeField] private LayerMask placementSurfaceMask = ~0;
    [SerializeField, Min(0f)] private float raycastDistance = 500f;

    private GameObject currentPlacingTower;
    private bool waitForMouseRelease;
    private bool currentPlacementIsValid;

    private void Awake()
    {
        EnsureInputSystemEventSystem();

        if (editorCamera == null)
        {
            SwitchCameraMode cameraMode = FindFirstObjectByType<SwitchCameraMode>();
            if (cameraMode != null)
            {
                editorCamera = cameraMode.topDownCamera;
            }
        }
    }

    private static void EnsureInputSystemEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        }

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
    }

    private void Update()
    {
        if (currentPlacingTower == null)
        {
            return;
        }

        if (editorCamera == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (waitForMouseRelease)
        {
            if (!mouse.leftButton.isPressed)
            {
                waitForMouseRelease = false;
            }

            return;
        }

        Ray placementRay = editorCamera.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(
            placementRay,
            raycastDistance,
            placementSurfaceMask,
            QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));

        // Find the first surface hit that is not part of the tower preview itself.
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == currentPlacingTower.transform ||
                hit.transform.IsChildOf(currentPlacingTower.transform))
            {
                continue;
            }

            currentPlacingTower.transform.position = hit.point;
            // Only the designated CanPlace ground is valid; enemy and tower colliders are rejected too.
            currentPlacementIsValid = hit.collider.CompareTag("CanPlace") &&
                hit.collider.GetComponentInParent<Enemy>() == null &&
                hit.collider.GetComponentInParent<TowerBehavior>() == null;

            // Ignore clicks over UI; the UI click that selected the tower must not place it.
            if (currentPlacementIsValid && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                currentPlacingTower = null;
            }

            return;
        }
    }

    public void SetTowerToPlace(GameObject tower)
    {
        if (tower == null)
        {
            Debug.LogWarning("Cannot place a tower because no tower prefab was provided.", this);
            return;
        }

        if (currentPlacingTower != null)
        {
            Destroy(currentPlacingTower);
        }

        currentPlacingTower = Instantiate(tower, Vector3.zero, Quaternion.identity);
        currentPlacementIsValid = false;
        // Wait for the UI button click to finish before accepting a placement click.
        waitForMouseRelease = Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}

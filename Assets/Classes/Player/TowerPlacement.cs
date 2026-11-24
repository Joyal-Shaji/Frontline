using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class TowerPlacement : MonoBehaviour
{
    [SerializeField] private Camera editorCamera;
    [SerializeField] private LayerMask placementSurfaceMask = ~0;
    [Tooltip("Layers the placement ray passes through, such as invisible map borders.")]
    [SerializeField] private LayerMask ignoredPlacementLayers = 1 << 2;
    [SerializeField, Min(0f)] private float raycastDistance = 500f;

    private GameObject currentPlacingTower;
    private TowerBehavior selectedTower;
    private bool waitForMouseRelease;
    private bool currentPlacementIsValid;
    private static readonly Rect PriorityPanelRect = new Rect(20f, 20f, 300f, 190f);

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
        if (editorCamera == null || !editorCamera.enabled)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (currentPlacingTower == null)
        {
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI() && !IsPointerOverPriorityPanel(mouse.position.ReadValue()))
            {
                SelectTowerUnderPointer(mouse.position.ReadValue());
            }
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
            placementSurfaceMask & ~ignoredPlacementLayers,
            QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));

        // Find the first surface hit that is not part of the tower preview itself.
        foreach (RaycastHit hit in hits)
        {
            if ((ignoredPlacementLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                continue;
            }

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
                TowerBehavior placedTower = currentPlacingTower.GetComponent<TowerBehavior>();
                if (placedTower != null)
                {
                    placedTower.SetPlacementPreview(false);
                    SelectTower(placedTower);
                }
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

        SelectTower(null);

        currentPlacingTower = Instantiate(tower, Vector3.zero, Quaternion.identity);
        TowerBehavior towerBehavior = currentPlacingTower.GetComponent<TowerBehavior>();
        if (towerBehavior != null)
        {
            towerBehavior.SetPlacementPreview(true);
        }
        currentPlacementIsValid = false;
        // Wait for the UI button click to finish before accepting a placement click.
        waitForMouseRelease = Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void SelectTowerUnderPointer(Vector2 pointerPosition)
    {
        Ray ray = editorCamera.ScreenPointToRay(pointerPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance, ~ignoredPlacementLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));

        foreach (RaycastHit hit in hits)
        {
            TowerBehavior tower = hit.collider.GetComponentInParent<TowerBehavior>();
            if (tower != null)
            {
                SelectTower(tower);
                return;
            }
        }

        SelectTower(null);
    }

    private void SelectTower(TowerBehavior tower)
    {
        if (selectedTower == tower)
        {
            return;
        }

        if (selectedTower != null)
        {
            selectedTower.ShowRangeIndicator(false);
        }

        selectedTower = tower;
        if (selectedTower != null)
        {
            selectedTower.ShowRangeIndicator(true);
        }
    }

    private void OnGUI()
    {
        if (selectedTower == null || editorCamera == null || !editorCamera.enabled)
        {
            return;
        }

        GUI.Box(PriorityPanelRect, "TURRET TARGET PRIORITY");
        GUI.Label(new Rect(35f, 55f, 270f, 24f), $"Range: {selectedTower.Range:0.0} m");
        DrawPriorityButton(new Rect(35f, 84f, 270f, 28f), "Closest enemy", TowerTargetPriority.ClosestEnemy);
        DrawPriorityButton(new Rect(35f, 117f, 270f, 28f), "Most current health", TowerTargetPriority.MostHealth);
        DrawPriorityButton(new Rect(35f, 150f, 270f, 28f), "Fastest enemy", TowerTargetPriority.FastestEnemy);
    }

    private void DrawPriorityButton(Rect rect, string label, TowerTargetPriority priority)
    {
        string buttonLabel = selectedTower.TargetPriority == priority ? $"✓  {label}" : label;
        if (GUI.Button(rect, buttonLabel))
        {
            selectedTower.SetTargetPriority(priority);
        }
    }

    private static bool IsPointerOverPriorityPanel(Vector2 pointerPosition)
    {
        Vector2 guiPosition = new Vector2(pointerPosition.x, Screen.height - pointerPosition.y);
        return PriorityPanelRect.Contains(guiPosition);
    }
}

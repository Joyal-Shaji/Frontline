using UnityEngine;

public enum TowerTargetPriority
{
    ClosestEnemy,
    MostHealth,
    FastestEnemy
}

public class TowerBehavior : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField, Min(0.1f)] private float range = 8f;
    [SerializeField, Min(0f)] private float damage = 20f;
    [SerializeField, Min(0.05f)] private float attacksPerSecond = 1f;
    [SerializeField, Min(0f)] private float turnSpeed = 360f;
    [SerializeField] private Transform turretHead;
    [SerializeField] private TowerTargetPriority targetPriority = TowerTargetPriority.ClosestEnemy;

    [Header("Range Indicator")]
    [SerializeField, Range(16, 128)] private int rangeCircleSegments = 64;
    [SerializeField, Range(0f, 1f)] private float rangeCircleOpacity = 0.22f;
    [SerializeField, Min(0.001f)] private float rangeCircleWidth = 0.04f;

    private float attackTimer;
    private LineRenderer rangeIndicator;
    private bool isPlacementPreview;

    public float Range => range;
    public TowerTargetPriority TargetPriority => targetPriority;

    private void Awake()
    {
        FindTurretHead();
        attackTimer = 0f;
    }

    private void Update()
    {
        if (isPlacementPreview)
        {
            return;
        }

        Enemy target = FindTarget();
        if (target == null)
        {
            attackTimer = 0f;
            return;
        }

        AimAt(target);
        attackTimer += Time.deltaTime;
        float attackInterval = 1f / Mathf.Max(0.05f, attacksPerSecond);
        if (attackTimer >= attackInterval)
        {
            attackTimer -= attackInterval;
            target.Health -= damage;
        }
    }

    public void SetTargetPriority(TowerTargetPriority priority)
    {
        targetPriority = priority;
    }

    public void SetPlacementPreview(bool preview)
    {
        isPlacementPreview = preview;
        enabled = !preview;
        ShowRangeIndicator(preview);
    }

    public void ShowRangeIndicator(bool visible)
    {
        EnsureRangeIndicator();
        rangeIndicator.enabled = visible;
    }

    private void FindTurretHead()
    {
        if (turretHead != null)
        {
            return;
        }

        Transform candidate = transform.Find("TurretStand/TurretHead");
        if (candidate == null)
        {
            candidate = transform.Find("TurretHead");
        }

        turretHead = candidate != null ? candidate : transform;
    }

    private Enemy FindTarget()
    {
        if (EntitySummoner.EnemiesInGame == null)
        {
            return null;
        }

        Enemy bestTarget = null;
        float bestValue = targetPriority == TowerTargetPriority.ClosestEnemy ? float.PositiveInfinity : float.NegativeInfinity;
        float rangeSquared = range * range;

        foreach (Enemy enemy in EntitySummoner.EnemiesInGame)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.Health <= 0f)
            {
                continue;
            }

            float distanceSquared = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distanceSquared > rangeSquared)
            {
                continue;
            }

            float value = targetPriority switch
            {
                TowerTargetPriority.MostHealth => enemy.Health,
                TowerTargetPriority.FastestEnemy => enemy.speed,
                _ => distanceSquared
            };

            bool isBetter = targetPriority == TowerTargetPriority.ClosestEnemy
                ? value < bestValue
                : value > bestValue;
            if (isBetter)
            {
                bestValue = value;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    private void AimAt(Enemy target)
    {
        Vector3 direction = target.transform.position - turretHead.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        turretHead.rotation = Quaternion.RotateTowards(turretHead.rotation, desiredRotation, turnSpeed * Time.deltaTime);
    }

    private void EnsureRangeIndicator()
    {
        if (rangeIndicator != null)
        {
            return;
        }

        GameObject circle = new GameObject("Range Indicator");
        circle.transform.SetParent(transform, false);
        circle.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        rangeIndicator = circle.AddComponent<LineRenderer>();
        rangeIndicator.useWorldSpace = false;
        rangeIndicator.loop = true;
        rangeIndicator.positionCount = rangeCircleSegments;
        rangeIndicator.startWidth = rangeCircleWidth;
        rangeIndicator.endWidth = rangeCircleWidth;
        rangeIndicator.numCapVertices = 2;
        rangeIndicator.numCornerVertices = 2;
        rangeIndicator.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeIndicator.receiveShadows = false;

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader != null)
        {
            rangeIndicator.material = new Material(lineShader);
        }

        Color color = new Color(0.2f, 0.95f, 0.7f, rangeCircleOpacity);
        rangeIndicator.startColor = color;
        rangeIndicator.endColor = color;
        rangeIndicator.enabled = false;
        UpdateRangeCircle();
    }

    private void UpdateRangeCircle()
    {
        if (rangeIndicator == null)
        {
            return;
        }

        rangeIndicator.positionCount = rangeCircleSegments;
        for (int i = 0; i < rangeCircleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / rangeCircleSegments;
            rangeIndicator.SetPosition(i, new Vector3(Mathf.Cos(angle) * range, 0f, Mathf.Sin(angle) * range));
        }
    }

    private void OnValidate()
    {
        if (rangeIndicator != null)
        {
            UpdateRangeCircle();
        }
    }

    private void OnDestroy()
    {
        if (rangeIndicator != null && rangeIndicator.material != null)
        {
            Destroy(rangeIndicator.material);
        }
    }
}

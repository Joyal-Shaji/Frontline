using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Jobs;

public class GameLoopManager : MonoBehaviour
{
    public static Vector3[] NodePositions;
    private static Queue<Enemy> EnemiesToRemove;

    public Transform NodeParent;
    public bool loopShouldEnd;
    [SerializeField] private TextAsset waveConfigurationFile;

    private WaveConfiguration waveConfiguration;
    private WaveStartInteractable waveButton;
    private int nextWaveIndex;
    private int pendingSpawns;
    private bool waveActive;

    public bool CanStartNextWave => !waveActive && waveConfiguration != null &&
        waveConfiguration.waves != null && nextWaveIndex < waveConfiguration.waves.Length;
    public bool IsWaveActive => waveActive;
    public int CurrentWaveNumber => waveActive ? nextWaveIndex : nextWaveIndex + 1;

    void Start()
    {
        EnemiesToRemove = new Queue<Enemy>();
        EntitySummoner.Init();

        if (NodeParent == null)
        {
            Debug.LogError("GameLoopManager needs a NodeParent assigned.", this);
            enabled = false;
            return;
        }

        NodePositions = new Vector3[NodeParent.childCount];
        for (int i = 0; i < NodeParent.childCount; i++)
        {
            NodePositions[i] = NodeParent.GetChild(i).position;
        }

        LoadWaveConfiguration();
        ConnectWaveButton();
        StartCoroutine(GameLoop());
    }

    private void Update()
    {
        if (waveActive && pendingSpawns == 0 && EntitySummoner.EnemiesInGame.Count == 0)
        {
            waveActive = false;
            Debug.Log($"Wave {nextWaveIndex} complete. Press the Next Wave button to continue.", this);
            UpdateWaveButton();
        }
    }

    private void LoadWaveConfiguration()
    {
        if (waveConfigurationFile == null)
        {
            waveConfigurationFile = Resources.Load<TextAsset>("Waves");
        }

        if (waveConfigurationFile == null)
        {
            Debug.LogError("Wave config not found. Add Assets/Resources/Waves.json or assign the file in the Inspector.", this);
            return;
        }

        try
        {
            waveConfiguration = JsonUtility.FromJson<WaveConfiguration>(waveConfigurationFile.text);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not parse wave config '{waveConfigurationFile.name}': {exception.Message}", this);
            return;
        }

        if (waveConfiguration == null || waveConfiguration.waves == null || waveConfiguration.waves.Length == 0)
        {
            Debug.LogError("Wave config must contain at least one wave.", this);
            waveConfiguration = null;
        }
    }

    private void ConnectWaveButton()
    {
        GameObject buttonObject = GameObject.Find("Next Wave Button");
        if (buttonObject == null)
        {
            Debug.LogWarning("No GameObject named 'Next Wave Button' was found; wave-start can still be called from code.", this);
            return;
        }

        waveButton = buttonObject.GetComponent<WaveStartInteractable>();
        if (waveButton == null)
        {
            waveButton = buttonObject.AddComponent<WaveStartInteractable>();
        }
        waveButton.Initialize(this);
    }

    private void UpdateWaveButton()
    {
        if (waveButton != null)
        {
            waveButton.SetAvailable(CanStartNextWave);
        }
    }

    public void StartNextWave()
    {
        if (!CanStartNextWave)
        {
            return;
        }

        WaveDefinition wave = waveConfiguration.waves[nextWaveIndex];
        nextWaveIndex++;
        waveActive = true;
        pendingSpawns = 0;

        if (wave.enemies != null)
        {
            foreach (WaveEnemyEntry entry in wave.enemies)
            {
                if (entry != null && entry.count > 0)
                {
                    pendingSpawns += entry.count;
                }
            }
        }

        Debug.Log($"Starting wave {nextWaveIndex} ({pendingSpawns} enemies).", this);
        UpdateWaveButton();
        StartCoroutine(SpawnWave(wave));
    }

    private IEnumerator SpawnWave(WaveDefinition wave)
    {
        float spawnInterval = Mathf.Max(0f, wave.spawnInterval);
        if (wave.enemies != null)
        {
            foreach (WaveEnemyEntry entry in wave.enemies)
            {
                if (entry == null || entry.count <= 0)
                {
                    continue;
                }

                for (int i = 0; i < entry.count; i++)
                {
                    EntitySummoner.SummonEnemy(entry.enemyID, entry.health, entry.speed);
                    pendingSpawns--;

                    if (spawnInterval > 0f && pendingSpawns > 0)
                    {
                        yield return new WaitForSeconds(spawnInterval);
                    }
                }
            }
        }
    }

    private IEnumerator GameLoop()
    {
        while (!loopShouldEnd)
        {
            if (EntitySummoner.EnemiesInGame.Count == 0)
            {
                yield return null;
                continue;
            }

            NativeArray<Vector3> nodes = new NativeArray<Vector3>(NodePositions, Allocator.TempJob);
            NativeArray<float> enemySpeeds = new NativeArray<float>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            NativeArray<int> nodeIndices = new NativeArray<int>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            TransformAccessArray enemyAccess = new TransformAccessArray(EntitySummoner.EnemiesInGameTransforms.ToArray(), 2);

            for (int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                enemySpeeds[i] = EntitySummoner.EnemiesInGame[i].speed;
                nodeIndices[i] = EntitySummoner.EnemiesInGame[i].NodeIndex;
            }

            MoveEnemiesJob moveJob = new MoveEnemiesJob
            {
                NodePositions = nodes,
                EnemySpeeds = enemySpeeds,
                NodeIndices = nodeIndices,
                deltaTime = Time.deltaTime
            };

            JobHandle moveJobHandle = moveJob.Schedule(enemyAccess);
            moveJobHandle.Complete();

            for (int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EntitySummoner.EnemiesInGame[i].NodeIndex = nodeIndices[i];
                if (nodeIndices[i] >= NodePositions.Length)
                {
                    EnqueEnemyToRemove(EntitySummoner.EnemiesInGame[i]);
                }
            }

            enemySpeeds.Dispose();
            nodeIndices.Dispose();
            enemyAccess.Dispose();
            nodes.Dispose();

            while (EnemiesToRemove.Count > 0)
            {
                EntitySummoner.RemoveEnemy(EnemiesToRemove.Dequeue());
            }

            yield return null;
        }
    }

    public static void EnqueEnemyToRemove(Enemy EnemyToRemove)
    {
        if (EnemyToRemove != null && EnemiesToRemove != null)
        {
            EnemiesToRemove.Enqueue(EnemyToRemove);
        }
    }
}

[Serializable]
public class WaveConfiguration
{
    public WaveDefinition[] waves;
}

[Serializable]
public class WaveDefinition
{
    public float spawnInterval = 1f;
    public WaveEnemyEntry[] enemies;
}

[Serializable]
public class WaveEnemyEntry
{
    public int enemyID;
    public int count;
    public float health;
    public float speed;
}

public struct MoveEnemiesJob : IJobParallelForTransform
{
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> NodePositions;
    [NativeDisableParallelForRestriction]
    public NativeArray<float> EnemySpeeds;
    [NativeDisableParallelForRestriction]
    public NativeArray<int> NodeIndices;
    public float deltaTime;

    public void Execute(int index, TransformAccess transform)
    {
        if (NodeIndices[index] < NodePositions.Length)
        {
            Vector3 PositionToMoveTo = NodePositions[NodeIndices[index]];
            transform.position = Vector3.MoveTowards(transform.position, PositionToMoveTo, EnemySpeeds[index] * deltaTime);

            if (transform.position == PositionToMoveTo)
            {
                NodeIndices[index]++;
            }
        }

    }
}
using UnityEngine;
using System.Collections.Generic;

public class EntitySummoner : MonoBehaviour
{
    public static List<Enemy> EnemiesInGame;
    public static List<Transform> EnemiesInGameTransforms;
    public static Dictionary<int, GameObject> EnemyPrefabs;
    public static Dictionary<int, Queue<Enemy>> EnemyObjectPools;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private static bool IsInitialized;
    public static void Init()
    {
        if (!IsInitialized)
        {
            EnemyPrefabs = new Dictionary<int, GameObject>();
            EnemyObjectPools = new Dictionary<int, Queue<Enemy>>();
            EnemiesInGameTransforms = new List<Transform>();
            EnemiesInGame = new List<Enemy>();

            EnemySummonData[] Enemies = Resources.LoadAll<EnemySummonData>("Enemies");

            foreach (EnemySummonData enemy in Enemies)
            {
                EnemyPrefabs.Add(enemy.EnemyID, enemy.EnemyPrefab);
                EnemyObjectPools.Add(enemy.EnemyID, new Queue<Enemy>());
            }
            IsInitialized = true;
        }
        else
        {
            Debug.Log("ENTITY SUMMONER: THIS CLASS IS ALREADY INTIALIZED");
        }

    }


    public static Enemy SummonEnemy(int EnemyID, float healthOverride = 0f, float speedOverride = 0f)
    {
        Enemy SummonedEnemy = null;

        if (EnemyPrefabs != null && EnemyPrefabs.TryGetValue(EnemyID, out GameObject enemyPrefab))
        {
            Queue<Enemy> ReferencedQueue = EnemyObjectPools[EnemyID];

            if (ReferencedQueue.Count > 0)
            {
                SummonedEnemy = ReferencedQueue.Dequeue();
                SummonedEnemy.Init(healthOverride, speedOverride);
                SummonedEnemy.gameObject.SetActive(true);
            }
            else
            {
                GameObject NewEnemy = Instantiate(enemyPrefab, GameLoopManager.NodePositions[0], Quaternion.identity);
                SummonedEnemy = NewEnemy.GetComponent<Enemy>();
                if (SummonedEnemy != null)
                {
                    SummonedEnemy.Init(healthOverride, speedOverride);
                }
            }
        }
        else
        {
            Debug.Log($"ENTITY SUMMONER: NO ENEMY PREFAB FOUND FOR ID {EnemyID}");
            return null;
        }

        if (SummonedEnemy == null)
        {
            Debug.LogError($"Enemy prefab for ID {EnemyID} is missing the Enemy component.");
            return null;
        }

        EnemiesInGameTransforms.Add(SummonedEnemy.transform);
        EnemiesInGame.Add(SummonedEnemy);
        SummonedEnemy.ID = EnemyID;
        return SummonedEnemy;
    }

    public static void RemoveEnemy(Enemy EnemyToRemove)
    {
        if (EnemyToRemove == null || EnemiesInGame == null || !EnemiesInGame.Contains(EnemyToRemove))
        {
            return;
        }

        if (EnemyObjectPools == null || !EnemyObjectPools.TryGetValue(EnemyToRemove.ID, out Queue<Enemy> pool))
        {
            Debug.LogError($"No object pool exists for enemy ID {EnemyToRemove.ID}.");
            return;
        }

        pool.Enqueue(EnemyToRemove);
        EnemyToRemove.gameObject.SetActive(false);
        EnemiesInGameTransforms.Remove(EnemyToRemove.transform);
        EnemiesInGame.Remove(EnemyToRemove);
    }
}

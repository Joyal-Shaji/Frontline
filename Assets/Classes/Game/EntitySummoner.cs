using UnityEngine;
using System.Collections;
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


    public static Enemy SummonEnemy(int EnemyID)
    {
        Enemy SummonedEnemy = null;

        if (EnemyPrefabs.ContainsKey(EnemyID))
        {
            Queue<Enemy> ReferencedQueue = EnemyObjectPools[EnemyID];

            if (ReferencedQueue.Count > 0)
            {
                SummonedEnemy = ReferencedQueue.Dequeue();
                SummonedEnemy.Init();

                SummonedEnemy.gameObject.SetActive(true);
            }
            else
            {
                GameObject NewEnemy = Instantiate(EnemyPrefabs[EnemyID], GameLoopManager.NodePositions[0], Quaternion.identity);
                SummonedEnemy = NewEnemy.GetComponent<Enemy>();
                SummonedEnemy.Init();
            }
        }
        else
        {
            Debug.Log($"ENTITY SUMMONER: NO ENEMY PREFAB FOUND FOR ID {EnemyID}");
        }
        EnemiesInGameTransforms.Add(SummonedEnemy.transform);
        EnemiesInGame.Add(SummonedEnemy);
        SummonedEnemy.ID = EnemyID;
        return SummonedEnemy;
    }

    public static void RemoveEnemy(Enemy EnemyToRemove)
    {
        EnemyObjectPools[EnemyToRemove.ID].Enqueue(EnemyToRemove);
        EnemyToRemove.gameObject.SetActive(false);
        EnemiesInGameTransforms.Remove(EnemyToRemove.transform);
        EnemiesInGame.Remove(EnemyToRemove);


    }
}

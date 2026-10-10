using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Jobs;

public class GameLoopManager : MonoBehaviour
{
    public static Vector3[] NodePositions;
    private static Queue<Enemy> EnemiesToRemove;
    private static Queue<int> EnemyIDsToSummon;

    public Transform NodeParent;
    public bool loopShouldEnd;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnemyIDsToSummon = new Queue<int>();
        EnemiesToRemove = new Queue<Enemy>();
        EntitySummoner.Init();
        NodePositions = new Vector3[NodeParent.childCount];
        for (int i = 0; i < NodeParent.childCount; i++)
        {
            NodePositions[i] = NodeParent.GetChild(i).position;
        }


        StartCoroutine(GameLoop());
        InvokeRepeating("SummmonTest", 0f, 1f); //Test to see if enemies are being summoned
        //InvokeRepeating("RemoveTest", 0f, 1.5f);    //Test to see if enemies are being removed
    }
    /* TESTS
        void RemoveTest()
        {
            if (EntitySummoner.EnemiesInGame.Count > 0)
            {
                EntitySummoner.RemoveEnemy(EntitySummoner.EnemiesInGame[Random.Range(0, EntitySummoner.EnemiesInGame.Count)]);
            }
        }
    */
    void SummmonTest()
    {
        EnqueEnemyIDtoSummon(1);
    }

    IEnumerator GameLoop()
    {
        while (loopShouldEnd == false)
        {
            //spawn enemies
            if (EnemyIDsToSummon.Count > 0)
            {
                for (int i = 0; i < EnemyIDsToSummon.Count; i++)
                {
                    EntitySummoner.SummonEnemy(EnemyIDsToSummon.Dequeue());
                }
            }
            //spawn towers
            //Move enemies
            NativeArray<Vector3> NodesToUse = new NativeArray<Vector3>(NodePositions, Allocator.TempJob);
            NativeArray<float> EnemySpeeds = new NativeArray<float>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            NativeArray<int> NodeIndices = new NativeArray<int>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            TransformAccessArray EnemyAccess = new TransformAccessArray(EntitySummoner.EnemiesInGameTransforms.ToArray(), 2);

            for (int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EnemySpeeds[i] = EntitySummoner.EnemiesInGame[i].speed;
                NodeIndices[i] = EntitySummoner.EnemiesInGame[i].NodeIndex;
            }

            MoveEnemiesJob MoveJob = new MoveEnemiesJob
            {
                NodePositions = NodesToUse,
                EnemySpeeds = EnemySpeeds,
                NodeIndices = NodeIndices,
                deltaTime = Time.deltaTime
            };

            JobHandle MoveJobHandle = MoveJob.Schedule(EnemyAccess);
            MoveJobHandle.Complete();

            for (int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EntitySummoner.EnemiesInGame[i].NodeIndex = NodeIndices[i];

                if (EntitySummoner.EnemiesInGame[i].NodeIndex == NodePositions.Length)
                {
                    EnqueEnemyToRemove(EntitySummoner.EnemiesInGame[i]);
                }
            }

            EnemySpeeds.Dispose();
            NodeIndices.Dispose();
            EnemyAccess.Dispose();
            NodesToUse.Dispose();



            //tick towers
            //apply effects
            //damage enemies
            //remove enemies
            if (EnemiesToRemove.Count > 0)
            {
                for (int i = 0; i < EnemiesToRemove.Count; i++)
                {
                    EntitySummoner.RemoveEnemy(EnemiesToRemove.Dequeue());
                }
            }
            //remove towers
            yield return null;
        }
    }
    public static void EnqueEnemyIDtoSummon(int ID)
    {
        EnemyIDsToSummon.Enqueue(ID);

    }
    public static void EnqueEnemyToRemove(Enemy EnemyToRemove)
    {
        EnemiesToRemove.Enqueue(EnemyToRemove);
    }
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
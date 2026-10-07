using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private static Queue<int> EnemyIDsToSummon;

    public bool loopShouldEnd;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnemyIDsToSummon = new Queue<int>();
        EntitySummoner.Init();

        StartCoroutine(GameLoop());
        InvokeRepeating("SummmonTest", 0f, 1f); //Test to see if enemies are being summoned
        InvokeRepeating("RemoveTest", 0f, 1.5f);    //Test to see if enemies are being removed
    }

    void RemoveTest()
    {
        if (EntitySummoner.EnemiesInGame.Count > 0)
        {
            EntitySummoner.RemoveEnemy(EntitySummoner.EnemiesInGame[Random.Range(0, EntitySummoner.EnemiesInGame.Count)]);
        }
    }
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
            //tick towers
            //apply effects
            //damage enemies
            //remove enemies
            //remove towers
            yield return null;
        }
    }
    public static void EnqueEnemyIDtoSummon(int ID)
    {
        EnemyIDsToSummon.Enqueue(ID);

    }
}

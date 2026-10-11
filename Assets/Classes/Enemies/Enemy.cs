using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public int NodeIndex;
    public float maxHealth;
    public float health;
    public float speed;
    public int ID;
    public float Health
    {
        get { return health; }
        set
        {
            health = value;
            if (health <= 0)
            {
                EntitySummoner.RemoveEnemy(this);
            }
        }
    }
    public void Init()
    {
        // Initialize enemy properties here
        health = maxHealth;
        transform.position = GameLoopManager.NodePositions[0];
        NodeIndex = 0;
    }
}

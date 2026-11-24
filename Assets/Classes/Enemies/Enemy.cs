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
    private float defaultMaxHealth;
    private float defaultSpeed;
    private bool defaultsCached;

    private void Awake()
    {
        CacheDefaults();
    }

    public float Health
    {
        get { return health; }
        set
        {
            health = value;
            if (health <= 0)
            {
                if (EntitySummoner.EnemiesInGame != null && EntitySummoner.EnemiesInGame.Contains(this))
                {
                    EntitySummoner.RemoveEnemy(this);
                }
            }
        }
    }

    public void Init(float healthOverride = 0f, float speedOverride = 0f)
    {
        CacheDefaults();
        maxHealth = defaultMaxHealth;
        speed = defaultSpeed;

        if (healthOverride > 0f)
        {
            maxHealth = healthOverride;
        }
        if (speedOverride > 0f)
        {
            speed = speedOverride;
        }

        health = maxHealth;
        transform.position = GameLoopManager.NodePositions[0];
        NodeIndex = 0;
    }

    private void CacheDefaults()
    {
        if (defaultsCached)
        {
            return;
        }

        defaultMaxHealth = maxHealth;
        defaultSpeed = speed;
        defaultsCached = true;
    }
}

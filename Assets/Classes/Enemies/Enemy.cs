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
    public void Init()
    {
        // Initialize enemy properties here
        health = maxHealth;
        transform.position = GameLoopManager.NodePositions[0];
        NodeIndex = 0;
    }
}

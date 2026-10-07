using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public float maxHealth;
    public float health;
    public float speed;
    public int ID;
    public void Init()
    {
        // Initialize enemy properties here
        health = maxHealth;
    }
}

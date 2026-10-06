using UnityEngine;
using System.Collections;

public class NewMonoBehaviourScript : MonoBehaviour
{

    public bool loopShouldEnd;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    IEnumerator GameLoop()
    {
        while (loopShouldEnd == false)
        {
            //spawn enemies
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
}

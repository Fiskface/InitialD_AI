using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.parent.parent.TryGetComponent<CarControllerRealistic>(out CarControllerRealistic car))
        {
            if (car.isAI)
            {
                //Debug.Log("You lose!");
                var a = other.transform.parent.parent.GetComponent<CarControllerSDC>();
                a.reachedGoal = true;
                Debug.Log($"{a.geneticManager.filePath}: reached goal in {a.timeSinceStart}");
            }
            else
            {
                Debug.Log("You Win!");
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<CarControllerRealistic>(out CarControllerRealistic car))
        {
            if (car.isAI)
            {
                Debug.Log("You lose!");
                other.GetComponent<CarControllerSDC>().reachedGoal = true;
            }
            else
            {
                Debug.Log("You Win!");
            }
        }
    }
}

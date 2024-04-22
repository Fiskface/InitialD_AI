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
                Debug.Log("You lose!");
                other.transform.parent.parent.GetComponent<CarControllerSDC>().reachedGoal = true;
            }
            else
            {
                Debug.Log("You Win!");
            }
        }
    }
}

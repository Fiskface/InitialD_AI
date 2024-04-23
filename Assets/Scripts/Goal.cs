using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Goal : MonoBehaviour
{
    public bool switchScene = true;

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.parent.parent.TryGetComponent<CarControllerRealistic>(out CarControllerRealistic car))
        {
            if (car.isAI)
            {
                var a = other.transform.parent.parent.GetComponent<CarControllerSDC>();
                a.reachedGoal = true;
                if(switchScene) SceneManager.LoadScene("YouLose");
            }
            else
            {
                if(switchScene) SceneManager.LoadScene("YouWin");
            }
        }
    }
}

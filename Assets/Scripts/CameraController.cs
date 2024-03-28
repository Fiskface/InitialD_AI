using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    private Rigidbody playerRB;
    public Vector3 offset;
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        playerRB = player.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 playerForward = (playerRB.velocity + player.transform.forward).normalized;
        transform.position = Vector3.Lerp(transform.position, 
            player.position + player.transform.TransformVector(new Vector3(offset.x, 0, offset.z)) + new Vector3(0, offset.y, 0) + playerForward * (-5f), speed * Time.deltaTime);
        
        transform.LookAt(player);
    }
}

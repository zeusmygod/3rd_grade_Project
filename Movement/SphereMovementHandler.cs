using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereMovementHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void Update()
    {
        CheckFallRespawn();
    }
    void CheckFallRespawn()
    {
        if (transform.position.y < -67)
        {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

            transform.position = Utils.GetSpawnPointSphere();
        }
    }
}

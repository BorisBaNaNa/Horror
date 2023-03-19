using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformPlayerFromTo : MonoBehaviour
{
    public Transform newPosition;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            other.GetComponent<PlayerController>().TeleportTo(newPosition);
            //other.transform.SetPositionAndRotation(newPosition.position,newPosition.rotation);
        }
    }
}

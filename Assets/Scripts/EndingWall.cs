using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndingWall : MonoBehaviour
{
    public GameObject audioClip;
    public GameObject postFX;
    public GameObject fakeFog;
    public GameObject wallCollider;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            audioClip.SetActive(false);
            fakeFog.SetActive(true);
            postFX.SetActive(false);
            wallCollider.GetComponent<Collider>().enabled = true;
            GetComponent<Collider>().enabled = false;
        }
    }
}

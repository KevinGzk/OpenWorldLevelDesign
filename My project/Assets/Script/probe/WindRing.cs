using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

public class WindRing : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            other.GetComponent<ThirdPersonController>().WindEffect();
        }
    }
}

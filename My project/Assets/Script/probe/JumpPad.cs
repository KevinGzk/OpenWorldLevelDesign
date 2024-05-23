using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using StarterAssets;

public class JumpPad : MonoBehaviour
{
    //when player enter, give a force upward in the direction of  jumppad
    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            other.GetComponent<ThirdPersonController>().JumpPadJump();
        }
    }

}

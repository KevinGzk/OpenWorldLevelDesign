using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using StarterAssets;

public class GlidePickUp : MonoBehaviour
{
    private bool canInteract = false;

    void Start()
    {
    }

    void Update()
    {
        pickUpInteract();
    }
    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            
            canInteract = true;
            
        }
    }

    void pickUpInteract()
    {
        if(canInteract)
        {
            if(Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("pressed E");
                
                GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>().SetGlidable(true);
                canInteract = false;
                    
                Destroy(this.gameObject, 0.5f);
            }
        }
    }
}

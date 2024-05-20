using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    GameObject interactable;
    GameObject thisInteractable;
    public bool canInteract = false;
    public bool haveInteracted = false;
    public Transform rotationCenter; // Assign in the Unity Editor to specify the center of rotation
    public float rotationAngle = 90f; // The angle to rotate around the center

    // Start is called before the first frame update
    void Start()
    {
        interactable = this.gameObject;
        thisInteractable = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        chestInteract();
        puzzleInteract();
    }

    void OnTriggerEnter(Collider collider)
    {
        if(collider.tag == "Player")
        {
            canInteract = true;
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if(collider.tag == "Player")
        {
            canInteract = false;
        }
    }

    void chestInteract()
    {
        if(canInteract)
        {
            if(interactable.tag == "Chest")
            {
                if(Input.GetKeyDown(KeyCode.E))
                {
                    canInteract = false;
                    Destroy(thisInteractable, 0.5f);
                }
            }
        }
    }

    void puzzleInteract()
    {
        if(canInteract)
        {
            if(interactable.tag == "Puzzle")
            {
                if(Input.GetKeyDown(KeyCode.E))
                {
                    canInteract = false;
                    haveInteracted = true;
                    // Use RotateAround for rotating around a specific point
                    if (rotationCenter != null)
                    {
                        thisInteractable.transform.RotateAround(rotationCenter.position, Vector3.up, rotationAngle);
                    }
                    else
                    {
                        Debug.LogWarning("Rotation Center not assigned for " + thisInteractable.name);
                    }
                }
            }
        }
    }
}


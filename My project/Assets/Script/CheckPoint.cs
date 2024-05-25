using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    bool interactable = false;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        if (interactable && Input.GetKeyDown(KeyCode.E))
        {
            GameManager.instance.CheckPointUpdate(gameObject);
            //change the color of the checkpoint to show that it has been activated
            GetComponent<Renderer>().material.color = Color.green;
            interactable = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            interactable = true;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Vector3 savePoint;
    public static GameManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        savePoint = GameObject.FindGameObjectWithTag("Player").transform.position;
    }

    public void CheckPointUpdate(GameObject gameObject)
    {
        savePoint = gameObject.transform.position;
    }

    public void PlayerRespawn()
    {
        Debug.Log("Player Respawned");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<CharacterController>().enabled = false;
        player.transform.position = savePoint;
        player.GetComponent<CharacterController>().enabled = true;
    }
}

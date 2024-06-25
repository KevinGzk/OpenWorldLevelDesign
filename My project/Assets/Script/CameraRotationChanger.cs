using UnityEngine;

public class CameraRotationChanger : MonoBehaviour
{
    public float desiredRotationX = -90f; // The desired X rotation angle
    public Transform playerCamera; // Reference to the player's camera transform

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 newRotation = playerCamera.rotation.eulerAngles;
            newRotation.x = desiredRotationX;
            playerCamera.rotation = Quaternion.Euler(newRotation);
        }
    }
}


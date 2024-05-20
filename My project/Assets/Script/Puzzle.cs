using System.Collections.Generic;
using UnityEngine;

public class Puzzle : MonoBehaviour
{
    public GameObject puzzleInteractable;
    public List<GameObject> objectsToMove; // Assign in the Inspector
    public Transform rotationCenter; // Assign in the Inspector for rotation center
    public float rotationAngle = 90f;
    public float rotationSpeed = 90f;
    public float moveHeight = 2f; // Desired height to move the objects
    public float moveSpeed = 1f; // Speed at which objects move upwards

    private bool isRotating = false;
    private bool isMoving = false;
    private Quaternion targetRotation;
    private List<Vector3> originalPositions; // To store original positions of objects

    private float totalRotation = 0f; // Total rotation applied


    void Start()
    {
        targetRotation = transform.rotation;
        originalPositions = new List<Vector3>();
        if (objectsToMove.Count > 0)
        {
            foreach (GameObject obj in objectsToMove)
            {
                originalPositions.Add(obj.transform.position); // Store the original positions
            }
        }
    }

    void Update()
    {
        if (puzzleInteractable.GetComponent<Interactable>().haveInteracted && !isRotating && !isMoving)
        {
            if (rotationAngle > 0 && rotationCenter != null) // Check if rotation is needed and a center is provided
            {
                StartRotationAroundCenter();
            }
            else
            {
                StartMovingObjects(); // Move objects immediately if no rotation is needed
            }
        }

        HandleRotation();
        HandleMovingObjects();
    }

    void StartRotationAroundCenter()
    {
    isRotating = true;
    totalRotation = 0f; // Reset total rotation
    puzzleInteractable.GetComponent<Interactable>().haveInteracted = false; // Reset interaction flag
    }


    void HandleRotation()
    {
        if (isRotating)
        {
            float step = rotationSpeed * Time.deltaTime;
            transform.RotateAround(rotationCenter.position, Vector3.up, step);
            totalRotation += step;

            // Stop rotating once we've rotated the desired amount
            if (totalRotation >= rotationAngle)
            {
                isRotating = false; // Rotation complete
                totalRotation = 0f; // Reset total rotation for next time

                // Correct any overshoot
                float overshoot = totalRotation - rotationAngle;
                transform.RotateAround(rotationCenter.position, Vector3.up, -overshoot);

                if (objectsToMove.Count > 0) // Check if there are objects to move
                {
                    StartMovingObjects();
                }
            }
        }
    }


    void StartMovingObjects()
    {
        isMoving = true; // Indicate that objects are now moving
    }

    void HandleMovingObjects()
    {
        if (isMoving && objectsToMove.Count > 0)
        {
            bool allMoved = true;
            for (int i = 0; i < objectsToMove.Count; i++)
            {
                Vector3 targetPosition = originalPositions[i] + new Vector3(0, moveHeight, 0);
                objectsToMove[i].transform.position = Vector3.MoveTowards(objectsToMove[i].transform.position, targetPosition, moveSpeed * Time.deltaTime);

                if (Vector3.Distance(objectsToMove[i].transform.position, targetPosition) > 0.1f)
                {
                    allMoved = false; // If any object hasn't reached its target, we're not done
                }
            }

            if (allMoved)
            {
                isMoving = false; // All objects have moved to their target positions
            }
        }
    }
}


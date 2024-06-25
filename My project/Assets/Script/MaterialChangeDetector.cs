using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MaterialChangeDetector : MonoBehaviour
{
    public Transform player;
    private MaterialChange lastMaterial;

    void Update()
    {
        DetectPlayerObstructions();
    }

    void DetectPlayerObstructions()
    {
        Vector3 direction = (player.position - Camera.main.transform.position).normalized;
        RaycastHit raycastHit;

        // Draw the ray in the Scene view
        Debug.DrawRay(Camera.main.transform.position, direction * 100, Color.red);

        if (Physics.Raycast(Camera.main.transform.position, direction, out raycastHit, Mathf.Infinity))
        {
            MaterialChange materialChangeObject = raycastHit.collider.gameObject.GetComponent<MaterialChange>();

            if (materialChangeObject)
            {
                materialChangeObject.SetTransparent();
                lastMaterial = materialChangeObject;
            }
            else
            {
                if (lastMaterial)
                {
                    lastMaterial.SetNormal();
                    lastMaterial = null;
                }
            }
        }
        else
        {
            if (lastMaterial)
            {
                lastMaterial.SetNormal();
                lastMaterial = null;
            }
        }
    }
}


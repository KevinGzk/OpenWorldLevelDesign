using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialChange : MonoBehaviour
{
    
    public Material transparentColor;
    private Material m_InitialColor;

    // Start is called before the first frame update
    void Start()
    {
        m_InitialColor = GetComponent<Renderer>().material;

    }

    public void SetTransparent()
    {
        GetComponent<Renderer>().material = transparentColor;
    }

    public void SetNormal()
    {
        GetComponent<Renderer>().material = m_InitialColor;
    }
}

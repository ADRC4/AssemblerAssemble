using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewColliding : MonoBehaviour {

    public static bool collided = false;

    public Material trueMaterial;
    public Material falseMaterial;

    void Start ()
    {
        this.GetComponent<MeshRenderer>().material = trueMaterial;
    }
    void OnTriggerStay(Collider other)
    {      
        collided = true;
        this.GetComponent<MeshRenderer>().material = falseMaterial;
    }
    void OnTriggerExit (Collider other)
    {    
        collided = false;
        this.GetComponent<MeshRenderer>().material = trueMaterial;
    }
    
}

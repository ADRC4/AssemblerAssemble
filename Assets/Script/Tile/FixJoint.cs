using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixJoint : MonoBehaviour
{
    public Color foregroundColor;
    public Color backgroundColor;

    public int breakForce = 10000;
    public int breakTorque = 10000;

    private List<GameObject> collidedObject = new List<GameObject>();
    private List<Joint> jointList = new List<Joint>();
    public static List<Joint> jointListPub = new List<Joint>();
    Joint joint;
    public static float reactionForce;
    public static float reactionTorque;

    bool hasJoint;

    bool gravity = false;

    Vector3 tilePos;
    Quaternion tileRot;


    private void Start()
    {
        //foregroundColor = Color.green;
        //backgroundColor = Color.red;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "PreviewTile" || collision.gameObject.tag == "Ground")
        {
            if (!collidedObject.Contains(collision.gameObject) )
            {
                collidedObject.Add(collision.gameObject);

                foreach (var colObj in collidedObject)
                {
                    if (collision.gameObject.GetComponent<Rigidbody>() != null  &&  jointList.Count < collidedObject.Count)
                    {
                        
                        joint = gameObject.AddComponent(typeof(FixedJoint)) as FixedJoint;
                        joint.breakForce = breakForce;
                        joint.breakTorque = breakTorque;
                        jointList.Add(joint);
                        jointListPub.Add(joint);
                    }
                    if(joint != null)
                    joint.connectedBody = colObj.GetComponent<Rigidbody>();
                }
            }       
        }
    }

    void Update()
    {
        if (gravity == false)
        {
            tilePos = this.gameObject.transform.position;
            tileRot = this.gameObject.transform.rotation;           
        }


        if (Input.GetKey(KeyCode.G))
        {    
            gameObject.GetComponent<Rigidbody>().isKinematic = false;
            CalculateForce();
            DisplayColor(reactionForce);
            gravity = true;
                
        }
        else if (Input.GetKey(KeyCode.H))
        {
            gameObject.GetComponent<Rigidbody>().isKinematic = false;
            CalculateForce();
            DisplayColor(reactionTorque);
            gravity = true;
        }
        else
        {
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
            gravity = false;
            this.gameObject.transform.position = tilePos;
            this.gameObject.transform.rotation = tileRot;
            collidedObject.Clear();
        }

    }


    void CalculateForce()
    {
        if (joint != null)
        {
            reactionForce = Mathf.Clamp(joint.currentForce.magnitude, 0.0f, 70.0f);
            reactionTorque = Mathf.Clamp(joint.currentTorque.magnitude, 0.0f, 70.0f);
        }
    }

    void DisplayColor(float forcetype)
    {        
        var tileRenderer = this.GetComponent<Renderer>();
        Material tileMaterial = new Material(tileRenderer.sharedMaterial);

        float colourPercent = forcetype /70;

        tileMaterial.color = Color.Lerp(foregroundColor, backgroundColor, colourPercent);
        tileRenderer.sharedMaterial = tileMaterial;
    }

}




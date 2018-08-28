using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastCreate : MonoBehaviour
{
    public GameObject blockPrefab;
    public GameObject blockPreviewPrefab;
    RaycastHit hit;
    
    int hozLayerMask = 1 << 8;
    int verXLayerMask = 1 << 9;
    int verZLayerMask = 1 << 10;
    int basePlaneLayerMask = 1 << 12;

    Quaternion rotH = Quaternion.identity;
    Quaternion rotVx = Quaternion.Euler(0, 0, 90);
    Quaternion rotVz = Quaternion.Euler(90, 0, 0);

    Vector3 buildPos;
    Vector3 distance;
    Quaternion buildRot;
    int tileLayer;

    bool buildMode = false;
    bool buildUp = false;

    public static List<GameObject> tileList = new List<GameObject>();

    private GameObject previewTile;

    Vector3 offsetG;
    int tileState = 1;

    void Start()
    {
        
    }
    
	void Update ()
    {

        if (Input.GetKeyDown(KeyCode.B))
        {
            buildMode = !buildMode;
            MainController._toggleBuildMode = buildMode;
            if (buildMode) previewTile = Instantiate(blockPreviewPrefab, buildPos, Quaternion.identity);
            else { if (previewTile != null) Destroy(previewTile); }
        }

        if (buildMode)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            hit = new RaycastHit();

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                previewTile.transform.rotation = rotH;
                tileState = 1;
                offsetG = Vector3.zero;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                previewTile.transform.rotation = rotH;
                tileState = 2;
                offsetG = Vector3.zero;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                previewTile.transform.rotation = rotVz;
                tileState = 3;
                offsetG = Vector3.forward * 0.4f;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                previewTile.transform.rotation = rotVz;
                tileState = 4;
                offsetG = Vector3.back * 0.4f;
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                previewTile.transform.rotation = rotVx;
                tileState = 5;
                offsetG = Vector3.left * 0.4f;
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                previewTile.transform.rotation = rotVx;
                tileState = 6;
                offsetG = Vector3.right * 0.4f;
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                buildUp = !buildUp;
                MainController._togglePath = buildUp;
            }

            if (buildUp)
            {

                if (previewTile.transform.rotation == rotH)
                {
                    if (Physics.Raycast(ray, out hit, 300, hozLayerMask)) PreviewTile(0.2f, 1.0f, 1.0f, 8, Vector3.zero, Vector3.zero, Vector3.zero);

                    else if (Physics.Raycast(ray, out hit, 300, verXLayerMask))
                    {
                        if (tileState == 1) PreviewTile(0.6f, 0.6f, 1.0f, 8, Vector3.left * 0.4f, Vector3.up * 0.4f, Vector3.zero);
                        else if (tileState == 2) PreviewTile(0.6f, 0.6f, 1.0f, 8, Vector3.right * 0.4f, Vector3.down * 0.4f, Vector3.zero);
                    }

                    else if (Physics.Raycast(ray, out hit, 300, verZLayerMask))
                    {
                        if (tileState == 1) PreviewTile(0.6f, 1.0f, 0.6f, 8, Vector3.forward * 0.4f, Vector3.zero, Vector3.up * 0.4f);
                        if (tileState == 2) PreviewTile(0.6f, 1.0f, 0.6f, 8, Vector3.back * 0.4f, Vector3.zero, Vector3.down * 0.4f);
                    }
                }

                else if (previewTile.transform.rotation == rotVx)
                {
                    if (Physics.Raycast(ray, out hit, 300, hozLayerMask)) PreviewTile(0.6f, 0.6f, 1.0f, 9, offsetG, Vector3.zero, Vector3.zero);

                    else if (Physics.Raycast(ray, out hit, 300, verXLayerMask)) PreviewTile(1.0f, 0.2f, 1.0f, 9, Vector3.zero, Vector3.zero, Vector3.zero);

                    else if (Physics.Raycast(ray, out hit, 300, verZLayerMask))
                    {
                        if (tileState == 5) PreviewTile(1.0f, 0.6f, 0.6f, 9, Vector3.zero, Vector3.forward * 0.4f, Vector3.left * 0.4f);
                        if (tileState == 6) PreviewTile(1.0f, 0.6f, 0.6f, 9, Vector3.zero, Vector3.back * 0.4f, Vector3.right * 0.4f);
                    }
                }

                else if (previewTile.transform.rotation == rotVz)
                {
                    if (Physics.Raycast(ray, out hit, 300, hozLayerMask)) PreviewTile(0.6f, 1.0f, 0.6f, 10, offsetG, Vector3.zero, Vector3.zero);

                    else if (Physics.Raycast(ray, out hit, 300, verXLayerMask))
                    {
                        if (tileState == 3) PreviewTile(1.0f, 0.6f, 0.6f, 10, Vector3.zero, Vector3.forward * 0.4f, Vector3.left * 0.4f);
                        if (tileState == 4) PreviewTile(1.0f, 0.6f, 0.6f, 10, Vector3.zero, Vector3.back * 0.4f, Vector3.right * 0.4f);
                    }

                    else if (Physics.Raycast(ray, out hit, 300, verZLayerMask)) PreviewTile(1.0f, 1.0f, 0.2f, 10, Vector3.zero, Vector3.zero, Vector3.zero);
                }

                //if (Physics.Raycast(ray, out hit, 300, hozLayerMask)) PreviewTile2(8);
                //if (Physics.Raycast(ray, out hit, 300, verXLayerMask)) PreviewTile2(9);
                //if (Physics.Raycast(ray, out hit, 300, verZLayerMask)) PreviewTile2(10);


            }
            else
            {
                if (Physics.Raycast(ray, out hit, 300, basePlaneLayerMask))
                {
                    if (previewTile.transform.rotation == rotH) PreviewTileGround(0.1f, 8);

                    else if (previewTile.transform.rotation == rotVx) PreviewTileGround(0.5f, 9);

                    else if (previewTile.transform.rotation == rotVz) PreviewTileGround(0.5f, 10);
                }
            }

            if (PreviewColliding.collided != true)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    PlaceBlock();

                }
            }
        }


    }

    void PreviewTile(float scaleH, float scaleVx, float scaleVz, int layer, Vector3 offsetH, Vector3 offsetVx, Vector3 offsetVz)
    {
        float scale = 1.0f;
        Vector3 offset;
        if (hit.normal == Vector3.up || hit.normal == Vector3.down)
        {
            scale = scaleH;
            offset = offsetH;
        }
        else if (hit.normal == Vector3.left || hit.normal == Vector3.right)
        {
            scale = scaleVx;
            offset = offsetVx;
        }
        else
        {
            scale = scaleVz;
            offset = offsetVz;
        }
        previewTile.transform.position = hit.transform.position + (hit.normal * scale) + offset;        
        tileLayer = layer;
    }

    //void PreviewTile2(int layer)
    //{
    //    float scale2 = 1.0f;
    //    Vector3 offset = Vector3.zero;

    //    if (hit.point.x - hit.transform.position.x < 0.2f || hit.point.y - hit.transform.position.y < 0.2f || hit.point.z - hit.transform.position.z < 0.2f)
    //    {
    //        if (previewTile.transform.rotation == hit.transform.rotation)
    //        {
    //            scale2 = 0.2f;
    //        }
    //        else
    //        {
    //            scale2 = 0.6f;
    //            //if (hit.normal.x ==0 || hit.normal.z ==0 ) offset = Vector3.right * 0.4f;                
    //        }
    //    }
    //    else
    //    {
    //        if (previewTile.transform.rotation == hit.transform.rotation)
    //            scale2 = 1.0f;
    //        else
    //            scale2 = 0.6f;
    //    }
    //    previewTile.transform.position = hit.transform.position + (hit.normal * scale2);
    //    tileLayer = layer;
    //}

    void PreviewTileGround (float previewPosY, int layer)
    {        
        var coord = hit.textureCoord * 60;
        previewTile.transform.position = new Vector3(-((int)coord.x + 0.5f - 30), previewPosY, -((int)coord.y + 0.5f - 30)) + offsetG;
        tileLayer = layer;
    }

    void PlaceBlock()
    {
        buildPos = previewTile.transform.position;
        buildRot = previewTile.transform.rotation;
        var tile = Instantiate(blockPrefab, buildPos, buildRot);
        tile.layer = tileLayer;
        tileList.Add(tile);
    }

 

}
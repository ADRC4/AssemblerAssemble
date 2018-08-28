using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateStack : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject robotPrefab;
    public List<GameObject> tileList = new List<GameObject>();
    public static int pickTileNumber = 200;
    public GameObject robot1;
    //public GameObject robot2;
    Vector3 halfSize;
    Vector3 origin;
    GameObject pickStack;
    public void Start()
    {
        halfSize = new Vector3(0.25f, 0.05f, 0.25f);

        pickStack = this.gameObject;
        origin = pickStack.transform.position;

        int direction = 1;
        if (origin.x < 0) direction = -1;
        Quaternion pickTileRot = Quaternion.Euler(0, 0, 90);

        for (int i = 0; i < 50; i++)
            for (int j = 0; j < 4; j++)
            {
                Vector3 pickTilePos = new Vector3(origin.x + (i * halfSize.y * 2 + halfSize.x * 4) * direction,
                                                  -j * halfSize.x * 2 + halfSize.x * 7,
                                                  origin.z);
                var pickTile = Instantiate(tilePrefab, pickTilePos, pickTileRot);
                tileList.Add(pickTile);
                pickTile.transform.parent = pickStack.transform;
            }

        Vector3 robotPos1 = new Vector3(origin.x + (halfSize.y * 3 + halfSize.x * 3) * direction, halfSize.x, origin.z);
        robot1 = Instantiate(robotPrefab, robotPos1, pickTileRot);
        robot1.transform.parent = pickStack.transform;
        //var robot2 = Instantiate(robotPrefab, robotPos1 + Vector3.up * halfSize.x * 2, pickTileRot);
        //robot2.transform.parent = pickStack.transform;
        //robot2.name = "robot2";
    }


}

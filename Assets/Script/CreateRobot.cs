using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateRobot : MonoBehaviour
{
    public GameObject pickingStack;    

    public static List<GameObject> _pickingStacks = new List<GameObject>();
    public static List<GameObject> _pickTiles = new List<GameObject>();
    
    public static int pickTileNumber;
    public static int placeTileNumber;
    public static GameObject[] previewTileList;
    public static GameObject[] placeTileList;

    AutoMovement autoMove;

    Vector3 stackOrigin = new Vector3(-15.5f, 0.1f, -0.5f);

    Vector3 pickingStackPos;

	void Start ()
    {
        pickingStackPos = stackOrigin;
	}

    int s;
    int n;
    int tileCount;
    Vector3 stackPos;
    Quaternion[] stackRot;

    void Update ()
    {
		previewTileList = GameObject.FindGameObjectsWithTag("PreviewTile");

        int gridCount = MainController.gridCount;

        pickTileNumber = previewTileList.Length + (gridCount * 2);

        placeTileList = GameObject.FindGameObjectsWithTag("Tile");
        placeTileNumber = placeTileList.Length;

        s = pickTileNumber / 200;

        n = pickTileNumber % 200;
        
        stackRot = new Quaternion[s];
        
        if (Input.GetKeyDown(KeyCode.B) && _pickingStacks.Count == 0) SpawnStack(0, stackOrigin, Quaternion.identity, 100);



         if (_pickingStacks.Count < s)
        {
            stackOrigin = stackOrigin + Vector3.back * s/2;
  
            for (int i = 0; i < s; i++)
            {
                if (i <= s/2)
                {
                    stackPos = stackOrigin;
                    stackRot[i] = Quaternion.identity;
                }
                else
                {
                    stackPos = new Vector3(-stackOrigin.x, stackOrigin.y, stackOrigin.z - s);
                    stackRot[i] = Quaternion.Euler(0, 180, 0);
                }

                if (i == s - 1) tileCount = n;
                else tileCount = 200;
  
                SpawnStack(i, stackPos, stackRot[i], tileCount);
            }     
        }

    }

    void SpawnStack(int index, Vector3 stackOrigin, Quaternion stackOrientation, int tileCount)
    { 
        pickingStackPos = stackOrigin + Vector3.forward * 2 * index;
        var stack = Instantiate(pickingStack, pickingStackPos, Quaternion.identity);
        var auto = stack.GetComponent<AutoMovement>();
        auto.SpawnPickRobot();
        auto.SpawnPickTile(tileCount);

        auto.stackIndex = index;
        stack.transform.rotation = stackOrientation;
        _pickingStacks.Add(stack);
    }

}


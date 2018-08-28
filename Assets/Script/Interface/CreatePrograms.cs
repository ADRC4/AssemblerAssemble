using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatePrograms : MonoBehaviour
{
    //Info
    public GUISkin skin;

    public GUISkin Program01;

    public Texture boundaryTexture;
    public Texture[] programTexture = new Texture[10];
    public Texture lineTexture;

    //Slider
    public GUISkin Slider;

    GUIStyle styleLeft = new GUIStyle();
    GUIStyle styleMiddle = new GUIStyle();
    GUIStyle styleRight = new GUIStyle();

    public GameObject boundaryPrefab;

    public static Bounds bounds = new Bounds();

    Vector3 boundSize;

    string boundSizeStringX = "7.5"; string boundSizeStringY = "8.5"; string boundSizeStringZ = "14";

    public Transform program;

    public GameObject pointPrefab;

    public GameObject floorPrefab;

    public string[] programsName = new string[8];

    public GameObject[] programPrefab = new GameObject[8];

    public Color[] programColor = new Color[8];

    public Vector3[] programPos = new Vector3[8];

    public Vector3[] programScl = new Vector3[8];

    GameObject[] programList;
    GameObject[] programsPointList;
    GameObject[] programsSlabList;

    GameObject programs;    
    
    GameObject boundary;    

    public static GameObject SelectedProgram;

    float scaleX;
    float scaleY;
    float scaleZ;

    int scaleXMin, scaleXMax;
    int scaleYMin, scaleYMax;
    int scaleZMin, scaleZMax;

    int pCount;

    int placeTileCount;
    int pickTileCount;
    int activeRobotCount;
    int totalRobotCount;

    int jointList;

    int assemblyHour, assemblyMin;

    
    void Start ()
    {
        pCount = programsName.Length;
        programList = new GameObject[pCount];
        programsPointList = new GameObject[pCount];
        programsSlabList = new GameObject[pCount];
    }

    void SpawnBoundary()
    {
        boundSize = new Vector3(float.Parse(boundSizeStringX) , float.Parse(boundSizeStringY) , float.Parse(boundSizeStringZ));
        boundary = Instantiate(boundaryPrefab, Vector3.zero, Quaternion.identity);
        boundary.transform.localScale = boundSize;
        boundary.transform.position = Vector3.zero + Vector3.up * (boundSize.y / 2);
        boundary.transform.SetParent(this.transform);
    }

    void SpawnPrograms(GameObject prefab, Vector3 prefabPosition, Color color, float density, Vector3 scale, int index)
    {        
        programs = Instantiate(prefab, prefabPosition, Quaternion.identity);
        programs.transform.localScale = scale;
        programs.transform.position = programs.transform.position + Vector3.up * (scale.y/2);
        programs.GetComponent<Renderer>().material.color = color;
        programs.transform.SetParent(this.transform);
        programList[index] = programs;
    }

    GameObject programsPoint;
    void SpawnProgramsPoint(Vector3 prefabPosition, Color color, float density, int index)
    {
        programsPoint = Instantiate(pointPrefab, prefabPosition, Quaternion.identity);        
        programsPoint.GetComponent<Renderer>().material.color = color;
        programsPoint.transform.SetParent(this.transform);
        programsPointList[index] = programsPoint;
    }

    GameObject programsSlab;
    void SpawnSlab(Vector3 prefabPosition, Color color, Vector3 scale, int index)
    {
        programsSlab = Instantiate(floorPrefab, prefabPosition, Quaternion.Euler(90,0,0));
        programsSlab.transform.localScale = scale;
        programsSlab.GetComponent<Renderer>().material.color = color;
        programsSlab.transform.SetParent(program);
        programsSlabList[index] = programsSlab;
    }

    
    void Update()
    {        
        placeTileCount = PathFinding.placeTiles.Count;       
        pickTileCount = (CreateStack.pickTileNumber) * PathFinding.startFaces.Count;
        activeRobotCount = PathFinding.allPaths.Count;
        totalRobotCount = PathFinding.startFaces.Count*2;
        jointList = placeTileCount*2;

        if (boundary != null && programsPoint == null)
        {
                boundary.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                boundary.transform.position = new Vector3(boundary.transform.position.x, scaleY / 2, boundary.transform.position.z);

                bounds.center = boundary.transform.position;
                bounds.extents = boundary.transform.localScale / 2;
                
                boundSizeStringX = bounds.extents.x.ToString();
                boundSizeStringY = bounds.extents.y.ToString();
                boundSizeStringZ = bounds.extents.z.ToString();
        }
        else if (programsPoint != null)
        {
                programsPoint.transform.position = new Vector3(scaleX, scaleY, scaleZ);
                programsSlab.transform.position = new Vector3(scaleX, scaleY -3, scaleZ);
        }

        //if (programs != null)
        //{
        //    programs.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        //    programs.transform.position = new Vector3 (programs.transform.position.x, scaleY / 2, programs.transform.position.z);
        //}

        assemblyMin = (placeTileCount * 5) % 60;
        assemblyHour = (placeTileCount * 5) / 60;
    }

    void OnGUI()
    {
        styleLeft.fontSize = 13;
        styleMiddle.fontSize = 13;
        styleRight.fontSize = 13;
        styleMiddle.alignment = TextAnchor.MiddleCenter;
        styleRight.alignment = TextAnchor.MiddleRight;

        GUI.skin = Program01;

        if (GUI.Button(new Rect(Screen.width - 240f, 25, 90, 90), boundaryTexture))
        {
            if (boundary == null)
            {
                SpawnBoundary();
                scaleX = boundSize.x; scaleY = boundSize.y; scaleZ = boundSize.z;
                scaleXMin = (int)boundSize.x - 5; scaleXMax = (int)boundSize.x + 5;
                scaleYMin = (int)boundSize.y - 5; scaleYMax = (int)boundSize.y + 5;
                scaleZMin = (int)boundSize.z - 5; scaleZMax = (int)boundSize.z + 5;
            }
            else
            {
                scaleX = boundSize.x; scaleY = boundSize.y; scaleZ = boundSize.z;
                programsPoint = null;
            }
        }

        GUI.Label(new Rect(Screen.width - 150f, 50, 100, 20), "Boundary", styleRight);

        boundSizeStringX = GUI.TextField(new Rect(Screen.width - 70f, 75, 25, 20), boundSizeStringX);
        boundSizeStringY = GUI.TextField(new Rect(Screen.width - 100f, 75, 25, 20), boundSizeStringY);
        boundSizeStringZ = GUI.TextField(new Rect(Screen.width - 130f, 75, 25, 20), boundSizeStringZ);

        //for (int i = 0; i < pCount; i++)
        //{
        //    if (GUI.Button(new Rect(Screen.width - 180f, 65 * i + 140, 60, 60), programTexture[i]))
        //    {
        //        if (programList[i] == null)
        //        {
        //            scaleX = programScl[i].x; scaleY = programScl[i].y; scaleZ = programScl[i].z;

        //            scaleXMin = (int)programScl[i].x - 2; scaleXMax = (int)programScl[i].x + 2;
        //            scaleYMin = (int)programScl[i].y - 2; scaleYMax = (int)programScl[i].y + 2;
        //            scaleZMin = (int)programScl[i].z - 2; scaleZMax = (int)programScl[i].z + 2;
        //            SpawnPrograms(programPrefab[i], Vector3.zero, programColor[i], Random.Range(30, 50), Vector3.one, i);
        //        }
        //        else
        //        {
        //            programs = programList[i];
        //        }
        //    }
        //    GUI.Label(new Rect(Screen.width - 150f, 65 * i + 150, 100, 20), programsName[i], styleRight);
        //    GUI.DrawTexture(new Rect(Screen.width - 177f, 65 * i + 145, 125, 55), lineTexture);
        //}

        for (int i = 0; i < pCount; i++)
        {
            if (GUI.Button(new Rect(Screen.width - 180f, 65 * i + 140, 60, 60), programTexture[i]))
            {
                if (programsPointList[i] == null)
                {
                    scaleX = programPos[i].x; scaleY = programPos[i].y; scaleZ = programPos[i].z;
                    var pos = new Vector3(scaleX, scaleY, scaleZ);
                    scaleXMin = (int)(bounds.center.x - bounds.extents.x); scaleXMax = (int)(bounds.center.x + bounds.extents.x);
                    scaleYMin = (int)(bounds.center.y - bounds.extents.y); scaleYMax = (int)(bounds.center.y + bounds.extents.y);
                    scaleZMin = (int)(bounds.center.z - bounds.extents.z); scaleZMax = (int)(bounds.center.z + bounds.extents.z);
                    SpawnProgramsPoint(pos, programColor[i], 1, i);
                    SpawnSlab(pos + Vector3.down * 3, programColor[i], programScl[i], i);
                    if (i == 8) programsSlabList[8].transform.rotation = Quaternion.Euler(60, 90, 90);

                }
                else
                {
                    programsPoint = programsPointList[i];
                    programsSlab = programsSlabList[i];
                    boundary = null;
                }
            }
            GUI.Label(new Rect(Screen.width - 150f, 65 * i + 150, 100, 20), programsName[i], styleRight);
            GUI.DrawTexture(new Rect(Screen.width - 177f, 65 * i + 145, 125, 55), lineTexture);
        }

        int s = 0;
        int textOffset = 40;
        int textOffsetV = 280;
        int buttonOffset = 101;
        int buttonOffsetV = 179;
        int buttonSize = 100;
        int gap = 70;
        
               
        GUI.Label(new Rect(textOffset, textOffsetV + gap * s++, 100, 20), "Tiles: ", styleLeft);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s - 10, buttonSize, buttonSize), placeTileCount.ToString() , styleMiddle);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s +10, buttonSize, buttonSize), pickTileCount.ToString(), styleMiddle);

        GUI.Label(new Rect(textOffset, textOffsetV + gap * s++, 100, 20), "Robots: ", styleLeft);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s -10, buttonSize, buttonSize), activeRobotCount.ToString(), styleMiddle);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s +10, buttonSize, buttonSize), totalRobotCount.ToString(), styleMiddle);

        GUI.Label(new Rect(textOffset, textOffsetV + gap * s++, 100, 20), "Assembly: ", styleLeft);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s -10, buttonSize, buttonSize), assemblyHour.ToString() + " h", styleMiddle);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s + 10, buttonSize, buttonSize), assemblyMin.ToString() + " m", styleMiddle);

        GUI.Label(new Rect(textOffset, textOffsetV + gap * s++, 100, 20), "Weight: ", styleLeft);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s - 10, buttonSize, buttonSize), (placeTileCount * 3).ToString(), styleMiddle);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s + 10, buttonSize, buttonSize), "kg", styleMiddle);

        GUI.Label(new Rect(textOffset, textOffsetV + gap * s++, 100, 20), "Joint: ", styleLeft);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s, buttonSize, buttonSize), jointList.ToString(), styleMiddle); 

        GUI.Label(new Rect(textOffset, textOffsetV + gap * s++, 100, 20), "Stress: ", styleLeft);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s, buttonSize, buttonSize), FixJoint.reactionForce.ToString(), styleMiddle);

        GUI.Label(new Rect(textOffset, textOffsetV + gap * s++, 100, 20), "Tension: ", styleLeft);
        GUI.Label(new Rect(buttonOffset, buttonOffsetV + gap * s, buttonSize, buttonSize), FixJoint.reactionTorque.ToString(), styleMiddle);

        GUI.skin = Slider;

        scaleX = (GUI.HorizontalSlider(new Rect(Screen.width - 225, Screen.height - 90, 200, 20), scaleX, scaleXMin, scaleXMax));
        scaleY = (GUI.HorizontalSlider(new Rect(Screen.width - 225, Screen.height - 70, 200, 20), scaleY, scaleYMin, scaleYMax));
        scaleZ = (GUI.HorizontalSlider(new Rect(Screen.width - 225, Screen.height - 50, 200, 20), scaleZ, scaleZMin, scaleZMax));
        
    }
}

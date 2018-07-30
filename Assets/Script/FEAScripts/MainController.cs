using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MainController : MonoBehaviour
{
    public Texture camTexture;
    public Texture analyseTexture;
    public Texture buildTexture;

    public Camera othorCam;
    public Camera perspCam;
    public GameObject[] tile = new GameObject[2];
    Grid3d _grid = null;
    GameObject _voids;
    GameObject _boundary;
    GameObject _program;
    Coroutine _liveUpdate;

    bool _toggleVoids = false;
    bool _toggleUpdate = false;
    bool _toggleTransparency = true;
    float _displacement = 0f;
    string _voxelSize = "1.0";
    // Task _task;


    public static List<Vector3> targetPosList = new List<Vector3>();

    public static bool _toggleCamera = true;
    public static bool _toggleBuildMode = false;
    public static bool _toggleBuildUp = false;
    public static bool _toggleSelect = false;

    [SerializeField]
    GUISkin _skin;

    void Awake()
    {
        othorCam.gameObject.SetActive(true);
        perspCam.gameObject.SetActive(false);

        _voids = GameObject.Find("Voids");
        _program = GameObject.Find("Program");

        Physics.queriesHitBackfaces = true;
    }
    int p;
    void OnGUI()
    {
        GUI.skin = _skin;

        //_voxelSize = GUI.TextField(new Rect(s * i++, 25, 100, 20), _voxelSize);



        if (GUI.Button(new Rect(20, Screen.height - 120, 80, 80), analyseTexture))
        {
            MakeGrid();

            _toggleVoids = true;

            foreach (var r in _program.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }
        GUI.Label(new Rect(30, Screen.height - 40, 100, 20), "ANALYSE");


        if (GUI.Button(new Rect(135, Screen.height - 120, 80, 80), buildTexture))
        {
            _toggleTransparency = true;

            foreach (var r in _voids.GetComponentsInChildren<Renderer>())
                r.enabled = false;

            StartCoroutine(SpawnTile());
            //SpawnTile();
        }
        GUI.Label(new Rect(155, Screen.height - 40, 90, 20), "BUILD!");


        int i = 2;
        int s = 150;

        if (_toggleUpdate != GUI.Toggle(new Rect(s * i++, Screen.height - 40, 100, 20), _toggleUpdate, "Auto update"))
        {
            _toggleUpdate = !_toggleUpdate;

            if (_toggleUpdate)
                _liveUpdate = StartCoroutine(LiveUpdate());
            else
                StopCoroutine(_liveUpdate);
        }

        if (_toggleVoids != GUI.Toggle(new Rect(s * i++, Screen.height - 40, 100, 20), _toggleVoids, "Show voids"))
        {
            _toggleVoids = !_toggleVoids;

            foreach (var r in _voids.GetComponentsInChildren<Renderer>())
                r.enabled = _toggleVoids;
        }

        _toggleTransparency = GUI.Toggle(new Rect(s * i++, Screen.height - 40, 100, 20), _toggleTransparency, "PointCloud(T)");

        foreach (var previewTile in RaycastCreate.tileList)
        {
            var render = previewTile.GetComponent<Renderer>();
            render.enabled = _toggleTransparency;
        }

        _toggleBuildMode = GUI.Toggle(new Rect(s * i++, Screen.height - 40, 100, 20), _toggleBuildMode, "BuildMode(B)");

        _toggleBuildUp = GUI.Toggle(new Rect(s * i++, Screen.height - 40, 110, 20), _toggleBuildUp, "OnStructure(V)");

        _toggleSelect = GUI.Toggle(new Rect(s * i++, Screen.height - 40, 100, 20), _toggleSelect, "Select(S)");

        if (_grid != null)
            _displacement = GUI.HorizontalSlider(new Rect(s * i++, Screen.height - 40, 200, 20), _displacement, 0, 200);

        if (GUI.Button(new Rect(Screen.width - 320, Screen.height - 65, 50, 50), camTexture) || Input.GetKeyDown(KeyCode.C))
        {
            _toggleCamera = !_toggleCamera;
            if (_toggleCamera)
            {
                othorCam.gameObject.SetActive(true);
                perspCam.gameObject.SetActive(false);
            }
            else
            {
                othorCam.gameObject.SetActive(false);
                perspCam.gameObject.SetActive(true);
            }
        }


    }

    void Update()
    {
        if (_grid == null) return;
        //_grid.DisplacementScale = _displacement;
        //Drawing.DrawMesh(_toggleTransparency, _grid.Mesh);

        foreach (var face in _grid.Faces.Where(f => f.IsClimbable))
        {
            if (face.Geometry == null)
                face.Geometry = Drawing.MakeFace(face.Center, face.Direction, _grid.VoxelSize, 1);

            Drawing.DrawMesh(false, face.Geometry);
        }

    }

    IEnumerator LiveUpdate()
    {
        while (true)
        {
            MakeGrid();
            yield return new WaitForSeconds(2.0f);
        }
    }

    public static int gridCount;   


    void MakeGrid()
    {
        //  if (_task != null && !_task.IsCompleted) return;

        var colliders = _voids
                      .GetComponentsInChildren<MeshCollider>()
                      .ToArray();

        var voxelSize = float.Parse(_voxelSize);

        _grid = new Grid3d(colliders, voxelSize);
        _grid.MakeMesh();

        _grid.TilePosition();

        gridCount = _grid.activeCenter.Count;
        _grid.activeCenter = _grid.activeCenter.OrderBy(position => Vector3.Distance(position, Vector3.zero)).ToList();

        for (int i = 0; i < gridCount; i++)
        {
            Vector3 center = _grid.activeCenter[i] + Vector3.down * 0.6f;
            targetPosList.Add(center);
        }

        //_task = Task.Run(() =>
        //{
        //    _grid = new Grid3d(colliders, bounds,voxelSize);
        //}).ContinueWith(_ =>
        //{
        //    _grid.MakeMesh();
        //}, TaskScheduler.FromCurrentSynchronizationContext());
    }

    //void SpawnTile()
    IEnumerator SpawnTile()
    {
        p = AutoMovement._pickTiles.Count;

        for (int i = 0; i < gridCount; i++)
        {
            Vector3 center = _grid.activeCenter[i];

            Vector3[] tilePos = new Vector3[6];

            tilePos[0] = center + Vector3.up * 0.4f;
            tilePos[1] = center + Vector3.down * 0.4f;
            tilePos[2] = center + Vector3.left * 0.4f;
            tilePos[3] = center + Vector3.right * 0.4f;
            tilePos[4] = center + Vector3.forward * 0.4f;
            tilePos[5] = center + Vector3.back * 0.4f;

            //targetPosList.Add(tilePos[1]);
            //targetPosList.Add(tilePos[2]);
            //targetPosList.Add(tilePos[0]);


            Quaternion[] tileRot = new Quaternion[6];

            tileRot[0] = Quaternion.identity;
            tileRot[1] = Quaternion.Euler(0, 90, 0);
            tileRot[2] = Quaternion.Euler(0, 0, 90);
            tileRot[3] = Quaternion.Euler(90, 0, 90);
            tileRot[4] = Quaternion.Euler(90, 0, 0);
            tileRot[5] = Quaternion.Euler(0, 90, 90);

            if (tilePos[0].z < 6 && tilePos[0].z > -9)
            {
                var tileTop = Instantiate(tile[0], tilePos[0], tileRot[0]);
                var tileBot = Instantiate(tile[0], tilePos[1], tileRot[0]);
                tileTop.layer = 8;
                tileBot.layer = 8;
            }
            else if (Mathf.RoundToInt(tilePos[0].z) == 6 && Mathf.RoundToInt(tilePos[0].z) == -9)
            {
                var tileTop = Instantiate(tile[1], tilePos[0], tileRot[1]);
                var tileBot = Instantiate(tile[1], tilePos[1], tileRot[1]);
                tileTop.layer = 8;
                tileBot.layer = 8;
            }
            else
            {
                var tileTop = Instantiate(tile[0], tilePos[0], tileRot[1]);
                var tileBot = Instantiate(tile[0], tilePos[1], tileRot[1]);
                tileTop.layer = 8;
                tileBot.layer = 8;
            }


            int r2 = (int)Random.Range(2, 5);

            if (r2 == 2 || r2 == 3)
            {
                if (tilePos[r2].x < -9)
                {
                    var tileVx = Instantiate(tile[0], tilePos[3], tileRot[2]);
                    tileVx.layer = 9;
                }
                else if (tilePos[r2].x > 9)
                {
                    var tileVx = Instantiate(tile[0], tilePos[2], tileRot[2]);
                    tileVx.layer = 9;
                }
                else
                {
                    var tileVx = Instantiate(tile[0], tilePos[r2], tileRot[2]);
                    tileVx.layer = 9;
                }
            }
            else
            {
                if (tilePos[r2].x < 0)
                {
                    var tileVz = Instantiate(tile[1], tilePos[r2], tileRot[4]);
                    tileVz.layer = 10;
                }
                else
                {
                    var tileVz = Instantiate(tile[1], tilePos[r2], tileRot[5]);
                    tileVz.layer = 10;
                }
            }

            // Destroy Pick Tile
            int n = Mathf.RoundToInt(p / 200);

            int v = Mathf.RoundToInt(i * 2 / (n + 2));

            for (int j = 1; j < n + 1; j++)
            {
                Destroy(AutoMovement._pickTiles[200 * j - v - 1]);
            }

            Destroy(AutoMovement._pickTiles[p - v - 1]);

            yield return new WaitForSeconds(0.001f);

        }

    }


}

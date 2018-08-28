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
    Grid3d _grid = null;
    GameObject _voids;
    GameObject _program;

    bool _toggleVoids = false;

    float _displacement = 0f;

    public static bool _toggleMesh = true;
    public static bool _toggleCamera = true;
    public static bool _toggleBuildMode = false;
    public static bool _togglePath = false;
    public static bool _toggleSelect = false;

    PathFinding pathFinding;

    [SerializeField]
    GUISkin _skin;

    void Awake()
    {
        othorCam.gameObject.SetActive(true);
        perspCam.gameObject.SetActive(false);

        _voids = GameObject.Find("Voids");
        _program = GameObject.Find("Program");

        Physics.queriesHitBackfaces = true;

        pathFinding = GetComponent<PathFinding>();
    }

    void OnGUI()
    {
        GUI.skin = _skin;

        if (GUI.Button(new Rect(20, Screen.height - 120, 80, 80), analyseTexture))
        {
            //_toggleVoids = true;
            foreach (var r in _program.GetComponentsInChildren<Renderer>())
                r.enabled = false;
            pathFinding.Analyse();
        }
        GUI.Label(new Rect(30, Screen.height - 40, 100, 20), "ANALYSE");


        if (GUI.Button(new Rect(135, Screen.height - 120, 80, 80), buildTexture))
        {
            _toggleMesh = true;
            foreach (var r in _voids.GetComponentsInChildren<Renderer>())
                r.enabled = false;
            pathFinding.Build();
        }
        GUI.Label(new Rect(155, Screen.height - 40, 90, 20), "BUILD!");


        int i = 2;
        int s = 150;

        if (_toggleVoids != GUI.Toggle(new Rect(s * i++, Screen.height - 40, 100, 20), _toggleVoids, "Show voids"))
        {
            _toggleVoids = !_toggleVoids;

            foreach (var r in _voids.GetComponentsInChildren<Renderer>())
                r.enabled = _toggleVoids;
        }

        _toggleMesh = GUI.Toggle(new Rect(s * i++, Screen.height - 40, 100, 20), _toggleMesh, "PointCloud(T)");

        foreach (var previewTile in RaycastCreate.tileList)
        {
            var render = previewTile.GetComponent<Renderer>();
            render.enabled = _toggleMesh;
        }

        _toggleBuildMode = GUI.Toggle(new Rect(s * i++, Screen.height - 40, 100, 20), _toggleBuildMode, "BuildMode(B)");

        _togglePath = GUI.Toggle(new Rect(s * i++, Screen.height - 40, 110, 20), _togglePath, "ShowPath(P)");

        _toggleSelect = GUI.Toggle(new Rect(s * i++, Screen.height - 40, 100, 20), _toggleSelect, "Select(S)");

        if (_grid != null)
            _displacement = GUI.HorizontalSlider(new Rect(s * i++, Screen.height - 40, 200, 20), _displacement, 0, 200);

        PathFinding.tumblingTime = GUI.HorizontalSlider(new Rect(s * i++, Screen.height - 40, 200, 20), PathFinding.tumblingTime, 0.5f, 0.01f);

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

}

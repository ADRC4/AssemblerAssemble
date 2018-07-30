using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using QuickGraph;

public class PathFinding : MonoBehaviour
{
    Grid3d _grid = null;
    GameObject _voids;
    bool _toggleVoids = true;
    bool _toggleTransparency = false;
    string _voxelSize = "1.0";

    [SerializeField]
    GUISkin _skin;

    int targetFace = 20;

    public GameObject robotPrefab;

    private void Awake()
    {
        _voids = GameObject.Find("Voids");
        Physics.queriesHitBackfaces = true;
    }

    private void Start()
    {
        MakeGrid();
        ToggleVoids();
    }

    void Update()
    {
        if (_grid == null) return;

        foreach (var face in _grid.Faces.Where(f => f.IsClimbable))
        {
            if (face.Geometry == null)
                face.Geometry = Drawing.MakeFace(face.Center, face.Direction, _grid.VoxelSize, 1);

            Drawing.DrawMesh(false, face.Geometry);

        }
        if(Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(MoveRobot());
        }
        RaycastSelect();
    }

    RaycastHit hit;
    Face startFace;
    Face endFace;
    void RaycastSelect()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        hit = new RaycastHit();
        if (Input.GetMouseButtonDown(0))
        {
            //Physics.queriesHitBackfaces = false;
            if (Physics.Raycast(ray, out hit, 300))
            {
                foreach (var face in _grid.Faces.Where(f => f.IsClimbable))
                {
                    if ((int)hit.point.x == (int)face.Center.x &&
                        (int)hit.point.y == (int)face.Center.y &&
                        (int)hit.point.z == (int)face.Center.z)
                    {
                        startFace = face;
                        startFace.Geometry = Drawing.MakeFace(startFace.Center, startFace.Direction, _grid.VoxelSize, 0);
                    }
                }
            }
        }
    }



    void OnGUI()
    {
        int i = 1;
        int s = 25;
        GUI.skin = _skin;

        _voxelSize = GUI.TextField(new Rect(s, s * i++, 100, 20), _voxelSize);

        targetFace = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(s, s * i++, 200, 20), targetFace, 0, 150));

        if (GUI.Button(new Rect(s, s * i++, 100, 20), "Generate"))
        {
            MakeGrid();
        }

    }

    List<Face> movePath = new List<Face>();
    void MakeGrid()
    {
        // create grid with voids
        var colliders = _voids
                      .GetComponentsInChildren<MeshCollider>()
                      .ToArray();

        var voxelSize = float.Parse(_voxelSize);
        _grid = new Grid3d(colliders, voxelSize);

        // select edges of boundary faces
        var edges = _grid.Edges.Where(e => e.ClimbableFaces.Length == 2);

        // create graph from edges
        var graphEdges = edges.Select(e => new Edge<Face>(e.ClimbableFaces[0], e.ClimbableFaces[1]));
        var graph = graphEdges.ToUndirectedGraph<Face, Edge<Face>>();

        // start face for shortest path        
        var start = _grid.Faces.Where(f => f.IsClimbable).Skip(0).First();
        var end = _grid.Faces.Where(f => f.IsClimbable).Skip(targetFace).First();

        // calculate shortest path from start face to all boundary faces
        var shortest = QuickGraph.Algorithms.AlgorithmExtensions.ShortestPathsDijkstra(graph, e => 1.0, start);

        //highlight shortest path
        IEnumerable<Edge<Face>> path;
        if (shortest(end, out path))
        {
            float t = 0f;

            start.Geometry = Drawing.MakeFace(start.Center, start.Direction, _grid.VoxelSize, t);
            movePath.Add(start);

            foreach (var edge in path)
            {
                var faceS = edge.Source;
                var faceT = edge.Target;
                faceS.Geometry = Drawing.MakeFace(faceS.Center, faceS.Direction, _grid.VoxelSize, t);
                faceT.Geometry = Drawing.MakeFace(faceT.Center, faceT.Direction, _grid.VoxelSize, t);
                movePath.Add(faceS);
                if (!movePath.Contains(faceT))
                movePath.Add(faceT);
            }

            end.Geometry = Drawing.MakeFace(end.Center, end.Direction, _grid.VoxelSize, t);
            movePath.Add(end);
        }

        ////create a mesh face for every outer face colored based on the path length
        //foreach (var face in _grid.Faces.Where(f => f.IsClimbable))
        //{
        //    float t = 1;

        //    if (face == start)
        //    {
        //        t = 1;
        //    }
        //    else if (face == end) t = 0;
        //    else
        //    {
        //        IEnumerable<Edge<Face>> path;
        //        if (shortest(face, out path))
        //        {                    
        //            t = path.Count() * 0.06f;
        //            t = Mathf.Clamp01(t);
        //        }
        //    }
        //    face.Geometry = Drawing.MakeFace(face.Center, face.Direction, _grid.VoxelSize, t);
        //}
    }

    IEnumerator MoveRobot()
    {
        var robot = Instantiate(robotPrefab, movePath[0].Center, Quaternion.identity);
        Normal direction = movePath[0].Direction;
        
        for (int i = 0; i < movePath.Count-1; i++)
        {
          robot.transform.position = movePath[i].Center;
          //robot.transform.rotation = movePath[i].Direction;
          yield return new WaitForSeconds(0.1f);
        }
        movePath.Clear();
        Destroy(robot);
    }

    void ToggleVoids()
    {
        _toggleVoids = !_toggleVoids;

        foreach (var r in _voids.GetComponentsInChildren<Renderer>())
            r.enabled = _toggleVoids;
    }
}
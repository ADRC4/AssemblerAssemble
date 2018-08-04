using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using QuickGraph;
using System;

public class PathFinding : MonoBehaviour
{
    Grid3d _grid = null;
    GameObject _voids;
    bool _toggleVoids = true; 
    bool _toggleUpdate = false;
    string _voxelSize = "1.2";
    Coroutine _liveUpdate;

    [SerializeField]
    GUISkin _skin;

    public GameObject robotPrefab;
    public GameObject tilePrefab;

    Face start;
    Face end;

    int startFace = 0;
    int endFace = 300;


    private void Awake()
    {
        _voids = GameObject.Find("Voids");
        Physics.queriesHitBackfaces = true;
    }

    private void Start()
    {
        MakeGrid();
        ToggleVoids();
        //SpawnTile();
    }


    int actualFaces = 0;

    void Update()
    {
        if (_grid == null) return;

        //   Drawing.DrawMesh(false, _grid.Mesh);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            actualFaces = 0;
            foreach (var face in _grid.Faces) face.IsUsed = false;

            var startFaces = _grid.Faces.Where(f => f.Index.y == 0 && f.Index.z %2 ==0 && f.Index.x % 2 == 0 && f.IsClimbable);
            var endFaces = _grid.Faces.Where(f => f.Center.y > 17.5f && Mathf.Abs(f.Center.x) < 1.5f && f.IsClimbable/* && !f.IsUsed*/);

            foreach (var sf in startFaces)
            {
                if (sf.IsUsed) continue;
                var ef = endFaces.OrderBy(endFace => Vector3.Distance(sf.Center, endFace.Center)).First();
                StartCoroutine(MoveRobot(sf, ef));
            }
  
        }

        if (Input.GetKey(KeyCode.Return))
        {
            foreach (var face in _grid.Faces) face.IsUsed = false;
            start = _grid.Faces.Where(f => f.IsClimbable).ToList()[startFace];
            end = _grid.Faces.Where(f => f.IsClimbable).ToList()[endFace];
            StartCoroutine(MoveRobot(start, end));
        }

        RaycastSelect();
    }

    IEnumerator LiveUpdate()
    {
        while (true)
        {
            MakeGrid();
            yield return new WaitForSeconds(2.0f);
        }
    }

    void OnGUI()
    {
        int i = 1;
        int s = 25;
        GUI.skin = _skin;

        _voxelSize = GUI.TextField(new Rect(s, s * i++, 100, 20), _voxelSize);

        startFace = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(s, s * i++, 200, 20), startFace, 0, 21));

        endFace = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(s, s * i++, 200, 20), endFace, 50, 1000));

        if (_toggleVoids != GUI.Toggle(new Rect(s, s * i++, 100, 20), _toggleVoids, "Show voids"))
        {
            ToggleVoids();
        }

    }

    void RaycastSelect()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = new RaycastHit();
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hit, 300))
            {
                foreach (var face in _grid.Faces.Where(f => f.IsClimbable))
                {
                    if ((int)hit.point.x == (int)face.Center.x &&
                        (int)hit.point.y == (int)face.Center.y &&
                        (int)hit.point.z == (int)face.Center.z)
                    {
                        start = face;
                        start.Geometry = Drawing.MakeFace(start.Center, start.Direction, _grid.VoxelSize, 0);
                    }
                }
            }
        }
    }

    UndirectedGraph<Face, TaggedEdge<Face, Edge>> graph;
    Mesh mesh;
    List<Vector3> vertices = new List<Vector3>();
    List<Vector3> meshVertices;

    void MakeGrid()
    {
        // create grid with voids
        var colliders = _voids
                      .GetComponentsInChildren<MeshCollider>()
                      .ToArray();

        var voxelSize = float.Parse(_voxelSize);
        _grid = new Grid3d(colliders, voxelSize);

        // create a mesh face for every outer face colored based on the path length (except a solid yellow path to end face)
        mesh = new Mesh()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        var meshes = new List<CombineInstance>();

        foreach (var face in _grid.Faces.Where(f => f.IsClimbable))
        {
            Mesh faceMesh;
            faceMesh = Drawing.MakeFace(face.Center, face.Direction, _grid.VoxelSize, 0);
            meshes.Add(new CombineInstance() { mesh = faceMesh });
        }

        mesh.CombineMeshes(meshes.ToArray(), true, false, false);
        GetComponent<MeshFilter>().mesh = mesh;

        meshVertices = new List<Vector3>(mesh.vertices);

        //// draw a polyline for the start-end path
        //{
        //    IEnumerable<TaggedEdge<Face, Edge>> path;
        //    if (shortest(end, out path))
        //    {
        //        var vertices = new List<Vector3>();                

        //        var current = start;
        //        vertices.Add(current.Center);

        //        foreach (var edge in path)
        //        {
        //            vertices.Add(edge.Tag.Center);
        //            current = edge.GetOtherVertex(current);
        //            vertices.Add(current.Center);
        //        }

        //        int vertexCount = mesh.vertexCount;
        //        var meshVertices = new List<Vector3>(mesh.vertices);
        //        meshVertices.AddRange(vertices);
        //        mesh.SetVertices(meshVertices);
        //        mesh.subMeshCount = 2;
        //        mesh.SetIndices(Enumerable.Range(vertexCount, meshVertices.Count - vertexCount).ToArray(), MeshTopology.LineStrip, 1);

        //    }
        //}

        this.gameObject.AddComponent<MeshCollider>();
    }

    void ToggleVoids()
    {
        _toggleVoids = !_toggleVoids;

        foreach (var r in _voids.GetComponentsInChildren<Renderer>())
            r.enabled = _toggleVoids;
    }
    int vertexCount;

    IEnumerator MoveRobot(Face start, Face end)
    {
        // select edges of boundary faces
        var edges = _grid.Edges.Where(e => e.ClimbableFaces.Length == 2 /*&& !e.ClimbableFaces.Any(f => f.IsUsed)*/);

        // create graph from edges
        var graphEdges = edges.Select(e => new TaggedEdge<Face, Edge>(e.ClimbableFaces[0], e.ClimbableFaces[1], e));
        var graph = graphEdges.ToUndirectedGraph<Face, TaggedEdge<Face, Edge>>();

        Func<TaggedEdge<Face, Edge>, double> cost = e =>
        {
             if (e.Target.Index.y == 0 && e.Source.Index.y == 0)
                 return 999999;
             if (e.Target.IsUsed && e.Source.IsUsed)
                 return 10.0;
             return 1.0;
        };

        var shortest = QuickGraph.Algorithms.AlgorithmExtensions.ShortestPathsDijkstra(graph, cost, start);

        IEnumerable<TaggedEdge<Face, Edge>> path;

        var faces = new HashSet<Face>();

        if (shortest(end, out path))
        {
            foreach (var edge in path)
            {
                faces.Add(edge.Source);
                faces.Add(edge.Target);
            }

            foreach (var face in faces)
            {
                face.IsUsed = true;
            }

            var robot = Instantiate(robotPrefab);
            
            var lPath = path.ToList();

            var current = start;
            {
                var forward = Vector3.Normalize(path.First().Tag.Center - current.Center);
                var up = current.Normal;
                var rotaxis = Vector3.Cross(forward, up);
                var rotation = Quaternion.LookRotation(forward, up);
                var pos = current.Center;

                robot.transform.position = pos + up * 0.1f;
                robot.transform.rotation = rotation;

                StartCoroutine(Tumble(robot, -rotaxis, pos + forward * 0.5f, 90));
                yield return new WaitForSeconds(0.5f);
            }

            for (int i = 0; i < lPath.Count - 1; i++)
            {                
                current = lPath[i].GetOtherVertex(current);                

                var forward1 = Vector3.Normalize(current.Center - lPath[i].Tag.Center);
                var forward2 = Vector3.Normalize(lPath[i + 1].Tag.Center - current.Center);
                var up = current.Normal;

                var rotation1 = Quaternion.LookRotation(forward1, up);
                var rotation2 = Quaternion.LookRotation(up, -forward1);
                var pos = current.Center;

                var rotaxis1 = Vector3.Cross(forward1, up);
                var rotaxis2 = Vector3.Cross(forward2, up);

                robot.transform.position = lPath[i].Tag.Center + up * 0.5f;
                robot.transform.rotation = rotation2;

                if (!current.IsOccupied)
                {
                    StartCoroutine(Tumble(robot, -rotaxis1, lPath[i].Tag.Center + forward1 * 0.1f, 90));
                    yield return new WaitForSeconds(0.5f);

                    StartCoroutine(Tumble(robot, -rotaxis2, pos + forward2 * 0.5f, 90));
                    yield return new WaitForSeconds(0.5f);
                }
            }           

            Destroy(robot);

            //draw a line of robot path
            {
                vertices = new List<Vector3>();

                var currentface = start;
                vertices.Add(currentface.Center);

                foreach (var edge in path)
                {
                    vertices.Add(edge.Tag.Center);
                    currentface = edge.GetOtherVertex(currentface);
                    vertices.Add(currentface.Center);

                    current = edge.GetOtherVertex(current);
                    if (Vector3.Distance(robot.transform.position,current.Center)<1.2f)
                    {
                        current.IsOccupied = true;
                    }
                    else current.IsOccupied = false;
                }

                vertexCount = mesh.vertexCount;
                meshVertices.AddRange(vertices);
                mesh.SetVertices(meshVertices);
                mesh.subMeshCount = 2;
                mesh.SetIndices(Enumerable.Range(vertexCount, meshVertices.Count - vertexCount).ToArray(), MeshTopology.LineStrip, 1);
            }
        }
    }

    IEnumerator Tumble(GameObject instance, Vector3 rotAxis, Vector3 pivot, float angle)
    {
        var rotSpeed = angle / 0.05f;
        var totalRotation = 0f;
        while (totalRotation <= angle)
        {
            var delta = rotSpeed * Time.deltaTime;

            if (totalRotation + delta > angle)
                delta = angle - totalRotation;

            totalRotation += delta;
            if (instance != null)
                instance.transform.RotateAround(pivot, rotAxis, delta);
            yield return null;
        }
    }

    void SpawnTile()
    {
        foreach (var voxel in _grid.GetVoxels().Where(v => v.IsActive))
        {
            Vector3 center = voxel.Center;

            Vector3[] tilePos = new Vector3[6];

            tilePos[0] = center + Vector3.up * 0.5f + Vector3.right * 0.1f;
            tilePos[1] = center + Vector3.down * 0.5f + Vector3.left * 0.1f;
            tilePos[2] = center + Vector3.left * 0.5f + Vector3.up * 0.1f;
            tilePos[3] = center + Vector3.right * 0.5f + Vector3.down * 0.1f;
            tilePos[4] = center + Vector3.forward * 0.6f + Vector3.up * 0.1f + Vector3.right * 0.1f;
            //tilePos[5] = center + Vector3.back * 0.5f;

            Quaternion[] tileRot = new Quaternion[6];

            tileRot[0] = Quaternion.identity;
            tileRot[1] = Quaternion.Euler(0, 90, 0);
            tileRot[2] = Quaternion.Euler(0, 0, 90);
            tileRot[3] = Quaternion.Euler(90, 0, 90);
            tileRot[4] = Quaternion.Euler(90, 0, 0);
            //tileRot[5] = Quaternion.Euler(0, 90, 90);

            for (int i = 0; i < 5; i++)
            {
                var tile = Instantiate(tilePrefab, tilePos[i], tileRot[i]);
            }
        }

    }
}

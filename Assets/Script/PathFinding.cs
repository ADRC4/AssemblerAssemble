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
    string _voxelSize = "1.2";

    public GameObject robotPrefab;
    public GameObject tilePrefab;
    public GameObject previewTilePrefab;

    GameObject previewTile;

    Face start;
    List<Face> endFaceList = new List<Face>();

    List<List<Vector3>> allPath = new List<List<Vector3>>();
    private void Awake()
    {
        _voids = GameObject.Find("Voids");
        Physics.queriesHitBackfaces = true;
    }

    private void Start()
    {
        previewTile = Instantiate(previewTilePrefab);
        ToggleVoids();
        StartCoroutine(MakeGrid());
    }

    bool buildMode;
    void Update()
    {
        if (_grid == null) return;

        //   Drawing.DrawMesh(false, _grid.Mesh);

        if (allPath != null)
            foreach (var path in allPath)
                if (path != null)
                    for (int i = 0; i < path.Count - 1; i++)
                        Drawing.DrawRectangularBar(path[i], path[i + 1], 0.05f, 1);


        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (var face in _grid.Faces) face.IsUsed = false;

            var startFaces = _grid.Faces.Where(f => f.Index.y == 0 && f.Index.z % 2 == 0 && f.Index.x % 2 == 0 && f.IsClimbable);

            var endFaces = _grid.Faces.Where(f => f.Center.y > 17.5f && Mathf.Abs(f.Center.x) < 1.5f && f.IsClimbable);

            foreach (var sf in startFaces)
            {
                if (sf.IsUsed) continue;
                sf.Geometry = Drawing.MakeFace(sf.Center, sf.Direction, _grid.VoxelSize, 0);
                var ef = endFaces.OrderBy(endFace => Vector3.Distance(sf.Center, endFace.Center)).First();
                ef.Geometry = Drawing.MakeFace(ef.Center, ef.Direction, _grid.VoxelSize, 0);
                StartCoroutine(MoveRobot(sf, ef));
            }

        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            foreach (var face in _grid.Faces) face.IsUsed = false;
            var startFaces = _grid.Faces.Where(f => f.Index.y == 0 && f.Index.z % 2 == 0 && f.Index.x % 2 == 0 && f.IsClimbable);
            foreach (var end in endFaceList)
            {
                start = startFaces.OrderBy(sf => Vector3.Distance(sf.Center, end.Center)).First();
                StartCoroutine(MoveRobot(start, end));
            }
            endFaceList.Clear();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            this.GetComponent<MeshRenderer>().enabled = false;
            StartCoroutine(SpawnTile());
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            buildMode = !buildMode;
        }
        if (buildMode)
        {
            previewTile.GetComponent<Renderer>().enabled = true;
            RaycastSelect();
        }
        else previewTile.GetComponent<Renderer>().enabled = false;
    }

    void RaycastSelect()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = new RaycastHit();

        var mask = LayerMask.GetMask("Void");

        if (Physics.Raycast(ray, out hit, 300, mask))
        {

            int faceIndex = hit.triangleIndex / 4;
            var face = _grid.Faces.Where(f => f.IsClimbable).ElementAt(faceIndex);

            var previewPos = face.Center + Vector3.Normalize(face.Normal) * 0.6f;
            previewTile.transform.position = previewPos;

            if (Input.GetMouseButtonDown(0))
            {
                var end = face;
                endFaceList.Add(end);
                Instantiate(previewTilePrefab, previewPos, Quaternion.identity);
            }


        }
    }

    UndirectedGraph<Face, TaggedEdge<Face, Edge>> graph;
    Mesh mesh;

    IEnumerator MakeGrid()
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

        var filter = GetComponent<MeshFilter>();//.mesh = mesh;
        var collider = this.gameObject.AddComponent<MeshCollider>();
        //   var meshes = new List<CombineInstance>();

        foreach (var face in _grid.Faces.Where(f => f.IsClimbable))
        {
            float t = 0;

            t = face.Voxels.First(v => v != null && v.IsActive).Value;
            // t = Mathf.Clamp01(voxel.FirstOrDefault().Value);

            Mesh faceMesh;
            faceMesh = Drawing.MakeFace(face.Center, face.Direction, _grid.VoxelSize, t);
            //   meshes.Add(new CombineInstance() { mesh = faceMesh });

            var subMesh = new Mesh()
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            var meshes = new[] { new CombineInstance() { mesh = mesh }, new CombineInstance() { mesh = faceMesh } };
            subMesh.CombineMeshes(meshes, true, false, false);
            mesh = subMesh;
            filter.mesh = mesh;
            collider.sharedMesh = mesh;
            yield return null;
        }
    }

    void ToggleVoids()
    {
        _toggleVoids = !_toggleVoids;

        foreach (var r in _voids.GetComponentsInChildren<Renderer>())
            r.enabled = _toggleVoids;
    }


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
                return 5.0;
            return 1.0;
        };

        var shortest = QuickGraph.Algorithms.AlgorithmExtensions.ShortestPathsDijkstra(graph, cost, start);

        IEnumerable<TaggedEdge<Face, Edge>> path;

        var faces = new HashSet<Face>();

        if (shortest(end, out path))
        {
            var robot = Instantiate(robotPrefab);
            var tile = Instantiate(tilePrefab);

            foreach (var edge in path)
            {
                faces.Add(edge.Source);
                faces.Add(edge.Target);
            }

            foreach (var face in faces)
            {
                face.IsUsed = true;

                var tileFace = face.Index.x + 1;


            }


            //draw a line of robot path

            var vertices = new List<Vector3>();

            var currentface = start;
            vertices.Add(currentface.Center);

            foreach (var edge in path)
            {
                vertices.Add(edge.Tag.Center);
                currentface = edge.GetOtherVertex(currentface);
                vertices.Add(currentface.Center);
            }
            allPath.Add(vertices);

            //robot tumbling

            var lPath = path.ToList();

            var current = start;
            {
                var forward = Vector3.Normalize(path.First().Tag.Center - current.Center);
                var up = current.Normal;
                var rotaxis = Vector3.Cross(forward, up);
                var rotation = Quaternion.LookRotation(forward, up);
                var pos = current.Center;

                robot.transform.position = pos + up * 0.1f - forward * 0.1f;
                robot.transform.rotation = rotation;

                tile.transform.position = robot.transform.position + rotaxis;
                tile.transform.rotation = robot.transform.rotation;

                StartCoroutine(Tumble(robot, -rotaxis, pos + forward * 0.4f, 90));
                StartCoroutine(Tumble(tile, -rotaxis, pos + forward * 0.4f, 90));
                yield return new WaitForSeconds(0.5f);
            }

            //for (int i = 0; i < lPath.Count - 1; i++)
            int i = 0;
            while (i < lPath.Count - 1)
            {
                var next = lPath[i].GetOtherVertex(current);

                if (!next.IsOccupied)
                {
                    current = next;
                    current.IsOccupied = true;

                    var forward1 = Vector3.Normalize(current.Center - lPath[i].Tag.Center);
                    var forward2 = Vector3.Normalize(lPath[i + 1].Tag.Center - current.Center);
                    var up = current.Normal;

                    var rotation1 = Quaternion.LookRotation(forward1, up);
                    var rotation2 = Quaternion.LookRotation(up, -forward1);
                    var pos = current.Center;

                    var rotaxis1 = Vector3.Cross(forward1, up);
                    var rotaxis2 = Vector3.Cross(forward2, up);

                    robot.transform.position = lPath[i].Tag.Center + up * 0.5f - forward1 * 0.1f;
                    robot.transform.rotation = rotation2;

                    tile.transform.position = robot.transform.position - rotaxis1;
                    tile.transform.rotation = robot.transform.rotation;


                    StartCoroutine(Tumble(robot, -rotaxis1, lPath[i].Tag.Center, 90));
                    StartCoroutine(Tumble(tile, -rotaxis1, lPath[i].Tag.Center, 90));
                    i++;
                    yield return new WaitForSeconds(0.5f);

                    tile.transform.position = robot.transform.position - rotaxis2;
                    tile.transform.rotation = robot.transform.rotation;
                    StartCoroutine(Tumble(robot, -rotaxis2, pos + forward2 * 0.4f, 90));
                    StartCoroutine(Tumble(tile, -rotaxis2, pos + forward2 * 0.4f, 90));
                    yield return new WaitForSeconds(0.5f);
                    current.IsOccupied = false;
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
            Destroy(robot);
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

    IEnumerator SpawnTile()
    {
        foreach (var voxel in _grid.GetVoxels().Where(v => v.IsActive).OrderBy(v => Vector3.Distance(v.Center, Vector3.zero)))
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
                yield return new WaitForSeconds(0.01f);
            }
        }

    }
}

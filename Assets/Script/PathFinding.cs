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
    public float voxelSize = 1.2f;

    public GameObject robotPrefab;
    public GameObject tilePrefab;
    public GameObject previewTilePrefab;

    GameObject previewTile;

    List<Face> endFaceList = new List<Face>();

    List<List<Vector3>> allPaths = new List<List<Vector3>>();

    private void Awake()
    {
        _voids = GameObject.Find("Voids");
        Physics.queriesHitBackfaces = true;
    }

    private void Start()
    {
        previewTile = Instantiate(previewTilePrefab);
        ToggleVoids();
        MakeGrid();
    }

    bool buildMode;
    bool togglePath = false;

    void Update()
    {
        if (_grid == null) return;

        if (Input.GetKeyDown(KeyCode.P))
        {
            togglePath = !togglePath;
        }

        if (togglePath)
        {
            if (allPaths != null)
                foreach (var path in allPaths)
                    if (path != null)
                        for (int i = 0; i < path.Count - 1; i++)
                            Drawing.DrawRectangularBar(path[i], path[i + 1], 0.05f, 1);
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            MakeMesh();
        }
        if (Input.GetKeyDown(KeyCode.Return)) // move to raycasted voxel
        {
            MakeGraph();
            allPaths.Clear();
            foreach (var face in _grid.Faces) face.IsUsed = false;
            var startFaces = _grid.Faces.Where(f => f.Index.y == 0 && f.Index.z % 2 == 0 && f.Index.x % 2 == 0 && f.IsClimbable);
            foreach (var end in endFaceList)
            {
                var start = startFaces.OrderBy(sf => Vector3.Distance(sf.Center, end.Center)).First();
                StartCoroutine(MoveRobot(start, end, graphFinal));
            }
            endFaceList.Clear();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(GrowVoxel());
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

    private void OnGUI()
    {
        tumblingTime = GUI.HorizontalSlider(new Rect(20, 50, 200, 20), tumblingTime, 0.5f, 0.01f);
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
                var voxel = face.Voxels.First(v => !v.IsActive);
                voxel.IsActive = true;
                var endFaces = voxel.Faces.Where(f => f.IsClimbable);
                foreach (var end in endFaces)
                    endFaceList.Add(end);
                Instantiate(previewTilePrefab, previewPos, Quaternion.identity);
            }
        }
    }


    List<Voxel> activeVoxels = new List<Voxel>();

    void MakeGrid()
    {
        // create grid with voids
        var colliders = _voids
                      .GetComponentsInChildren<MeshCollider>()
                      .ToArray();
        _grid = new Grid3d(colliders, voxelSize);
    }


    IEnumerator GrowVoxel()
    {
        //sorting the Voxel
        var faces = _grid.Faces.Where(f => f.IsActive);
        var graphFaces = faces.Select(e => new TaggedEdge<Voxel, Face>(e.Voxels[0], e.Voxels[1], e));
        var graphVoxel = graphFaces.ToUndirectedGraph<Voxel, TaggedEdge<Voxel, Face>>();
        var startVoxel = _grid.GetVoxels().Where(v => Vector3.Distance(v.Center, Vector3.zero) < 1.5f).First();
        var shortestGrow = QuickGraph.Algorithms.AlgorithmExtensions.ShortestPathsDijkstra(graphVoxel, e => 1.0, startVoxel);

        foreach (var voxel in _grid.GetVoxels().Where(v => v.IsActive))
        {
            IEnumerable<TaggedEdge<Voxel, Face>> path;
            if (shortestGrow(voxel, out path))
            {
                voxel.Order = path.Count() + UnityEngine.Random.value * 0.9f;
            }
            activeVoxels.Add(voxel);
            voxel.IsActive = false;
        }

        activeVoxels = activeVoxels.OrderBy(v => v.Order).ToList();

        foreach (var face in _grid.Faces) face.IsUsed = false;
        foreach (var edge in _grid.Edges) edge.ClimbableFaces = new Face[0];

        //List<Face> startFaces = new List<Face>()
        //{
        //_grid.Faces.Where(f => f.Index.y == 0 && f.Index.x == 0 && f.Index.z == 0 && f.Direction == Axis.Y).First(),
        //_grid.Faces.Where(f => f.Index.y == 0 && f.Index.x == 2 && f.Index.z == 0 && f.Direction == Axis.Y).First(),
        //_grid.Faces.Where(f => f.Index.y == 0 && f.Index.x == 9 && f.Index.z == 0 && f.Direction == Axis.Y).First(),
        //_grid.Faces.Where(f => f.Index.y == 0 && f.Index.x == 11 && f.Index.z == 0 && f.Direction == Axis.Y).First(),

        //_grid.Faces.Where(f => f.Index.y == 0 && f.Index.x == 0 && f.Index.z == 24 && f.Direction == Axis.Y).First(),
        //_grid.Faces.Where(f => f.Index.y == 0 && f.Index.x == 2 && f.Index.z == 24 && f.Direction == Axis.Y).First(),
        //_grid.Faces.Where(f => f.Index.y == 0 && f.Index.x == 9 && f.Index.z == 24 && f.Direction == Axis.Y).First(),
        //_grid.Faces.Where(f => f.Index.y == 0 && f.Index.x == 11 && f.Index.z == 24 && f.Direction == Axis.Y).First()
        //};

        foreach (var voxel in activeVoxels)
        {
            voxel.IsActive = true;
            
            //var climableFaces = _grid.Faces.Where(f => f.IsClimbable || (f.Index.y == 0 && f.Direction == Axis.Y && !f.Voxels.First(v => v != null).IsActive));

            foreach (var edge in _grid.Edges) edge.ClimbableFaces = edge.Faces.Where(f => f != null && f.IsClimbable).ToArray();

            // Shortest Path
            var edges = _grid.Edges.Where(e => e.ClimbableFaces.Length == 2);
            var graphEdges = edges.Select(e => new TaggedEdge<Face, Edge>(e.ClimbableFaces[0], e.ClimbableFaces[1], e));
            var graph = graphEdges.ToUndirectedGraph<Face, TaggedEdge<Face, Edge>>();

            var startFaces = _grid.Faces.Where(f => f.IsClimbable && f.Index.y == 0);
            var endFaces = voxel.Faces.Where(f => f.IsClimbable);

            for (int j = 0; j < endFaces.Count(); j++)
            {
                if (endFaces.Count() > startFaces.Count()) StartCoroutine(MoveRobot(startFaces.First(), endFaces.ElementAt(j), graph));
                else
                {
                    if (voxel.Center.z < 0) StartCoroutine(MoveRobot(startFaces.ElementAt(j), endFaces.ElementAt(j), graph));
                    else StartCoroutine(MoveRobot(startFaces.ElementAt(startFaces.Count()-j-1), endFaces.ElementAt(j), graph));
                }
            }
            yield return new WaitForSeconds(tumblingTime*10);
            
        }

    }

    UndirectedGraph<Face, TaggedEdge<Face, Edge>> graphFinal;
    void MakeGraph()
    {
        // Shortest Path
        foreach (var edge in _grid.Edges) edge.ClimbableFaces = edge.Faces.Where(f => f != null && f.IsClimbable).ToArray();
        var edges = _grid.Edges.Where(e => e.ClimbableFaces.Length == 2);
        var graphEdges = edges.Select(e => new TaggedEdge<Face, Edge>(e.ClimbableFaces[0], e.ClimbableFaces[1], e));
        graphFinal = graphEdges.ToUndirectedGraph<Face, TaggedEdge<Face, Edge>>();
    }

    void MakeMesh()
    {
        var climableFaces = _grid.Faces.Where(f => f.IsClimbable).ToList();
        var mesh = new Mesh()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        var filter = GetComponent<MeshFilter>();
        var collider = this.gameObject.AddComponent<MeshCollider>();

        foreach (var face in climableFaces)
        {
            float t = face.Voxels.First(v => v != null && v.IsActive).Value;

            Mesh faceMesh;
            faceMesh = Drawing.MakeFace(face.Center, face.Direction, _grid.VoxelSize, t);

            var subMesh = new Mesh()
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            var meshes = new[] { new CombineInstance() { mesh = mesh }, new CombineInstance() { mesh = faceMesh } };
            subMesh.CombineMeshes(meshes, true, false, false);
            mesh = subMesh;
            filter.mesh = mesh;
            collider.sharedMesh = mesh;
        }
    }

    void ToggleVoids()
    {
        _toggleVoids = !_toggleVoids;

        foreach (var r in _voids.GetComponentsInChildren<Renderer>())
            r.enabled = _toggleVoids;
    }

    public float tumblingTime = 0.05f;

    IEnumerator MoveRobot(Face start, Face end, UndirectedGraph<Face, TaggedEdge<Face, Edge>> graph)
    {
        Func<TaggedEdge<Face, Edge>, double> cost = e =>
        {
            if (e.Target.Index.y == 0 && e.Source.Index.y == 0) // prevent moving to another start face
                return 2.0;
            if (e.Target.IsUsed && e.Source.IsUsed) // prioritize to not crossing other robot path
                return 2.0;
            return 1.0;
        };

        var shortest = QuickGraph.Algorithms.AlgorithmExtensions.ShortestPathsDijkstra(graph, cost, start);

        IEnumerable<TaggedEdge<Face, Edge>> path;

        foreach (var face in _grid.Faces)
        {
            if (face.Normal == Vector3.up) face.Offset = new Vector3(0.1f, 0, 0.1f);
            else if (face.Normal == Vector3.down) face.Offset = new Vector3(0.1f, 0, -0.1f);
            else if (face.Normal == Vector3.left) face.Offset = new Vector3(0, 0.1f, 0.1f);
            else if (face.Normal == Vector3.right) face.Offset = new Vector3(0, 0.1f, 0.1f);
            else if (face.Normal == Vector3.forward) face.Offset = new Vector3(0.1f, -0.1f, 0);
            else if (face.Normal == Vector3.back) face.Offset = new Vector3(0.1f, 0.1f, 0);
        }

        var faces = new HashSet<Face>();

        if (shortest(end, out path))
        {
            var robot = Instantiate(robotPrefab);
            var tile = Instantiate(tilePrefab);
            var preview = Instantiate(tilePrefab);

            foreach (var edge in path)
            {
                faces.Add(edge.Source);
                faces.Add(edge.Target);
            }

            foreach (var face in faces) face.IsUsed = true;            

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
            allPaths.Add(vertices);

            //robot tumbling
            var lPath = path.ToList();
            var current = start;
            {
                var forward = Vector3.Normalize(path.First().Tag.Center - current.Center);
                var up = current.Normal;
                var rotaxis = Vector3.Cross(forward, up);
                var rotation = Quaternion.LookRotation(forward, up);
                var pos = current.Center;
                                               
                robot.transform.position = pos  + start.Offset + up * 0.1f;
                robot.transform.rotation = rotation;

                tile.transform.position = robot.transform.position + rotaxis;
                tile.transform.rotation = robot.transform.rotation;

                StartCoroutine(Tumble(robot, -rotaxis, pos + start.Offset + forward * 0.5f, 90));
                StartCoroutine(Tumble(tile, -rotaxis, pos + start.Offset + forward * 0.5f, 90));
                yield return new WaitForSeconds(tumblingTime);
            }

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

                    robot.transform.position = pos + current.Offset + up * 0.5f - forward1 * 0.6f;
                    robot.transform.rotation = rotation2;

                    //var sideFace = _grid.Edges.Where(e => e.IsAdjacent<Face,Edge>(current.Center));

                    var avaiableFaces = current.Voxels.First(v => !v.IsActive).Faces.Where(f => !f.IsClimbable);
                    var faceEdges = current.Edges;
                    foreach (var edge in faceEdges)
                        preview.transform.position = edge.Center;
                    

                    tile.transform.position = robot.transform.position - rotaxis1;
                    tile.transform.rotation = robot.transform.rotation;

                    StartCoroutine(Tumble(robot, -rotaxis1, pos + current.Offset - forward1 * 0.5f, 90));
                    StartCoroutine(Tumble(tile, -rotaxis1, pos + current.Offset - forward1 * 0.5f, 90));
                    i++;
                    yield return new WaitForSeconds(tumblingTime);

                    var nextFace = lPath[i].GetOtherVertex(current);      
                    if (Vector3.SignedAngle(current.Normal, nextFace.Normal, rotaxis2) != 90) // check if nextface is perperdicular
                    {
                        tile.transform.position = robot.transform.position - rotaxis2;
                        tile.transform.rotation = robot.transform.rotation;
                        StartCoroutine(Tumble(robot, -rotaxis2, pos + current.Offset + forward2 * 0.5f, 90));
                        StartCoroutine(Tumble(tile, -rotaxis2, pos + current.Offset + forward2 * 0.5f, 90));
                        yield return new WaitForSeconds(tumblingTime);
                    }

                    current.IsOccupied = false;
                }
                else
                {
                    yield return new WaitForSeconds(tumblingTime*2);
                }
            }

            current = end;
            {
                var forward = Vector3.Normalize( current.Center - path.Last().Tag.Center);
                var up = current.Normal;
                var rotaxis = Vector3.Cross(forward, up);
                var rotation = Quaternion.LookRotation(forward, up);
                var pos = current.Center;

                robot.transform.position = pos + end.Offset + up * 0.5f - forward * 0.6f;
                robot.transform.rotation = Quaternion.LookRotation(up, -forward);

                if (up == Vector3.right) tile.transform.position = pos + end.Offset + up * 0.1f;
                else tile.transform.position = pos + end.Offset - up * 0.1f;
                tile.transform.rotation = rotation;

                //foreach (var face in end.Voxels.First(v => v != null).Faces.Where(f => !f.IsClimbable))
                //{
                //    Quaternion tileRot = Quaternion.identity;
                //    Vector3 tilePos = new Vector3();
                //    Vector3 direction = face.Normal;
                //    if (direction == Vector3.up)
                //    {
                //        tileRot = Quaternion.Euler(0, 90, 0);
                //        tilePos = face.Center - Vector3.Normalize(face.Normal) * 0.1f + Vector3.right * 0.1f;
                //    }
                //    else if (direction == Vector3.down)
                //    {
                //        tileRot = Quaternion.Euler(0, 90, 0);
                //        tilePos = face.Center - Vector3.Normalize(face.Normal) * 0.1f + Vector3.left * 0.1f;
                //    }
                //    else if (direction == Vector3.right)
                //    {
                //        tileRot = Quaternion.Euler(0, 0, 90);
                //        tilePos = face.Center - Vector3.Normalize(face.Normal) * 0.1f + Vector3.down * 0.1f;
                //    }
                //    else if (direction == Vector3.left)
                //    {
                //        tileRot = Quaternion.Euler(0, 0, 90);
                //        tilePos = face.Center - Vector3.Normalize(face.Normal) * 0.1f + Vector3.up * 0.1f;
                //    }
                //    else if (direction == Vector3.forward || direction == Vector3.back)
                //    {
                //        tileRot = Quaternion.Euler(90, 0, 0);
                //        tilePos = face.Center;
                //    }
                //    Instantiate(tilePrefab, tilePos, tileRot);
                //}
                yield return new WaitForSeconds(tumblingTime);
            }
            Destroy(robot);
            allPaths.Remove(vertices);
            foreach (var face in faces) face.IsUsed = false;
        }
    }

    IEnumerator Tumble(GameObject instance, Vector3 rotAxis, Vector3 pivot, float angle)
    {
        var rotSpeed = angle / tumblingTime;
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
}

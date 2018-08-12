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

    List<Face> startFaces;
    List<Face> endFaceList = new List<Face>();

    List<List<Vector3>> allPaths = new List<List<Vector3>>();

    private void Awake()
    {
        _voids = GameObject.Find("Voids");
        Physics.queriesHitBackfaces = true;
    }
    Face sf;
    private void Start()
    {
        previewTile = Instantiate(previewTilePrefab);
        ToggleVoids();
        MakeGrid();



        //startFaces = _grid.Faces.Where(f => f.Index.y == 0 && f.Index.z % 2 == 0 && f.Index.x % 2 == 0 && f.IsClimbable).ToList();
        //sf = _grid.Faces.Where(f => f.Index.y == 0 && f.Index.z % 2 == 0 && f.Index.x % 2 == 0 && f.IsClimbable).First();

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

        if (Input.GetKeyDown(KeyCode.U))
        {
            StartCoroutine(GrowVoxel());
        }

        if (togglePath)
        {
            if (allPaths != null)
                foreach (var path in allPaths)
                    if (path != null)
                        for (int i = 0; i < path.Count - 1; i++)
                            Drawing.DrawRectangularBar(path[i], path[i + 1], 0.05f, 1);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(BuildAutomatic());
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            allPaths.Clear();
            foreach (var face in _grid.Faces) face.IsUsed = false;
            var startFaces = _grid.Faces.Where(f => f.Index.y == 0 && f.Index.z % 2 == 0 && f.Index.x % 2 == 0 && f.IsClimbable);
            foreach (var end in endFaceList)
            {
                var start = startFaces.OrderBy(sf => Vector3.Distance(sf.Center, end.Center)).First();
                StartCoroutine(MoveRobot(start, end));
            }
            endFaceList.Clear();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
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

    //List<float> waitTime;
    IEnumerator BuildAutomatic()
    {
        int sfCount = startFaces.Count;
        int turn = climableFaces.Count / sfCount;
 
        for (int j = 0; j < turn; j++)
        {
            var startFaces = _grid.Faces.Where(f => f.Index.y == 0 && f.Index.z % 2 == 0 && f.Index.x % 2 == 0 && f.Direction != Axis.Y && f.IsClimbable);
            for (int i = 0; i < sfCount; i++)
            {
                var ef = climableFaces[sfCount * j + i];
                var sf = startFaces.OrderBy(f => Vector3.Distance(f.Center, ef.Center)).Where(f => !f.IsUsed).First();
                StartCoroutine(MoveRobot(sf, ef));
            }
            yield return new WaitForSeconds(10f);
            allPaths.Clear();
        }        
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
    List<Face> climableFaces;

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
        foreach (var voxel in _grid.GetVoxels().Where(v => v.IsActive))
        {
            activeVoxels.Add(voxel);
            voxel.IsActive = false;
        }

        activeVoxels = activeVoxels.OrderBy(v => Vector3.Distance(v.Center, Vector3.zero)).ToList();


        foreach (var voxel in activeVoxels)
        {
            voxel.IsActive = true;

            var climableFaces = _grid.Faces.Where(f => f.IsClimbable);

            var mesh = new Mesh()
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            var filter = GetComponent<MeshFilter>();

            foreach (var face in climableFaces)
            {
                float t = 0;

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
            }
            yield return new WaitForSeconds(0.1f);

        }

    }

    IEnumerator GrowFace()
    {
        climableFaces = _grid.Faces.Where(f => f.IsClimbable).ToList();

        // select edges of boundary faces
        var edges = _grid.Edges.Where(e => e.ClimbableFaces.Length == 2);

        // create graph from edges
        var graphEdges = edges.Select(e => new TaggedEdge<Face, Edge>(e.ClimbableFaces[0], e.ClimbableFaces[1], e));
        graph = graphEdges.ToUndirectedGraph<Face, TaggedEdge<Face, Edge>>();

        var startFace = _grid.Faces.Where(f => Vector3.Distance(f.Center, Vector3.zero) < 1.5f && f.IsClimbable).First();
        var shortestGrow = QuickGraph.Algorithms.AlgorithmExtensions.ShortestPathsDijkstra(graph, e => 1.0, startFace);  

        foreach (var face in climableFaces)
        {
            IEnumerable<TaggedEdge<Face, Edge>> path;
            if (shortestGrow(face, out path))
            {
                face.Order = path.Count();
            }   
        }

        climableFaces = climableFaces.OrderBy(f => f.Order).ToList();

        List<Face> notClimableFaces = new List<Face>();
        foreach (var face in climableFaces)
        {
            var voxel = face.Voxels.First(v => v != null && v.IsActive);
            var notClimbFaces = voxel.Faces.Where(f => !f.IsClimbable);
            foreach (var f in notClimbFaces) notClimableFaces.Add(f);
        }

        startFaces = _grid.Faces.Where(f => f.Index.y == 0 && f.Index.z % 2 == 0 && f.Index.x % 2 == 0 && f.IsClimbable).ToList();

        mesh = new Mesh()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        var filter = GetComponent<MeshFilter>();
        var collider = this.gameObject.AddComponent<MeshCollider>();

        //foreach (var face in climableFaces)
        //{
        //    float t = face.Voxels.First(v => v != null && v.IsActive).Value;

        //    Mesh faceMesh;
        //    faceMesh = Drawing.MakeFace(face.Center, face.Direction, _grid.VoxelSize, t);

        //    var subMesh = new Mesh()
        //    {
        //        indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        //    };

        //    var meshes = new[] { new CombineInstance() { mesh = mesh }, new CombineInstance() { mesh = faceMesh } };
        //    subMesh.CombineMeshes(meshes, true, false, false);
        //    mesh = subMesh;
        //    filter.mesh = mesh;
        //    collider.sharedMesh = mesh;
        //    //yield return null;
        //}

        foreach (var face in notClimableFaces)
        {
            Quaternion rotation = Quaternion.identity;
            Vector3 direction = face.Normal;
            if (direction == Vector3.up || direction == Vector3.down)
                rotation = Quaternion.Euler(0, 90, 0);
            else if (direction == Vector3.left || direction == Vector3.right)
                rotation = Quaternion.Euler(0, 0, 90);
            else if (direction == Vector3.forward || direction == Vector3.back)
                rotation = Quaternion.Euler(90, 0, 0);
            Instantiate(tilePrefab, face.Center - Vector3.Normalize(face.Normal) * 0.1f, rotation);
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
        Func<TaggedEdge<Face, Edge>, double> cost = e =>
        {
            if (e.Target.Index.y == 0 && e.Source.Index.y == 0) // prevent moving to another start face
                return 999999;
            if (e.Target.IsUsed && e.Source.IsUsed) // prioritize to not crossing other robot path
                return 2.0;
            return 1.0;
        };

        var shortest = QuickGraph.Algorithms.AlgorithmExtensions.ShortestPathsDijkstra(graph, cost, start);

        IEnumerable<TaggedEdge<Face, Edge>> path;

        var faces = new HashSet<Face>();

        if (shortest(end, out path))
        {
            var robot = Instantiate(robotPrefab);
            var tile = Instantiate(tilePrefab);

            //float wait = path.Count() * 0.1f + 1f;
            //waitTime.Add(wait);

            foreach (var edge in path)
            {
                faces.Add(edge.Source);
                faces.Add(edge.Target);
            }

            foreach (var face in faces)
            {
                face.IsUsed = true;
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

                robot.transform.position = pos + up * 0.1f - forward * 0.1f;
                robot.transform.rotation = rotation;

                tile.transform.position = robot.transform.position + rotaxis;
                tile.transform.rotation = robot.transform.rotation;

                StartCoroutine(Tumble(robot, -rotaxis, pos + forward * 0.4f, 90));
                StartCoroutine(Tumble(tile, -rotaxis, pos + forward * 0.4f, 90));
                yield return new WaitForSeconds(0.05f);
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

                    robot.transform.position = lPath[i].Tag.Center + up * 0.5f - forward1 * 0.1f;
                    robot.transform.rotation = rotation2;

                    tile.transform.position = robot.transform.position - rotaxis1;
                    tile.transform.rotation = robot.transform.rotation;


                    StartCoroutine(Tumble(robot, -rotaxis1, lPath[i].Tag.Center, 90));
                    StartCoroutine(Tumble(tile, -rotaxis1, lPath[i].Tag.Center, 90));
                    i++;
                    yield return new WaitForSeconds(0.05f);

                    tile.transform.position = robot.transform.position - rotaxis2;
                    tile.transform.rotation = robot.transform.rotation;
                    StartCoroutine(Tumble(robot, -rotaxis2, pos + forward2 * 0.4f, 90));
                    StartCoroutine(Tumble(tile, -rotaxis2, pos + forward2 * 0.4f, 90));
                    yield return new WaitForSeconds(0.05f);
                    current.IsOccupied = false;
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            current = end;
            {
                var forward = Vector3.Normalize( current.Center - path.Last().Tag.Center);
                var up = current.Normal;
                var rotaxis = Vector3.Cross(forward, up);
                var rotation = Quaternion.LookRotation(forward, up);
                var pos = current.Center;

                robot.transform.position = path.Last().Tag.Center + up * 0.5f - forward * 0.1f;
                robot.transform.rotation = rotation;

                tile.transform.position = pos - Vector3.Normalize(up) * 0.1f;
                tile.transform.rotation = rotation;
                yield return new WaitForSeconds(0.05f);
            }

            Destroy(robot);

            foreach (var face in faces)
            {
                face.IsUsed = false;
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

    IEnumerator SpawnTile()
    {

        climableFaces = climableFaces.OrderBy(f => f.Order).ToList();

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

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
    readonly float voxelSize = 0.6f;
    public static float tumblingTime = 0.1f;

    Vector3 halfSize;

    public GameObject robotPrefab;
    public GameObject tilePrefab;
    public GameObject stackPrefab;

    public static List<List<Vector3>> allPaths = new List<List<Vector3>>();
    public static List<GameObject> placeTiles = new List<GameObject>();

    private void Awake()
    {
        halfSize = new Vector3(0.25f, 0.05f, 0.25f);
        _voids = GameObject.Find("Voids");
        Physics.queriesHitBackfaces = true;
    }

    private void Start()
    {
        ToggleVoids();
        MakeGrid();
    }

    void Update()
    {
        if (_grid == null) return;

        foreach (var face in _grid.GetFaces())
        {
            Debug.DrawRay(face.Center, face.Normal * 0.2f, Color.white);
        }

        if (Input.GetKeyDown(KeyCode.P)) MainController._togglePath = !MainController._togglePath;
        if (MainController._togglePath)
        {
            if (allPaths != null)
                foreach (var path in allPaths)
                    if (path != null)
                        for (int i = 0; i < path.Count - 1; i++)
                            Drawing.DrawRectangularBar(path[i], path[i + 1], 0.02f, 1);
        }

        if (Input.GetKeyDown(KeyCode.M))
            MakeMesh();

        if (Input.GetKey(KeyCode.E)) MakeEdge();


        if (Input.GetKeyDown(KeyCode.Return)) // move to raycasted voxel
        {
            StartCoroutine(MoveToRaycastVoxel());
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Build();
        }

        if (MainController._toggleBuildMode)
        {
            RaycastSelect();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            MainController._toggleMesh = !MainController._toggleMesh;
            this.GetComponent<MeshRenderer>().enabled = MainController._toggleMesh;
        }
    }

    void ToggleVoids()
    {
        _toggleVoids = !_toggleVoids;

        foreach (var r in _voids.GetComponentsInChildren<Renderer>())
            r.enabled = _toggleVoids;
    }

    public static List<Voxel> activeVoxels = new List<Voxel>();
    List<Face> linkFaces = new List<Face>();
    public static List<Face> startFaces = new List<Face>();
    List<GameObject> _stackList = new List<GameObject>();

    public void Build() { StartCoroutine(GrowVoxel()); }
    public void Analyse() { MakeMesh(); }
    
    void MakeGrid()
    {
        // create grid with voids
        var colliders = _voids
                      .GetComponentsInChildren<MeshCollider>()
                      .ToArray();
        _grid = new Grid3d(colliders, voxelSize);

        //sorting the Voxel
        var faces = _grid.GetFaces().Where(f => f.IsActive);
        var graphFaces = faces.Select(e => new TaggedEdge<Voxel, Face>(e.Voxels[0], e.Voxels[1], e));
        var graphVoxel = graphFaces.ToUndirectedGraph<Voxel, TaggedEdge<Voxel, Face>>();
        var startVoxel = _grid.GetVoxels().Where(v => Vector3.Distance(v.Center, Vector3.zero) < 1.5f).First();
        var shortestGrow = QuickGraph.Algorithms.AlgorithmExtensions.ShortestPathsDijkstra(graphVoxel, e => 1.0, startVoxel);

        linkFaces = faces.ToList();
        //foreach (var botomFaces in _grid.GetFaces().Where(f => f.Index.y == 0 && f.Direction == Axis.Y)) linkFaces.Add(botomFaces);

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

        for (int i = 0; i < _grid.GetFaces().Max(f => f.Index.z) / 2; i++)
        {
            startFaces.Add(_grid.GetFaces().Where(f => f.Index.y == 0 && f.Index.x == 0 && f.Index.z == i * 2 && f.Direction == Axis.Y).First());
            startFaces.Add(_grid.GetFaces().Where(f => f.Index.y == 0 && f.Index.x == 15 && f.Index.z == i * 2 && f.Direction == Axis.Y).First());
        }

        foreach (var sf in startFaces)
        {
            var stack = Instantiate(stackPrefab, sf.Center, Quaternion.identity);
            _stackList.Add(stack);
        }
    }
    List<Face> previewFaces = new List<Face>();
    List<Voxel> previewVoxels = new List<Voxel>();
    public void RaycastSelect()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = new RaycastHit();

        var mask = LayerMask.GetMask("Void");

        if (Physics.Raycast(ray, out hit, 300, mask))
        {
            int faceIndex = hit.triangleIndex / 4;
            var face = _grid.GetFaces().Where(f => f.IsClimbable).ElementAt(faceIndex);
            if (face != null)
            {
                var voxel = face.Voxels.First(v => !v.IsActive); 
                if (voxel != null) Drawing.DrawCube(voxel.Center, voxelSize);
                if (Input.GetMouseButtonDown(0))
                {
                    previewFaces.Add(face);
                    previewVoxels.Add(voxel);
                } 
            }

            if (previewVoxels.Count != 0)
            foreach (var prevoxel in previewVoxels) Drawing.DrawCube(prevoxel.Center, voxelSize);
        }
    }

    public IEnumerator MoveToRaycastVoxel()
    {
        allPaths.Clear();
        foreach (var voxel in previewVoxels)
        {
            var linkFaces = voxel.Faces.Where(f => f.IsClimbable && f.Normal != Vector3.left);
            foreach (var end in linkFaces)
            {
                Vector3 faceOffset = new Vector3();
                if (end.Normal == Vector3.up) faceOffset = new Vector3(0, halfSize.y * 2, -halfSize.y * 2);
                else if (end.Normal == Vector3.down) faceOffset = new Vector3(0, -halfSize.y * 2, halfSize.y * 2);
                else if (end.Normal == Vector3.forward) faceOffset = new Vector3(0, halfSize.y * 2, halfSize.y * 2);
                else if (end.Normal == Vector3.back) faceOffset = new Vector3(0, -halfSize.y * 2, -halfSize.y * 2);
                else if (end.Normal == Vector3.right) faceOffset = new Vector3(halfSize.y * 2, 0, 0);         
                else
                    throw new Exception("normal not found");

                if (startFaces.All(f => f.IsUsed)) foreach (var sf in startFaces) sf.IsUsed = false;
                var start = startFaces.OrderBy(sf => Vector3.Distance(sf.Center, end.Center)).First(f => !f.IsUsed);
                var stack = _stackList.First(st => st.transform.position == start.Center);

                var tileStackList = stack.GetComponent<CreateStack>().tileList;
                foreach (Transform tile in stack.transform)
                    if (tile.gameObject.name == "robot1") tile.GetComponent<Renderer>().enabled = false;
                Destroy(tileStackList.First());
                tileStackList.Remove(tileStackList.First());

                StartCoroutine(MoveRobot(start, end, graphFinal, true, 0, faceOffset));
            }
            MakeGraph();
        }

        yield return new WaitForSeconds(tumblingTime * 30);
        foreach (var voxel in previewVoxels)
        {
            voxel.IsActive = true;
            // Shortest Path to each voxel
            var edges = _grid.GetEdges().Where(e => e.ClimbableFaces.Length == 2);
            var graphEdges = edges.Select(e => new TaggedEdge<Face, Edge>(e.ClimbableFaces[0], e.ClimbableFaces[1], e));
            var graph = graphEdges.ToUndirectedGraph<Face, TaggedEdge<Face, Edge>>();

            var endFaces = voxel.Faces.Where(f => f.IsClimbable).ToList();
            foreach(var end in endFaces)
            {
                if (startFaces.All(f => f.IsUsed)) foreach (var sf in startFaces) sf.IsUsed = false;
                var start = startFaces.OrderBy(sf => Vector3.Distance(sf.Center, end.Center)).First(f => !f.IsUsed);
                var stack = _stackList.First(st => st.transform.position == start.Center);
                var tileStackList = stack.GetComponent<CreateStack>().tileList;
                stack.GetComponent<CreateStack>().robot1.GetComponent<Renderer>().enabled = false;
                Destroy(tileStackList.First());
                tileStackList.Remove(tileStackList.First());

                StartCoroutine(MoveRobot(start, end, graph, true, 0, Vector3.zero));
            }
        }
        yield return new WaitForSeconds(tumblingTime * 10);
        previewVoxels.Clear();
    }

    UndirectedGraph<Face, TaggedEdge<Face, Edge>> graphFinal;
    public void MakeGraph()
    {
        var edges = _grid.GetEdges().Where(e => e.ClimbableFaces.Length == 2);
        var graphEdges = edges.Select(e => new TaggedEdge<Face, Edge>(e.ClimbableFaces[0], e.ClimbableFaces[1], e));
        graphFinal = graphEdges.ToUndirectedGraph<Face, TaggedEdge<Face, Edge>>();
    }

    public void MakeMesh()
    {
        foreach (var voxel in activeVoxels) voxel.IsActive = true;
        var climableFaces = _grid.GetFaces().Where(f => f.IsClimbable).ToList();
        var mesh = new Mesh()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        var filter = GetComponent<MeshFilter>();
        var collider = this.gameObject.AddComponent<MeshCollider>();

        foreach (var face in climableFaces)
        {
            float t = 0;
            //t = face.Voxels.First(v => v != null && v.IsActive).Value;

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
        this.GetComponent<MeshRenderer>().enabled = false;
        SpawnTile();
    }


    public IEnumerator GrowVoxel()
    {
        foreach (var face in _grid.GetFaces()) face.IsUsed = false;

        for (int i = 0; i < activeVoxels.Count-1; i++)
        {
            activeVoxels[i].IsActive = true;

            // Shortest Path to each voxel
            var edges = _grid.GetEdges().Where(e => e.ClimbableFaces.Length == 2);
            var graphEdges = edges.Select(e => new TaggedEdge<Face, Edge>(e.ClimbableFaces[0], e.ClimbableFaces[1], e));
            var graph = graphEdges.ToUndirectedGraph<Face, TaggedEdge<Face, Edge>>();

            var endFaces = activeVoxels[i].Faces.Where(f => f.IsClimbable).ToList();

            for (int j = 0; j < endFaces.Count; j++) if (endFaces[j].Normal == Vector3.right && !linkFaces.Contains(endFaces[j])) endFaces.Remove(endFaces[j]); // remove the right climbable face;
            
            foreach (var end in endFaces)
            {                
                // search for the nearest unused start point and delete the picking tile on that stack
                if (startFaces.All(f => f.IsUsed)) foreach (var sf in startFaces) sf.IsUsed = false;
                var start = startFaces.OrderBy(sf => Vector3.Distance(sf.Center, end.Center)).First(f => !f.IsUsed);
                var stack = _stackList.First(st => st.transform.position == start.Center);

                var tileStackList = stack.GetComponent<CreateStack>().tileList;
                stack.GetComponent<CreateStack>().robot1.GetComponent<Renderer>().enabled = false;
                Destroy(tileStackList.First());
                tileStackList.Remove(tileStackList.First());

                StartCoroutine(MoveRobot(start, end, graph, true, 0, Vector3.zero));
            }

            yield return new WaitForSeconds(tumblingTime*2);

            var endFacesNext = activeVoxels[i + 1].Faces.Where(f => f.IsClimbable).ToList();

            foreach (var end in endFacesNext) // add extra linkFaces
            {
                if (linkFaces.Contains(end) && end.Direction != Axis.X)
                {
                    Vector3 faceOffset = new Vector3();
                    if (end.Direction == Axis.Y && end.Boundary == Face.BoundaryType.Right) // up
                        faceOffset = new Vector3(0, halfSize.y * 2, -halfSize.y * 2);
                    else if (end.Direction == Axis.Y && end.Boundary == Face.BoundaryType.Left) // down
                        faceOffset = new Vector3(0, -halfSize.y * 2, halfSize.y * 2);
                    else if (end.Direction == Axis.Z && end.Boundary == Face.BoundaryType.Right) // forward
                        faceOffset = new Vector3(0, halfSize.y * 2, halfSize.y * 2);
                    else if (end.Direction == Axis.Z && end.Boundary == Face.BoundaryType.Left) // back
                        faceOffset = new Vector3(0, -halfSize.y * 2, -halfSize.y * 2);
                    else
                        throw new Exception("normal not found");

                    if (startFaces.All(f => f.IsUsed)) foreach (var sf in startFaces) sf.IsUsed = false;
                    var start = startFaces.OrderBy(sf => Vector3.Distance(sf.Center, end.Center)).First(f => !f.IsUsed);
                    var stack = _stackList.First(st => st.transform.position == start.Center);

                    var tileStackList = stack.GetComponent<CreateStack>().tileList;
                    foreach (Transform tile in stack.transform)
                        if (tile.gameObject.name == "robot1") tile.GetComponent<Renderer>().enabled = false;
                    Destroy(tileStackList.First());
                    tileStackList.Remove(tileStackList.First());

                    StartCoroutine(MoveRobot(start, end, graph, true, 0, faceOffset));
                }
            }
            yield return new WaitForSeconds(tumblingTime * 20);
        }
    }
    void MakeEdge()
    {
        var face = _grid.GetFaces().Where(f => f.IsClimbable && f.Index.y == 0 && f.Direction == Axis.Y).Skip(20).First();
        Drawing.DrawCube(face.Center, 0.5f);
        var edges = face.Edges.ToList();
        foreach (var edge in edges)
        {
            var sideFaces = edge.ClimbableFaces.Last();
            Drawing.DrawCube(sideFaces.Center, 0.2f);
        }
    }

    public IEnumerator MoveRobot(Face start, Face end, UndirectedGraph<Face, TaggedEdge<Face, Edge>> graph, bool withTile, int index, Vector3 faceOffset)
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

        var faces = new HashSet<Face>();

        if (shortest(end, out path))
        {
            var endNormal = end.Normal;

            GameObject robot = Instantiate(robotPrefab);
            var tile = Instantiate(tilePrefab);
            
            foreach (var edge in path)
            {
              faces.Add(edge.Source);
              faces.Add(edge.Target);
            }

            foreach (var face in faces)
            {
                if (face.Direction == Axis.Y && face.Boundary == Face.BoundaryType.Right) // up
                    face.Offset = new Vector3(halfSize.y, 0, halfSize.y);
                else if (face.Direction == Axis.Y && face.Boundary == Face.BoundaryType.Left) // down
                    face.Offset = new Vector3(halfSize.y, 0, -halfSize.y);
                else if (face.Direction == Axis.X && face.Boundary == Face.BoundaryType.Left) // left
                    face.Offset = new Vector3(0, halfSize.y, halfSize.y);
                else if (face.Direction == Axis.X && face.Boundary == Face.BoundaryType.Right) // right
                    face.Offset = new Vector3(0, halfSize.y, halfSize.y);
                else if (face.Direction == Axis.Z && face.Boundary == Face.BoundaryType.Right) //forward
                    face.Offset = new Vector3(halfSize.y, -halfSize.y, 0);
                else if (end.Direction == Axis.Z && face.Boundary == Face.BoundaryType.Left) // back
                    face.Offset = new Vector3(halfSize.y, halfSize.y, 0);

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

                robot.transform.position = pos + start.Offset + up * halfSize.y;
                robot.transform.rotation = rotation;

                if (withTile)
                {
                    tile.transform.position = robot.transform.position + rotaxis/2;
                    tile.transform.rotation = robot.transform.rotation;
                }

                StartCoroutine(Tumble(robot, -rotaxis, pos + start.Offset + forward * halfSize.x, 90));
                if (withTile) StartCoroutine(Tumble(tile, -rotaxis, pos + start.Offset + forward * halfSize.x, 90));
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

                    robot.transform.position = pos + current.Offset + up * halfSize.x - forward1 * voxelSize / 2;
                    robot.transform.rotation = rotation2;

                    //Checking sideFaces
                    var sideEdge = current.Edges.Where(e => e != lPath[i + 1].Tag && e != lPath[i].Tag).ToArray();
                    Vector3 tileDirection = -rotaxis1;
                    //Vector3 tileDirection = Vector3.Normalize(sideEdge.First().Center - current.Center);
                    ////if (current.Voxels.First(v => !v.IsActive).Faces.First(f => f.Normal == rotaxis1).IsClimbable) tileDirection = rotaxis1;                   

                    //var sideFace1 = sideEdge.First().ClimbableFaces.Last();
                    //var sideFace2 = sideEdge.Last().ClimbableFaces.Last();

                    if (withTile)
                    {
                        //if (Vector3.SignedAngle(current.Normal, sideFace1.Normal, forward1) == 90 || sideFace1.IsOccupied)
                        //    tileDirection = -tileDirection;
                        tile.transform.position = robot.transform.position + tileDirection/2;
                        tile.transform.rotation = robot.transform.rotation;
                    }

                    StartCoroutine(Tumble(robot, -rotaxis1, pos + current.Offset - forward1 * halfSize.x, 90));
                    if (withTile) StartCoroutine(Tumble(tile, -rotaxis1, pos + current.Offset - forward1 * halfSize.x, 90));
                    i++;
                    yield return new WaitForSeconds(tumblingTime);
                    var nextFace = lPath[i].GetOtherVertex(current);

                    if (withTile)
                    {                        
                        if (rotaxis1 != rotaxis2) // change direction
                        {

                            //tileDirection = Vector3.Normalize(sideEdge.Last().Center - current.Center);
                            //if (Vector3.SignedAngle(current.Normal, sideFace2.Normal, forward2) == 90 || sideFace2.IsOccupied)
                            //    tileDirection = -tileDirection;
                            tileDirection = -rotaxis2;
                            StartCoroutine(Tumble(tile, forward1, robot.transform.position - rotaxis1 * halfSize.x + up * halfSize.y, 180));
                            yield return new WaitForSeconds(tumblingTime);
                            StartCoroutine(Tumble(tile, -forward2, robot.transform.position - rotaxis2 * halfSize.x + up * halfSize.y, 180));
                            yield return new WaitForSeconds(tumblingTime);
                        }
                        tile.transform.position = robot.transform.position + tileDirection/2;
                        tile.transform.rotation = robot.transform.rotation;
                    }

                    // check the angle of nextFaces
                    float rotAngle2;
                    if (Vector3.SignedAngle(current.Normal, nextFace.Normal, rotaxis2) == 90) rotAngle2 = 0;
                    else if (Vector3.SignedAngle(current.Normal, nextFace.Normal, rotaxis2) == -90) rotAngle2 = 180;
                    else rotAngle2 = 90;

                    StartCoroutine(Tumble(robot, -rotaxis2, pos + current.Offset + forward2 * halfSize.x, rotAngle2));
                    if (withTile) StartCoroutine(Tumble(tile, -rotaxis2, pos + current.Offset + forward2 * halfSize.x, rotAngle2));
                    yield return new WaitForSeconds(tumblingTime);

                    current.IsOccupied = false;
                }
                else
                {
                    yield return new WaitForSeconds(tumblingTime * 2);
                }
            }

            current = end;
            {
                var forward = Vector3.Normalize(current.Center - path.Last().Tag.Center);
                var up = endNormal;
                var rotation = Quaternion.LookRotation(forward, up);
                var rotaxis = Vector3.Cross(forward, up);
                var pos = current.Center;

                robot.transform.position = pos + end.Offset + up * halfSize.x - forward * voxelSize/2;
                robot.transform.rotation = Quaternion.LookRotation(up, -forward);
                if (withTile)
                {
                    tile.transform.position = robot.transform.position - rotaxis/2;
                    tile.transform.rotation = robot.transform.rotation;

                    StartCoroutine(Tumble(tile, -up, pos + end.Offset - rotaxis * halfSize.x - forward * halfSize.x, 180));
                    yield return new WaitForSeconds(tumblingTime);
                    StartCoroutine(Tumble(tile, -rotaxis, pos + end.Offset - rotaxis * halfSize.x - forward * halfSize.x, 90));
                    yield return new WaitForSeconds(tumblingTime);

                    tile.transform.rotation = rotation;
                    if (end.Normal == Vector3.right) tile.transform.position = pos + end.Offset + up * halfSize.y;
                    else if (end.Normal == Vector3.left) tile.transform.position = pos + end.Offset - up * halfSize.y;
                    else tile.transform.position = pos + end.Offset - up * halfSize.y + faceOffset;
                }
                yield return new WaitForSeconds(tumblingTime);
                placeTiles.Add(tile);
            }
            Destroy(robot);
            allPaths.Remove(vertices);
            foreach (var face in faces) face.IsUsed = false;            
        }
        //finish[index] = true;
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

    public void SpawnTile()
    {
        foreach (var voxel in activeVoxels)
        {
            Vector3 center = voxel.Center;

            Vector3[] tilePos = new Vector3[6];

            tilePos[0] = center + new Vector3(halfSize.y, halfSize.x, halfSize.y); //up
            tilePos[1] = center + new Vector3(halfSize.y, -halfSize.x, -halfSize.y); //down
            tilePos[2] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.y); //left
            tilePos[3] = center + new Vector3(halfSize.y, -halfSize.y, halfSize.x); //forward
            tilePos[4] = center + new Vector3(halfSize.y, halfSize.y, -halfSize.x); //back

            Quaternion[] tileRot = new Quaternion[6];

            tileRot[0] = Quaternion.identity;
            tileRot[1] = Quaternion.Euler(0, 90, 0);
            tileRot[2] = Quaternion.Euler(0, 0, 90);
            tileRot[3] = Quaternion.Euler(90, 0, 0);
            tileRot[4] = Quaternion.Euler(90, 0, 0);

            for (int i = 0; i < 5; i++) Instantiate(tilePrefab, tilePos[i], tileRot[i]); 
        }
    }
}

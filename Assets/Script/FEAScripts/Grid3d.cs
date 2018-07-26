using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BriefFiniteElementNet;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class Grid3d
{
    public Voxel[,,] Voxels;
    public Corner[,,] Corners;
    public List<Face> Faces;
    public List<Edge> Edges;


    public Vector3Int Size;
    public float VoxelSize;
    public Vector3 Corner;
    public IEnumerable<MeshCollider> Voids;
    public Mesh[] Mesh;
    public List<Vector3> activeCenter = new List<Vector3>();

    private float _displacement = 0;

    public float DisplacementScale
    {
        get { return _displacement; }
        set
        {
            if (value != _displacement)
            {
                _displacement = value;
                MakeMesh();
            }
        }
    }

    public Grid3d(IEnumerable<MeshCollider> voids, float voxelSize = 1.0f, float displacement = 10f)
    {
        var watch = new Stopwatch();
        watch.Start();

        Voids = voids;
        VoxelSize = voxelSize;
        _displacement = displacement;

        var bbox = new Bounds();
        foreach (var v in voids.Select(v => v.bounds))
            bbox.Encapsulate(v);

        bbox.min = new Vector3(bbox.min.x, 0, bbox.min.z);
        var sizef = bbox.size / voxelSize;
        Size = new Vector3Int((int)sizef.x, (int)sizef.y, (int)sizef.z);
        sizef = new Vector3(Size.x, Size.y, Size.z);

        Corner = bbox.min + (bbox.size - sizef * voxelSize) * 0.5f;

        // make voxels
        Voxels = new Voxel[Size.x, Size.y, Size.z];

        for (int z = 0; z < Size.z; z++)
            for (int y = 0; y < Size.y; y++)
                for (int x = 0; x < Size.x; x++)
                {
                    Voxels[x, y, z] = new Voxel(new Vector3Int(x, y, z), this);
                }

        // make faces
        Faces = new List<Face>();

        for (int z = 0; z < Size.z; z++)
            for (int y = 0; y < Size.y; y++)
                for (int x = 0; x < Size.x + 1; x++)
                {
                    var center = Corner + new Vector3(x, y+0.5f, z + 0.5f) * VoxelSize;
                    var left = x == 0 ? null : Voxels[x - 1, y, z];
                    var right = x == Size.x ? null : Voxels[x, y, z];
                    Faces.Add(new Face(center, Normal.X, left, right));
                }

        for (int z = 0; z < Size.z; z++)
            for (int y = 0; y < Size.y + 1; y++)
                for (int x = 0; x < Size.x; x++)
                {
                    var center = Corner + new Vector3(x + 0.5f, y, z + 0.5f) * VoxelSize;
                    var down = y == 0 ? null : Voxels[x, y - 1, z];
                    var top = y == Size.y ? null : Voxels[x, y, z];
                    Faces.Add(new Face(center, Normal.Y, down, top));
                }

        for (int z = 0; z < Size.z + 1; z++)
            for (int y = 0; y < Size.y; y++)
                for (int x = 0; x < Size.x; x++)
                {
                    var center = Corner + new Vector3(x + 0.5f, y + 0.5f, z) * VoxelSize;
                    var back = z == 0 ? null : Voxels[x, y, z - 1];
                    var forward = z == Size.z ? null : Voxels[x, y, z];
                    Faces.Add(new Face(center, Normal.Z, back, forward));
                }

        // make edges
        Edges = new List<Edge>();

        for (int z = 0; z < Size.z; z++)
            for (int y = 0; y < Size.y + 1; y++)
                for (int x = 0; x < Size.x + 1; x++)
                {
                    var leftBottom = (x == 0 || y == 0) ? null : Voxels[x - 1, y - 1, z];
                    var rightBottom = (x == Size.x || y == 0) ? null : Voxels[x, y - 1, z];
                    var leftTop = (x == 0 || y == Size.y) ? null : Voxels[x - 1, y, z];
                    var rightTop = (x == Size.x || y == Size.y) ? null : Voxels[x, y, z];

                    Edges.Add(new Edge(Normal.X, leftBottom, rightBottom, leftTop, rightTop));
                }

        for (int z = 0; z < Size.z + 1; z++)
            for (int y = 0; y < Size.y + 1; y++)
                for (int x = 0; x < Size.x; x++)
                {
                    var leftBottom = (z == 0 || y == 0) ? null : Voxels[x, y - 1, z - 1];
                    var rightBottom = (z == Size.z || y == 0) ? null : Voxels[x, y - 1, z];
                    var leftTop = (z == 0 || y == Size.y) ? null : Voxels[x, y, z - 1];
                    var rightTop = (z == Size.z || y == Size.y) ? null : Voxels[x, y, z];

                    Edges.Add(new Edge(Normal.X, leftBottom, rightBottom, leftTop, rightTop));
                }

        for (int z = 0; z < Size.z + 1; z++)
            for (int y = 0; y < Size.y; y++)
                for (int x = 0; x < Size.x + 1; x++)
                {
                    var leftBottom = (x == 0 || z == 0) ? null : Voxels[x - 1, y, z - 1];
                    var rightBottom = (x == Size.x || z == 0) ? null : Voxels[x, y, z - 1];
                    var leftTop = (x == 0 || z == Size.z) ? null : Voxels[x - 1, y, z];
                    var rightTop = (x == Size.x || z == Size.z) ? null : Voxels[x, y, z];

                    Edges.Add(new Edge(Normal.X, leftBottom, rightBottom, leftTop, rightTop));
                }

        // make corners
        Corners = new Corner[Size.x + 1, Size.y + 1, Size.z + 1];

        for (int z = 0; z < Size.z + 1; z++)
            for (int y = 0; y < Size.y + 1; y++)
                for (int x = 0; x < Size.x + 1; x++)
                {
                    Corners[x, y, z] = new Corner(new Vector3Int(x, y, z), this);
                }

        // calculate
        Analysis();

        Debug.Log($"Time to generate grid: {watch.ElapsedMilliseconds} ms");
    }

    IEnumerable<Voxel> GetVoxels()
    {
        for (int z = 0; z < Size.z; z++)
            for (int x = 0; x < Size.x; x++)
                for (int y = 0; y < Size.y; y++)
                {
                    yield return Voxels[x, y, z];
                }
    }

    public void Analysis()
    {
        // analysis model
        var model = new Model();

        var nodes = GetVoxels()
             .Where(b => b.IsActive)
             .SelectMany(v => v.GetCorners())
             .Distinct()
             .ToArray();


        var elements = GetVoxels()
             .Where(b => b.IsActive)
             .SelectMany(v => v.MakeTetrahedrons())
             .ToArray();

        model.Nodes.Add(nodes);
        model.Elements.Add(elements);

        model.Solve();

        //var activeVoxels = GetVoxels().Where(b => b.IsActive).ToArray();

        //int i = 0;
        //foreach(var element in elements)
        //{
        //    var tensor = element.GetInternalForce(LoadCombination.DefaultLoadCombination);
        //    var c = new[] { tensor.S11, tensor.S12, tensor.S13, tensor.S21, tensor.S22, tensor.S23, tensor.S31, tensor.S32, tensor.S33};
        //    var stress = c.Max();
        //    var index = i / 5;
        //    activeVoxels[index].Value += (float)stress;
        //}

        // analysis results
        foreach (var node in nodes)
        {
            var d = node
           .GetNodalDisplacement(LoadCase.DefaultLoadCase)
           .Displacements;

            node.Displacement = new Vector3((float)d.X, (float)d.Z, (float)d.Y);
            var length = node.Displacement.magnitude;

            foreach (var voxel in node.GetConnectedVoxels())
                voxel.Value += length;
        }

        var activeVoxels = GetVoxels().Where(v => v.IsActive);

        foreach (var voxel in activeVoxels)
            voxel.Value /= voxel.GetCorners().Count();
        //

        var min = activeVoxels.Min(v => v.Value);
        var max = activeVoxels.Max(v => v.Value);

        foreach (var voxel in activeVoxels)
            voxel.Value = Mathf.InverseLerp(min, max, voxel.Value);

    }

    public void MakeMesh()
    {
        Mesh = GetVoxels()
            .Where(v => v.IsActive)
            .Select(v =>
        {
            var corners = v.GetCorners()
                    .Select(c => c.DisplacedPosition)
                    .ToArray();

            return Drawing.MakeTwistedBox(corners, v.Value, null);
        }).ToArray();
    }

    public void TilePosition()
    {
        var activeVoxel = GetVoxels().Where(b => b.IsActive);
        Vector3 center;
        foreach (var voxel in activeVoxel)
        {
            center = voxel.Center;

            if (voxel.Value < 0.7)
                activeCenter.Add(center);

        }
    }

}
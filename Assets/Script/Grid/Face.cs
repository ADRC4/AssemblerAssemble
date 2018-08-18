using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BriefFiniteElementNet;
using BriefFiniteElementNet.Elements;
using System;
using QuickGraph;

public class Face
{
    public Voxel[] Voxels;
    public Edge[] Edges;
    public Vector3Int Index;
    public Vector3 Center;
    public Axis Direction;
    public Vector3 Normal;
    //public float NormalizedDistance = 0f;
    public Mesh Geometry;
    public Vector3 Offset;
    public int Order;

    Grid3d _grid;
    // public FrameElement2Node Frame;

    public bool IsActive => Voxels.Count(v => v != null && v.IsActive) == 2;
    public bool IsUsed;
    public bool IsOccupied;

    public bool IsClimbable
    {
        get
        {
            //if (Index.y == 0 && Direction == Axis.Y) return false;
            return Voxels.Count(v => v != null && v.IsActive) == 1;
        }
    }

    public Face(int x, int y, int z, Axis direction, Grid3d grid)
    {
        _grid = grid;
        Index = new Vector3Int(x, y, z);
        Direction = direction;
        Voxels = GetVoxels();

        foreach (var v in Voxels.Where(v => v != null))
            v.Faces.Add(this);

        Center = GetCenter();
        Normal = GetNormal();
        Edges = GetEdge();
    }

    Vector3 GetCenter()
    {
        int x = Index.x;
        int y = Index.y;
        int z = Index.z;

        switch (Direction)
        {
            case Axis.X:
                return _grid.Corner + new Vector3(x, y + 0.5f, z + 0.5f) * _grid.VoxelSize;
            case Axis.Y:
                return _grid.Corner + new Vector3(x + 0.5f, y, z + 0.5f) * _grid.VoxelSize;
            case Axis.Z:
                return _grid.Corner + new Vector3(x + 0.5f, y + 0.5f, z) * _grid.VoxelSize;
            default:
                throw new Exception("Wrong direction.");
        }
    }

    Voxel[] GetVoxels()
    {
        int x = Index.x;
        int y = Index.y;
        int z = Index.z;

        switch (Direction)
        {
            case Axis.X:
                return new[]
                {
                   x == 0 ? null : _grid.Voxels[x - 1, y, z],
                   x == _grid.Size.x ? null : _grid.Voxels[x, y, z]
                };
            case Axis.Y:
                return new[]
                {
                   y == 0 ? null : _grid.Voxels[x, y - 1, z],
                   y == _grid.Size.y ? null : _grid.Voxels[x, y, z]
                };
            case Axis.Z:
                return new[]
                {
                   z == 0 ? null : _grid.Voxels[x, y, z - 1],
                   z == _grid.Size.z ? null : _grid.Voxels[x, y, z]
                 };
            default:
                throw new Exception("Wrong direction.");
        }
    }

    Vector3 GetNormal()
    {
        int x = Index.x;
        int y = Index.y;
        int z = Index.z;

        bool left = Voxels[0] != null && Voxels[0].IsActive;

        switch (Direction)
        {
            case Axis.X:
             return left ? Vector3.right : -Vector3.right; 
            case Axis.Y:
                return left ? Vector3.up : -Vector3.up;
            case Axis.Z:
             return left ? Vector3.forward : -Vector3.forward;
            default:
                throw new Exception("Wrong direction.");
        }
    }

    Edge[] GetEdge()
    {
        int x = Index.x;
        int y = Index.y;
        int z = Index.z;

        switch (Direction)
        {
            case Axis.X:
                return new[]
                {
                    y == 0 ? Voxels[0]?.Edges[0] : Voxels[1]?.Edges[1],
                    z == 0 ? Voxels[0]?.Edges[2] : Voxels[1]?.Edges[3],
                };
            case Axis.Y:
                return new[]
                {
                    x == 0 ? Voxels[0]?.Edges[4] : Voxels[1]?.Edges[5],
                    z == 0 ? Voxels[0]?.Edges[6] : Voxels[1]?.Edges[7],
                };
            case Axis.Z:
                return new[]
                {
                    x == 0 ? Voxels[0]?.Edges[8] : Voxels[1]?.Edges[9],
                    y == 0 ? Voxels[0]?.Edges[10] : Voxels[1]?.Edges[11],
                };
            default:
                throw new Exception("Wrong direction.");
        }
    }
}

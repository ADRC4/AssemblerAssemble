using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BriefFiniteElementNet;
using BriefFiniteElementNet.Elements;
using Constraint = BriefFiniteElementNet.Constraints;
using System;

public enum Normal { X, Y, Z };

public class Face
{
    public Voxel Start;
    public Voxel End;
    public Vector3 Center;
    public Normal Direction;

    public Voxel[] Neighbours;

    public bool IsClimbable
    {
        get
        {
            var voxels = Neighbours.Where(v => v != null);
            if (voxels.Count(v => v.IsActive) == 1)
                return true;
            else
                return false;
        }
    }

    // public FrameElement2Node Frame;

    public bool IsActive => Start.IsActive && End.IsActive;

    public Face(Vector3 center, Normal direction, Voxel start, Voxel end)
    {
        Center = center;
        Direction = direction;
        Start = start;
        End = end;

        Neighbours = new[] { Start, End };

        start?.Faces.Add(this);
        end?.Faces.Add(this);

        //Frame = new FrameElement2Node(start, end)
        //{
        //    Iy = 0.02,
        //    Iz = 0.02,
        //    A = 0.01,
        //    J = 0.05,
        //    E = 210e9,
        //    G = 70e9,
        //    ConsiderShearDeformation = false,
        //};
    }
}

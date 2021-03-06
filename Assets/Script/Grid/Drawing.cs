﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class Drawing : MonoBehaviour
{
    [SerializeField]
    Mesh _box;
    [SerializeField]
    Mesh _cylinder;
    [SerializeField]
    Material _opaque;
    [SerializeField]
    Material _transparent;
    [SerializeField]
    Material _black;
    [SerializeField]

    Material _pathMaterial;
    Material _previewMaterial;

    static Drawing _instance;
    static Gradient _gradient = new Gradient();
    //static Mesh _unitFace;

    void Awake()
    {
        _instance = this;
        //_unitFace = UnitFace(0);

        var gck = new GradientColorKey[2];
        var gak = new GradientAlphaKey[0];
        gck[0].color = Color.green;
        gck[0].time = 0.0F;
        gck[1].color = Color.red;
        gck[1].time = 1.0F;
        _gradient.SetKeys(gck, gak);

        var texture = new Texture2D(256, 2)
        {
            wrapMode = TextureWrapMode.Clamp,
        };

        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            var color = _gradient.Evaluate(t);
            texture.SetPixel(i, 0, color);
        }

        texture.SetPixel(0, 1, Color.yellow);

        texture.Apply();

        _opaque.mainTexture = texture;
        _transparent.mainTexture = texture;

        _pathMaterial = new Material(_opaque);
        var properties = new MaterialPropertyBlock();
        var texRed = new Texture2D(1, 1);
        texRed.SetPixel(0, 0, Color.red);
        texRed.Apply();
        _pathMaterial.mainTexture = texRed;

        //_previewMaterial = new Material(_transparent);
        //var texBlue = new Texture2D(1, 1);
        //texBlue.SetPixel(0, 0, Color.blue);
        //texBlue.Apply();
        //_pathMaterial.mainTexture = texBlue;

    }

    public static void DrawCube(Vector3 center, float size)
    {
        var color = Color.blue;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_Color", color);
        var matrix = Matrix4x4.TRS(
                center,
                Quaternion.identity,
                Vector3.one * (size * 0.999f)
                );

        Graphics.DrawMesh(_instance._box, matrix, _instance._previewMaterial, 0);
    }

    //public static void DrawFace(Vector3 center, Axis direction, float size)
    //{
    //    Quaternion rotation = Quaternion.identity;

    //    switch (direction)
    //    {
    //        case Axis.X:
    //            rotation = Quaternion.Euler(0, 90, 0);
    //            break;
    //        case Axis.Y:
    //            rotation = Quaternion.Euler(90, 0, 0);
    //            break;
    //        case Axis.Z:
    //            rotation = Quaternion.Euler(0, 0, 0);
    //            break;
    //        default:
    //            break;
    //    }

    //    var matrix = Matrix4x4.TRS(
    //            center,
    //            rotation,
    //            Vector3.one * size
    //            );

    //    _instance._opaque.mainTexture = null;
    //    Graphics.DrawMesh(_unitFace, matrix, _instance._opaque, 0, null, 0);
    //    Graphics.DrawMesh(_unitFace, matrix, _instance._black, 0, null, 1);
    //}

    public static void DrawBar(Vector3 start, Vector3 end, float radius, float t)
    {
        var color = Color.red;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_Color", color);

        var vector = end - start;

        var matrix = Matrix4x4.TRS(
                        start + vector * 0.5f,
                        Quaternion.LookRotation(vector) * Quaternion.Euler(90, 0, 0),
                        new Vector3(radius, vector.magnitude * 0.5f, radius)
                        );

        Graphics.DrawMesh(_instance._cylinder, matrix, _instance._opaque, 0, null, 0, properties);
    }

    public static void DrawRectangularBar(Vector3 start, Vector3 end, float radius, float t)
    {
        var vector = end - start;

        var matrix = Matrix4x4.TRS(
                        start + vector * 0.5f,
                        Quaternion.LookRotation(vector) * Quaternion.Euler(90, 0, 0),
                        new Vector3(radius, vector.magnitude * 1f, radius)
                        );

        Graphics.DrawMesh(_instance._box, matrix, _instance._pathMaterial, 0);
    }

    public static void DrawMesh(bool isTransparent, params Mesh[] mesh)
    {
        var material = isTransparent ? _instance._transparent : _instance._opaque;

        foreach (var m in mesh)
        {
            Graphics.DrawMesh(m, Matrix4x4.identity, material, 0);

            if (m.subMeshCount > 1)
                Graphics.DrawMesh(m, Matrix4x4.identity, _instance._black, 0, null, 1);
        }
    }

    public static Mesh MakeTwistedBox(Vector3[] corners, float t, Mesh mesh = null)
    {
        Vector3 center = Vector3.zero;
        foreach (var corner in corners)
            center += corner;

        center /= corners.Length;

        corners = corners
            .Select(c => c + (center - c).normalized * 0.05f)
            .ToArray();

        var f = new[]
        {
            0,1,3,2,
            4,6,7,5,
            6,4,0,2,
            4,5,1,0,
            5,7,3,1,
            7,6,2,3
        };

        var v = f.Select(i => corners[i]).ToArray();

        if (mesh == null)
        {
            mesh = new Mesh()
            {
                vertices = v,
                uv = Enumerable.Repeat(new Vector2(t, 0), v.Length).ToArray()
            };

            var faces = Enumerable.Range(0, 24).ToArray();
            mesh.SetIndices(faces, MeshTopology.Quads, 0);
        }
        else
        {
            mesh.vertices = v;
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh MakeFace(Vector3 center, Axis direction, float size, float u, float v = 0)
    {
        Quaternion rotation = Quaternion.identity;

        switch (direction)
        {
            case Axis.X:
                rotation = Quaternion.Euler(0, 90, 0);
                break;
            case Axis.Y:
                rotation = Quaternion.Euler(90, 0, 0);
                break;
            case Axis.Z:
                rotation = Quaternion.Euler(0, 0, 0);
                break;
        }

        //var f = new[]
        //{
        //    0,1,2,3,
        //    7,6,5,4
        //};

        var tris = new[]
        {
            0,1,2, 0,2,3,
            6,5,4, 7,6,4
        };

        //var l = new[]
        //{
        //    0,1,2,3,0
        //};

        float s = size * 0.5f;

        var vertices = new[]
        {
            new Vector3(-s,-s,0),
            new Vector3(s,-s,0),
            new Vector3(s,s,0),
            new Vector3(-s,s,0),
            new Vector3(-s,-s,0),
            new Vector3(s,-s,0),
            new Vector3(s,s,0),
            new Vector3(-s,s,0)
        };

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = rotation * vertices[i];
            vertices[i] += center;
        }

        var n = -Vector3.forward;
        n = rotation * n;

        var mesh = new Mesh()
        {
            vertices = vertices,
            normals = new[] { n, n, n, n, -n, -n, -n, -n },
            uv = Enumerable.Repeat(new Vector2(u, v), vertices.Length).ToArray(),
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            // subMeshCount = 2,
        };

        mesh.SetIndices(tris, MeshTopology.Triangles, 0);
        //mesh.SetIndices(l, MeshTopology.LineStrip, 1);
        // mesh.SetIndices(f, MeshTopology.Quads, 0);

        mesh.RecalculateBounds();
        // mesh.RecalculateTangents();
        return mesh;
    }
}
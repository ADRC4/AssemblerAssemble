using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileColor : MonoBehaviour
{
    public Color foregroundColor;
    public Color backgroundColor;

    void Start()
    {
        var tileRenderer = this.GetComponent<Renderer>();
        Material tileMaterial = new Material(tileRenderer.sharedMaterial);
        float colourPercent = Random.Range(0.0f, 1.0f);
        tileMaterial.color = Color.Lerp(foregroundColor, backgroundColor, colourPercent);
        tileRenderer.sharedMaterial = tileMaterial;
    }

}

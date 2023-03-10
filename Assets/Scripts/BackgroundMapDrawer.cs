using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Random = UnityEngine.Random;

/// <summary>
/// Class <c>BackgroundMapDrawer</c> loads a two-dimensional ROS navigation
///  map and converts it to a 2D array. 
/// </summary>
public class BackgroundMapDrawer : MonoBehaviour
{
    public Camera m_camera;

    // array of integers, each integer corresponding to a grid cell
    public float cellDim = 0.1f;
    private int[,] grid;
    private int gridXDim = 1024;
    private int gridYDim = 1024;
    private int[] gridPadding;
    private float cellDimX;
    private float cellDimY;
    public RectTransform rectTransform;

    private Texture2D texture;
    private Renderer renderer;
    private Color[] _colors;
    private Color[] _edgeColors;
    private Vector2 materialDim; // the dimensions of the material
    private Vector2 textureDim;

    // Start is called before the first frame update
    void Start()
    {
        LoadDefaultMap();
        //LoadBlankMap();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void LoadDefaultMap()
    {
        LoadMap("map_default");
    }

    public void LoadBlankMap()
    {
        Color voidColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);
        textureDim = new Vector2(Mathf.Max(gridXDim, gridYDim), Mathf.Max(gridXDim, gridYDim));
        texture = new Texture2D(Mathf.RoundToInt(textureDim.x), Mathf.RoundToInt(textureDim.y));
        renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = texture;
        for (int y = 0; y < gridYDim; y++)
        {
            for (int x = 0; x < gridXDim; x++)
            {
                texture.SetPixel(x, y, voidColor);
            }
        }
        texture.Apply();
    }

    public void LoadMap(MapLayout ml)
    {
        // start with ml read in from ROS directly
        gridXDim = Mathf.Max(ml.width, ml.height);
        gridYDim = Mathf.Max(ml.width, ml.height);
        gridPadding = GetPadding(ml.data, ml.width, ml.height);
        Debug.Log(gridPadding[0] + " " + gridPadding[1] + " " + gridPadding[2] + " " + gridPadding[3]);
        grid = new int[gridXDim, gridYDim];
        textureDim = new Vector2(Mathf.Max(gridXDim, gridYDim), Mathf.Max(gridXDim, gridYDim));
        texture = new Texture2D(Mathf.RoundToInt(textureDim.x), Mathf.RoundToInt(textureDim.y));
        renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = texture;
        materialDim = new Vector2(renderer.bounds.min.x * -1, renderer.bounds.min.y * -1);
        cellDimX = (materialDim.x * 2) / gridXDim;
        cellDimY = (materialDim.y * 2) / gridYDim;

        // draw each pixel
        Color[] wallColors = Enumerable.Repeat(new Color(0.0f, 0.0f, 0.0f, 1f), 6 * 6).ToArray();
        Color[] floorColors = Enumerable.Repeat(new Color(1.0f, 1.0f, 1.0f, 1f), 6 * 6).ToArray();
        Color[] voidColors = Enumerable.Repeat(new Color(0.2f, 0.2f, 0.2f, 1f), 6 * 6).ToArray();
        Color wallColor = new Color(0.0f, 0.0f, 0.0f, 1f);
        Color floorColor = new Color(1.0f, 1.0f, 1.0f, 1f);
        Color voidColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);
        Debug.Log(ml.data.Length);
        Debug.Log(gridXDim);
        Debug.Log(gridYDim);
        Color clearColor = Color.clear;
        // pad the top
        for (int y = 0; y < gridPadding[2]; y++)
        {
            for (int x = 0; x < gridXDim; x++)
            {
                texture.SetPixel(x, y, voidColor);
            }
        }
        for (int y = gridPadding[2]; y < gridYDim - gridPadding[3]; y++)
        {
            // pad the left
            for (int x = 0; x < gridPadding[0]; x++)
            {
                texture.SetPixel(x, y, voidColor);
            }
            for (int x = gridPadding[0]; x < gridXDim - gridPadding[1]; x++)
            {
                texture.SetPixel(x, y, clearColor);
                int yconv = y - gridPadding[2];
                int xconv = x - gridPadding[0];
                int idx = yconv * ml.width + (ml.width - xconv - 1);
                grid[x, y] = ml.data[idx];
                Color color;
                Color[] colors;
                if (grid[x, y] == -1)
                {
                    color = voidColor;
                    colors = voidColors;
                }
                else if (grid[x, y] == 100)
                {
                    color = wallColor;
                    colors = wallColors;
                }
                else
                {
                    color = floorColor;
                    colors = floorColors;
                }
                texture.SetPixel(x, y, color);
                //ColorGridCell(new Vector2(x,y), colors);
            }
            // pad the right
            for (int x = gridXDim - gridPadding[1]; x < gridXDim; x++)
            {
                texture.SetPixel(x, y, voidColor);
            }
        }
        // pad the bottom
        for (int y = gridYDim - gridPadding[3]; y < gridYDim; y++)
        {
            for (int x = 0; x < gridXDim; x++)
            {
                texture.SetPixel(x, y, voidColor);
            }
        }
        texture.Apply();
    }

    public void LoadMap(string map_name)
    {
        // load the grid from file
        //string path = "Assets/Resources/map.json";
        //StreamReader reader = new StreamReader(path);
        //string jsonString = reader.ReadToEnd();
        TextAsset mapText = Resources.Load<TextAsset>(map_name);
        string jsonString = mapText.text;
        Debug.Log(jsonString);
        //reader.Close();
        MapLayout ml = JsonUtility.FromJson<MapLayout>(jsonString);
        gridXDim = Mathf.Max(ml.width, ml.height);
        gridYDim = Mathf.Max(ml.width, ml.height);
        gridPadding = GetPadding(ml.data, ml.width, ml.height);
        Debug.Log(gridPadding[0] + " " + gridPadding[1] + " " + gridPadding[2] + " " + gridPadding[3]);
        grid = new int[gridXDim, gridYDim];
        textureDim = new Vector2(Mathf.Max(gridXDim, gridYDim), Mathf.Max(gridXDim, gridYDim));
        texture = new Texture2D(Mathf.RoundToInt(textureDim.x), Mathf.RoundToInt(textureDim.y));
        renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = texture;
        materialDim = new Vector2(renderer.bounds.min.x * -1, renderer.bounds.min.y * -1);
        cellDimX = (materialDim.x * 2) / gridXDim;
        cellDimY = (materialDim.y * 2) / gridYDim;

        // draw each pixel
        Color[] wallColors = Enumerable.Repeat(new Color(0.0f, 0.0f, 0.0f, 1f), 6 * 6).ToArray();
        Color[] floorColors = Enumerable.Repeat(new Color(1.0f, 1.0f, 1.0f, 1f), 6 * 6).ToArray();
        Color[] voidColors = Enumerable.Repeat(new Color(0.2f, 0.2f, 0.2f, 1f), 6 * 6).ToArray();
        Color wallColor = new Color(0.0f, 0.0f, 0.0f, 1f);
        Color floorColor = new Color(1.0f, 1.0f, 1.0f, 1f);
        Color voidColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);
        Debug.Log(ml.data.Length);
        Debug.Log(gridXDim);
        Debug.Log(gridYDim);
        Color clearColor = Color.clear;
        // pad the top
        for (int y = 0; y < gridPadding[2]; y++)
        {
            for (int x = 0; x < gridXDim; x++)
            { 
                texture.SetPixel(x, y, voidColor); 
            }
        }
        for (int y = gridPadding[2]; y < gridYDim - gridPadding[3]; y++)
        {
            // pad the left
            for (int x = 0; x < gridPadding[0]; x++)
            {
                texture.SetPixel(x, y, voidColor);
            }
            for (int x = gridPadding[0]; x < gridXDim - gridPadding[1]; x++)
            {
                texture.SetPixel(x, y, clearColor);
                int yconv = y - gridPadding[2];
                int xconv = x - gridPadding[0];
                int idx = yconv * ml.width + (ml.width - xconv - 1);
                grid[x, y] = ml.data[idx];
                Color color;
                Color[] colors;
                if (grid[x, y] == -1)
                {
                    color = voidColor;
                    colors = voidColors;
                }
                else if (grid[x, y] == 100)
                {
                    color = wallColor;
                    colors = wallColors;
                }
                else
                {
                    color = floorColor;
                    colors = floorColors;
                }
                texture.SetPixel(x, y, color);
                //ColorGridCell(new Vector2(x,y), colors);
            }
            // pad the right
            for (int x = gridXDim - gridPadding[1]; x < gridXDim; x++)
            {
                texture.SetPixel(x, y, voidColor);
            }
        }
        // pad the bottom
        for (int y = gridYDim - gridPadding[3]; y < gridYDim; y++)
        {
            for (int x = 0; x < gridXDim; x++)
            {
                texture.SetPixel(x, y, voidColor);
            }
        }
        texture.Apply();
    }

    private int[] GetPadding(int[] data, int width, int height)
    {
        int leftMost = 1000000000;
        int rightMost = -1;
        int topMost = 1000000000;
        int bottomMost = -1;
        for (int y = 0; y < height; y++ )
        {
            for (int x = 0; x < width; x++ )
            {
                int idx = y * width + (width - x - 1);
                if (data[idx] > -1)
                {
                    if (y < topMost)
                    {
                        topMost = y;
                    }
                    if (y > bottomMost)
                    {
                        bottomMost = y;
                    }
                    if (x < leftMost)
                    {
                        leftMost = x;
                    }
                    if (x > rightMost)
                    {
                        rightMost = x;
                    }
                }
            }
        }
        Debug.Log("Topmost: " + topMost);
        Debug.Log("Bottommost: " + bottomMost);
        int currCenterX = Mathf.RoundToInt(leftMost + (rightMost - leftMost) / 2.0f);
        int currCenterY = Mathf.RoundToInt(topMost + (bottomMost - topMost) / 2.0f);
        int[] padding = { 0, 0, 0, 0 }; // left, right, top, bottom
        if (width > height)
        {
            // pad the height
            int topPadded = 0;
            int bottomPadded = 0;
            int padAllowance = width - height;
            int idealCenter = Mathf.RoundToInt(width / 2.0f);
            if (currCenterY < idealCenter)
            {
                Debug.Log("Curr center (" + currCenterY + ") is less than ideal (" + idealCenter + ")! Padding top!");
                topPadded = Mathf.Min(padAllowance, idealCenter - currCenterY);
                if (topPadded < padAllowance)
                {
                    bottomPadded = padAllowance - topPadded;
                } 
            }
            else
            {
                Debug.Log("BOTTOM ALL PADDED");
                bottomPadded = padAllowance;
            }
            padding[2] = topPadded;
            padding[3] = bottomPadded;
        }
        else if (width < height)
        {
            // pad the width
            int leftPadded = 0;
            int rightPadded = 0;
            int padAllowance = height - width;
            int idealCenter = Mathf.RoundToInt(height / 2.0f);
            if (currCenterX < idealCenter)
            {
                Debug.Log("LEFT PADDED");
                leftPadded = Mathf.Min(padAllowance, idealCenter - currCenterX);
                if (leftPadded < padAllowance)
                {
                    rightPadded = padAllowance - leftPadded;
                }
            }
            else
            {
                Debug.Log("RIGHT ALL PADDED");
                rightPadded = padAllowance;
            }
            padding[0] = leftPadded;
            padding[1] = rightPadded;
        }
        return padding;
    }

    private void ColorGridCell(Vector2 newCell, Color[] color)
    {
        List<float> bounds = GetGridCellBounds(newCell, materialDim);
        Pair lowCoord = WorldPTToTexture(new Vector2(bounds[0], bounds[2]));
        Pair highCoord = WorldPTToTexture(new Vector2(bounds[1], bounds[3]));
        texture.SetPixels(lowCoord.x, lowCoord.y, highCoord.x - lowCoord.x, highCoord.y - lowCoord.y, color);
    }

    public List<float> GetGridCellBounds(Vector2 cell, Vector2 textureDim)
    {

        // low x, high x, low y, high y
        Vector2 tpt = GridCellToPT(cell, textureDim);
        List<float> l = new List<float>();
        l.Add(tpt.x); // x low
        l.Add(tpt.x + cellDimX); // x high
        l.Add(tpt.y - cellDimY); // y low
        l.Add(tpt.y); // y high
        return l;
    }

    // take a 2d px in world space and convert it to grid coordinates
    public Vector2 PTToGridCell(Vector2 pt, Vector2 textureDim)
    {

        // x corresponds to col. y corresponds to row.
        Vector2 tpt = pt;   // row-column format
        // ORIGINAL: 
        //tpt.y += textureDim.y;
        //tpt.y /= cellDimY;
        //tpt.y = Mathf.Floor(tpt.y);
        //tpt.x -= textureDim.x;
        //tpt.x /= cellDimX;
        //tpt.x = -1 * Mathf.Ceil(tpt.x);

        // FIXED:
        tpt.x = pt.y * -1.0f;
        tpt.x += 5.0f;
        tpt.x *= (gridXDim / 10.0f);  // === /= (10.0f / gridXDim)
        tpt.x = Mathf.Floor(tpt.x);
        tpt.y = pt.x + 5.0f;
        tpt.y *= (gridYDim / 10.0f);
        tpt.y = Mathf.Floor(tpt.y);
        return tpt;
    }

    public Vector2 GridCellToPT(Vector2 cell, Vector2 textureDim)
    {
        Vector2 tpt = cell;
        // ORIGINAL: 
        //tpt.x *= cellDimY;
        //tpt.x -= textureDim.y;
        //tpt.y *= -1;
        //tpt.y *= cellDimX;
        //tpt.y += textureDim.x;

        // FIXED:
        //float temp = tpt.x;
        //tpt.x = tpt.y;
        //tpt.y = temp;
        tpt.y *= (10.0f / gridXDim);
        tpt.y -= 5.0f;
        tpt.y *= -1.0f;
        tpt.x *= (10.0f / gridYDim);
        tpt.x = tpt.x - 5.0f;
        return tpt;
    }

    private Pair WorldPTToTexture(Vector2 pt)
    {
        int x = Mathf.RoundToInt(((pt.x + materialDim.x) / (materialDim.x * 2)) * textureDim.x);
        int y = Mathf.RoundToInt(((pt.y + materialDim.y) / (materialDim.y * 2)) * textureDim.y);
        return new Pair(y, x);
    }

    public int GetCellContents(Vector2 cell)
    {
        // Returns the contents of a cell.
        int row = Mathf.RoundToInt(cell.x);
        int col = Mathf.RoundToInt(cell.y);
        return grid[row, col];
    }

    public void SetCellContents(Vector2 cell, int val)
    {
        int row = Mathf.RoundToInt(cell.x);
        int col = Mathf.RoundToInt(cell.y);
        grid[row, col] = val;
    }

    private float CellDistance(int startX, int startY, int endX, int endY)
    {
        return Mathf.Sqrt(Mathf.Pow(endX - startX, 2) + Mathf.Pow(endY - startY, 2));
    }
}

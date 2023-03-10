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
/// Class <c>RegionGrid</c> stores a grid in which each cell corresponds
///  to a user-defined region.
/// </summary>
public class RegionGrid : MonoBehaviour
{
    // array of integers, each integer corresponding to a grid cell
    public float cellDim = 0.1f;
    private Region[,] grid;
    private int gridXDim = 1024;
    private int gridYDim = 1024;
    private Vector2 materialDim;
    private float cellDimX;
    private float cellDimY;
    public RectTransform rectTransform;

    // store data about each region
    public List<Region> regions;

    // event for regions to communicate with grid
    UnityEvent cb; // callback

    // for updating ros nodes
    private RosNode ros;

    // Start is called before the first frame update
    void Start()
    {
        //LoadMap();
        grid = new Region[gridXDim, gridYDim];
        regions = new List<Region>();
        cb = new UnityEvent();
        cb.AddListener(RemoveEmptyRegions);
        ros = GameObject.Find("ROS").GetComponent<RosNode>();
}
    // Update is called once per frame
    void Update()
    {
        
    }

    public void PrintRegions()
    {
        foreach (Region region in regions)
        {
            Debug.Log("REGION " + region.GetName());
            Dictionary<Vector2, bool> cells = region.GetCells();
            List<Vector2> keys = new List<Vector2>();
            var keycollection = cells.Keys;
            foreach (var key in keycollection)
            {
                Debug.Log("         " + key);
            }
        }
    }

    void LoadMap()
    {
        // load the grid from file
        string path = "Assets/Resources/map.json";
        StreamReader reader = new StreamReader(path);
        string jsonString = reader.ReadToEnd();
        Debug.Log(jsonString);
        reader.Close();
        MapLayout ml = JsonUtility.FromJson<MapLayout>(jsonString);
        gridXDim = ml.width;
        gridYDim = ml.height;
    }

    public void UpdateMaterialDim(Vector2 md) {
        materialDim = md;
        cellDimX = (md.x * 2) / gridXDim;
        cellDimY = (md.y * 2) / gridYDim;
    }

    void RemoveEmptyRegions()
    {
        List<Region> toRemove = new List<Region>();
        foreach (Region reg in regions)
        {
            if (reg.Size() == 0)
            {
                toRemove.Add(reg);
            }
        }
        foreach (Region reg in toRemove)
        {
            regions.Remove(reg);
        }
        Debug.Log(regions.Count);
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

    public Vector2 GridCellToPT(Vector2 cell)
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

    public List<float> GetGridCellBounds(Vector2 cell, Vector2 textureDim)
    {

        // low x, high x, low y, high y
        Vector2 tpt = GridCellToPT(cell);
        List<float> l = new List<float>();
        l.Add(tpt.x); // x low
        l.Add(tpt.x + (10.0f / gridXDim)); // x high
        l.Add(tpt.y - (10.0f / gridYDim)); // y low
        l.Add(tpt.y); // y high
        return l;
    }

    public Region GetCellContents(Vector2 cell)
    {
        // Returns the contents of a cell.
        int row = Mathf.RoundToInt(cell.x);
        int col = Mathf.RoundToInt(cell.y);
        return grid[row, col];
    }

    public Region InitializeCellContents(Vector2 cell)
    {
        // If the cell contents are null, initializes
        //   new contents and places the contents in
        //   the cell.
        // Returns the contents of a cell.
        int row = Mathf.RoundToInt(cell.x);
        int col = Mathf.RoundToInt(cell.y);
        Region contents = GetCellContents(cell);
        if (contents == null)
        {
            // automatically determine initial region name
            string default_name = "new region";
            foreach (Region reg in regions) {
                string name = reg.GetName();
                if (name == default_name) {
                    default_name += "_0";
                }
            }
            contents = new Region(default_name);
            regions.Add(contents);
            grid[row, col] = contents;
            contents.AddCell(cell);
            ros.SendAvailableEntities();
        }
        return contents;
    }

    public void SetCellContents(Vector2 cell, Region reg)
    {
        int row = Mathf.RoundToInt(cell.x);
        int col = Mathf.RoundToInt(cell.y);
        Region contents = grid[row, col];
        if (contents == null)
        {
            grid[row, col] = reg;
            if (reg != null) {
                reg.AddCell(cell);
            }
        } else if (contents != reg)
        {
            contents.SubtractCell(new Vector2(row, col), cb);
            if (reg != null) {
                reg.AddCell(cell);
            }
            grid[row, col] = reg;
        } // else do nothing
    }

    public Dictionary<Vector2, bool> DeleteRegion(Region reg)
    {
        Dictionary<Vector2, bool> cells = reg.GetCells();
        foreach (KeyValuePair<Vector2, bool> entry in cells)
        {
            int row = Mathf.RoundToInt(entry.Key.x);
            int col = Mathf.RoundToInt(entry.Key.y);
            grid[row, col] = null;
        }
        regions.Remove(reg);
        reg.DestroyRegion(cb);
        ros.SendAvailableEntities();
        return cells;
    }

    public Region GetBorderRegion(int row, int col, int radius) {
        Region originalRegion = grid[row, col];
        Region neighbor = null;
        for (int i = row - radius; i < row + radius + 1; i++) {
            if (i < 0 || i >= gridXDim) {
                continue;
            }
            for (int j = col - radius; j < col + radius + 1; j++) {
                if (j < 0 || j >= gridYDim) {
                    continue;
                }
                if (i == j) {
                    continue;
                }
                if (CellDistance(row, col, i, j) > radius) {
                    continue;
                }
                Region reg = grid[i, j];
                if (reg != null && reg != originalRegion) {
                    // we found another region
                    neighbor = reg;
                }
            }
        }
        return neighbor;
    }

    private float CellDistance(int startX, int startY, int endX, int endY)
    {
        return Mathf.Sqrt(Mathf.Pow(endX - startX, 2) + Mathf.Pow(endY - startY, 2));
    }

    public void MakeAllRegionsRaycastTargets(bool makeTargets) {
        List<Region> toRemove = new List<Region>();
        foreach (Region reg in regions)
        {
            reg.SetTextAsRaycastTarget(makeTargets);
        }
    }

    public List<Region> GetRegions()
    {
        return regions;
    }

    public RegionSaveData[] SaveRegions()
    {
        RegionSaveData[] rsd = new RegionSaveData[regions.Count];
        for (int i = 0; i < regions.Count; i++)
        {
            Region reg = regions[i];
            rsd[i] = reg.SaveRegion();
        }
        return rsd;
    }

    public void LoadRegions(RegionSaveData[] regData)
    {
        // destroy all existing regions
        List<Region> temp = new List<Region>();
        foreach (Region reg in regions)
        {
            temp.Add(reg);
        }
        drawer regDraw = GameObject.Find("Drawer").GetComponent<drawer>();
        foreach (Region reg in temp)
        {
            //Dictionary<Vector2, bool> cells = reg.GetCells();
            //List<Vector2> keys = new List<Vector2>();
            //var keycollection = cells.Keys;
            //foreach (var key in keycollection)
            //{
            //    keys.Add(key);
            //}
            //Vector2[] keyArray = keys.ToArray();
            //regDraw.ColorExistingCells(keyArray, Color.clear);
            DeleteRegion(reg);
        }

        // load the new region
        foreach (RegionSaveData regSD in regData)
        {
            Region newReg = new Region(regSD.name);
            newReg.UpdateColor(regSD._color);
            foreach (Vector2 cell in regSD.cells)
            {
                SetCellContents(cell, newReg);
            }
            //regDraw.ColorExistingCells(regSD.cells, regSD._color);
            Vector2 centerCell = newReg.GetCenter();
            Vector2 centerPT = GridCellToPT(centerCell);
            newReg.UpdateTextPosition(centerPT.x, centerPT.y);

            // Added 27 Sept 22 because the loaded regions aren't currently being saved
            regions.Add(newReg);
        }
        ros.SendAvailableEntities();
    }
}

/// <summary>
/// Class <c>Region</c> stores all information relevant to a region,
///  including the cells included in the region and all display
///  information.
/// </summary>
public class Region
{
    private string name;
    private GameObject textObj;
    private GameObject textInputObj;
    private Text textDisplay;
    private ContentSizeFitter textContentFitter;
    private InputField textInput; 
    private Dictionary<Vector2, bool> cells;
    private Color _color;
    private GameObject canvas;
    private GameObject contentPanel;
    public GameObject regionInputPrefab;

    // for updating ros nodes
    private RosNode ros;

    public Region(string name)
    {
        this.name = name;
        canvas = GameObject.Find("InputTextCanvas");
        contentPanel = GameObject.Find("ContentPanel");
        textObj = new GameObject("RegionName_"+name);
        textObj.transform.SetParent(canvas.transform);
        textObj.transform.localPosition = new Vector3(0,0,0);
        TextInfo ti1 = textObj.AddComponent<TextInfo>();
        ti1.reg = this;
        textContentFitter = textObj.AddComponent<ContentSizeFitter>();
        textContentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        textDisplay = textObj.AddComponent<Text>();
        textDisplay.text = name;
        textDisplay.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        textDisplay.color = Color.black;
        textDisplay.fontSize = 32;
        textObj.SetActive(false);

        regionInputPrefab = Resources.Load("RegionInputField") as GameObject;
        textInputObj = GameObject.Instantiate(regionInputPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        textInputObj.transform.SetParent(canvas.transform);
        textInputObj.transform.localPosition = new Vector3(0, 0, 0);
        textInputObj.transform.localScale = new Vector3(0.005f, 0.005f, 1f);
        textInputObj.transform.Find("Placeholder").GetComponent<Text>().text = "Enter text...";
        textInputObj.GetComponent<InputField>().text = name;
        textInputObj.GetComponent<InputField>().onValueChanged.AddListener(delegate { ValueChangeCheck(); });
        TextInfo ti2 = textInputObj.AddComponent<TextInfo>();
        ti2.reg = this;
        textInputObj.SetActive(true);


        //textDisplay.raycastTarget = false;
        cells = new Dictionary<Vector2, bool>();
        _color = new Color(Random.Range(0.0f, 0.6f), Random.Range(0.0f, 0.6f), Random.Range(0.0f, 0.6f), 0.4f);

        ros = GameObject.Find("ROS").GetComponent<RosNode>();

    }

    public void ValueChangeCheck()
    {
        this.name = textInputObj.GetComponent<InputField>().text;
        ros.SendAvailableEntities();

    }

    public void UpdateColor(Color c)
    {
        _color = c;
    }

    public void AddCell(Vector2 cell)
    {
        cells.Add(cell,true);
    }

    public void SubtractCell(Vector2 cell, UnityEvent ev)
    {
        cells.Remove(cell);
        if (cells.Count == 0)
        {
            DestroyRegion(ev);
        }
    }

    public void DestroyRegion(UnityEvent ev)
    {
        Debug.Log("NO MORE REGION");
        UnityEngine.Object.Destroy(textObj);
        UnityEngine.Object.Destroy(textInputObj);
        ev.Invoke();
    }

    public int Size()
    {
        return cells.Count;
    }

    public void UpdateTextPosition(float x, float y) {
        RectTransform rt = textInputObj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        Debug.Log(textInputObj.GetComponent<RectTransform>().anchoredPosition);
    }

    public void SetTextAsRaycastTarget(bool makeTarget) {
        textDisplay.raycastTarget = makeTarget;
    }

    public void SwitchToTextInput() {
        textInputObj.SetActive(true);
        textObj.SetActive(false);
    }

    public void SwitchToText() {
        textInputObj.SetActive(false);
        textObj.SetActive(true);
    }

    // getters
    public Color GetColor()
    {
        return _color;
    }

    public String GetName()
    {
        return this.name;
    }

    public Dictionary<Vector2, bool> GetCells() {
        return cells;
    }

    public Vector2 GetCenter() {
        List<Vector2> vects = new List<Vector2>(cells.Keys);
        return new Vector2(vects.Average(x=>x.x), vects.Average(x=>x.y));
    }

    public RegionSaveData SaveRegion()
    {
        RegionSaveData rsd = new RegionSaveData();
        rsd.name = name;
        List<Vector2> keys = new List<Vector2>();
        var keycollection = cells.Keys;
        foreach (var key in keycollection)
        {
            keys.Add(key);
        }
        Vector2[] keyArray = keys.ToArray();
        rsd.cells = keyArray;
        rsd._color = _color;
        return rsd;
    }
    
}

public class TextInfo : MonoBehaviour
{
    public Region reg;
}

[System.Serializable]
public class RegionSaveData
{
    public string name;
    public Vector2[] cells;
    public Color _color;
}

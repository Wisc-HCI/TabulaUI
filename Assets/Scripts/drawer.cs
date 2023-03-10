using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

/// <summary>
/// Class <c>drawer</c> facilitates the drawing of regions.
/// </summary>
public class drawer : MonoBehaviour
{
    public Camera m_camera;
    public RegionGrid grid;

    // public Image panelImage;
    // public RenderTexture rText;

    private Texture2D texture;
    private Renderer renderer;
    private int penSize;
    private Color[] _colors;
    private Color[] _edgeColors;
    private Vector2 materialDim; // the dimensions of the material
    private Vector2 textureDim = new Vector2(1024, 1024);

    // draw modes and pen sizes
    private ModeState currModeState;
    private int borderSize = 2;
    private int largePenSize = 48;
    private int mediumPenSize = 32;
    private int smallPenSize = 16;

    Region currReg; 
    /*
     * Added this class so we can identify the GameObject underneath the EventSystem's pointer
    */

    void Start()
    {
        texture = new Texture2D(Mathf.RoundToInt(textureDim.x), Mathf.RoundToInt(textureDim.y));
        
        // Debug.Log("panelImage parent = " + panelImage.transform.parent.name);
        // texture = new RenderTexture(Mathf.RoundToInt(textureDim.x), Mathf.RoundToInt(textureDim.y));
       
        renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = texture;

        // texture status update
        bool null_check = false;
        null_check = (texture == null);
        
        // added as Texture 2D
        renderer.material.SetColor("_Color", new Color(0.3f, 0.4f, 0.6f, 0.3f));
        UpdatePenSize(5);
        materialDim = new Vector2(renderer.bounds.min.x * -1, renderer.bounds.min.y * -1);
        grid.UpdateMaterialDim(materialDim);
        currReg = null;

        // link the mode state
        currModeState = GameObject.Find("ModeState").GetComponent<ModeState>();

        // make the material transparent
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color color = Color.clear;
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
    }

    public void UpdatePenSize(int size) {
        penSize = size;
        _edgeColors = Enumerable.Repeat(new Color(0.0f, 0.0f, 0.0f, 0.5f), penSize * penSize).ToArray();
        _colors = Enumerable.Repeat(renderer.material.color, penSize * penSize).ToArray();
    }

    private void Update()
    {
        Draw();
    }

    void Draw()
    {
        // do NOT do anything if the current mode is not DRAW
        if (currModeState.GetMainMode() != ModeState.MainMode.Draw)
        {
            return;
        }
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            if (currReg != null)
            {
                Vector2 centerCell = currReg.GetCenter();
                Vector2 centerPT = grid.GridCellToPT(centerCell);
                currReg.UpdateTextPosition(centerPT.x, centerPT.y);
            }
            currReg = null;
            // set all text to be raycast targets
            grid.MakeAllRegionsRaycastTargets(true);
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            // determine if text is touched
            Vector2 pos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = pos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool continueToDraw = false;
            if (results.Count > 0)
            {
                continueToDraw = false;
                for (int i = 0; i < results.Count; i++)
                {
                    //Debug.Log(results[i].gameObject.name);
                    // determine if button is touched
                    if (results[i].gameObject.name.Contains("Brush") || results[i].gameObject.name.Contains("Erase")
                                                                        || results[i].gameObject.name.Contains("Draw") ||
                                                                        results[i].gameObject.name.Contains("Select") || 
                                                                        results[i].gameObject.name.Contains("Program Mode") ||
                                                                        results[i].gameObject.name.Contains("Mode Toggler") ||
                                                                        results[i].gameObject.name.Contains("Request Map") ||
                                                                        results[i].gameObject.name.Contains("Save") ||
                                                                        results[i].gameObject.name.Contains("Load") )
                    {
                        return;
                    }

                    if (results[i].gameObject.name.Contains("RegionName_"))
                    {
                        //Debug.Log("TOUCHED!!!!!!!");
                        //Region reg = results[i].gameObject.GetComponent<TextInfo>().reg;
                        //reg.SwitchToTextInput();
                    }

                    if (results[i].gameObject.name.Contains("Background") || results[i].gameObject.name.Contains("ContentPanel"))
                    {
                        continueToDraw = true;
                    }
                }
            }
            if (!continueToDraw)
            {
                return;
            }

            Vector3 screenMousePos = Input.mousePosition; // (0,0) is bottom left
            screenMousePos.z = 6.0f; // m_camera.nearClipPlane;
            Vector2 worldMousePos = m_camera.ScreenToWorldPoint(screenMousePos); // (0,0) is center of world
            // must convert world position to grid position
            // now I need to go from "world point" to local space
            Vector3 localMousePosVect3 = renderer.transform.InverseTransformPoint(worldMousePos);
            Vector2 localMousePos = new Vector2(localMousePosVect3.x, localMousePosVect3.z);
            Vector2 cell = grid.PTToGridCell(localMousePos, materialDim);

            // first, test to see whether the region eraser is selected and a region was clicked.
            // If so, just erase the whole region and return from this method.
            if (currModeState.GetDrawMode() == ModeState.DrawMode.Erase && currModeState.GetEraseMode() == ModeState.EraseMode.Region)
            {
                Region reg = grid.GetCellContents(cell);
                if (reg == null)
                {
                    return;
                }
                Dictionary<Vector2, bool> cells = grid.DeleteRegion(reg);

                // remove all non-border cells, keepnig track of which ones to set later
                Dictionary<Vector2, Region> setLater = new Dictionary<Vector2, Region>();
                foreach (KeyValuePair<Vector2, bool> entry in cells)
                {
                    int r = Mathf.RoundToInt(entry.Key.x);
                    int c = Mathf.RoundToInt(entry.Key.y);
                    Vector2 newCell = new Vector2(r, c);
                    List<float> bounds = grid.GetGridCellBounds(newCell, materialDim);
                    Color[] clearColor = Enumerable.Repeat(Color.clear, 6 * 6).ToArray();
                    Pair lowCoord = WorldPTToTexture(new Vector2(bounds[0], bounds[2]));
                    Pair highCoord = WorldPTToTexture(new Vector2(bounds[1], bounds[3]));
                    if (texture.GetPixel(lowCoord.x, lowCoord.y).a > 0.4)
                    {
                        // determine whether to switch cell to other region or not
                        // if the region is with in <borderSize>, switch it.
                        // Otherwise, delete it.
                        Region neighbor = grid.GetBorderRegion(r, c, borderSize);
                        if (neighbor != null)
                        {
                            // switch the r and c grid cell to the neighbor region
                            Vector2 sourceCell = new Vector2(r, c);
                            setLater.Add(sourceCell, neighbor);
                            //grid.SetCellContents(sourceCell, neighbor);
                            continue;
                        }
                    }
                    texture.SetPixels(lowCoord.x, lowCoord.y, highCoord.x - lowCoord.x, highCoord.y - lowCoord.y, clearColor);
                }
                // switch any cells as necessary
                foreach (KeyValuePair<Vector2, Region> entry in setLater)
                {
                    Vector2 sourceCell = entry.Key;
                    Region neighbor = entry.Value;
                    grid.SetCellContents(sourceCell, neighbor);
                }
                texture.Apply();

                return;
            }

            // get the pen size
            if (currModeState.GetDrawMode() == ModeState.DrawMode.Brush && currModeState.GetBrushMode() == ModeState.BrushMode.Large)
            {
                UpdatePenSize(largePenSize);
            }
            if (currModeState.GetDrawMode() == ModeState.DrawMode.Brush && currModeState.GetBrushMode() == ModeState.BrushMode.Medium)
            {
                UpdatePenSize(mediumPenSize);
            }
            if (currModeState.GetDrawMode() == ModeState.DrawMode.Brush && currModeState.GetBrushMode() == ModeState.BrushMode.Small)
            {
                UpdatePenSize(smallPenSize);
            }
            if (currModeState.GetDrawMode() == ModeState.DrawMode.Erase && currModeState.GetEraseMode() == ModeState.EraseMode.Large)
            {
                UpdatePenSize(largePenSize);
            }
            if (currModeState.GetDrawMode() == ModeState.DrawMode.Erase && currModeState.GetEraseMode() == ModeState.EraseMode.Medium)
            {
                UpdatePenSize(mediumPenSize);
            }
            if (currModeState.GetDrawMode() == ModeState.DrawMode.Erase && currModeState.GetEraseMode() == ModeState.EraseMode.Small)
            {
                UpdatePenSize(smallPenSize);
            }

            // get region associated with cell
            if (currModeState.GetDrawMode() == ModeState.DrawMode.Select)
            {
                // do nothing
            } else if (currModeState.GetDrawMode() == ModeState.DrawMode.Erase)
            {
                //currReg = null;
            } else if (currReg == null)
            {
                currReg = grid.InitializeCellContents(cell);
                grid.MakeAllRegionsRaycastTargets(false);
                _colors = Enumerable.Repeat(currReg.GetColor(), penSize * penSize).ToArray();
            } else
            {
                _colors = Enumerable.Repeat(currReg.GetColor(), penSize * penSize).ToArray();
            }

            if (currModeState.GetDrawMode() == ModeState.DrawMode.Brush)
            {
                int roundedX = Mathf.RoundToInt(cell.x);
                int roundedY = Mathf.RoundToInt(cell.y);
                Color[] color;
                for (int r = roundedX - penSize; r < roundedX + penSize; r++)
                {
                    for (int c = roundedY - penSize; c < roundedY + penSize; c++)
                    {
                        Vector2 newCell = new Vector2(r, c);
                        float dist = distance(cell, newCell);
                        if (dist > penSize)
                        {
                            continue;
                        } else if (dist > (penSize - borderSize))
                        {
                            // if the new cell is already part of the region, do nothing
                            if (grid.GetCellContents(cell) == grid.GetCellContents(newCell))
                            {
                                continue;
                            }
                            // else, set color to edge color
                            color = _edgeColors;
                        } else
                        {
                            color = _colors;
                        }
                        grid.SetCellContents(newCell, currReg);
                        ColorGridCell(newCell, color);
                    }
                }
            } 
            else if (currModeState.GetDrawMode() == ModeState.DrawMode.Erase)
            {
                int roundedX = Mathf.RoundToInt(cell.x);
                int roundedY = Mathf.RoundToInt(cell.y);
                Color[] clearColor = Enumerable.Repeat(Color.clear, penSize * penSize).ToArray();
                Color[] color;
                for (int r = roundedX - (penSize + borderSize); r < roundedX + (penSize + borderSize); r++)
                {
                    for (int c = roundedY - (penSize + borderSize); c < roundedY + (penSize + borderSize); c++)
                    {
                        Vector2 newCell = new Vector2(r, c);
                        Region reg = grid.GetCellContents(newCell);
                        if (reg != null)
                        {
                            currReg = reg;
                            grid.MakeAllRegionsRaycastTargets(true);
                        }
                        float dist = distance(cell, newCell);
                        if (dist <= penSize)
                        {
                            if (grid.GetCellContents(newCell) == null)
                            {
                                continue;
                            }
                            grid.SetCellContents(newCell, null);
                        } else if (dist > penSize + borderSize)
                        {
                            continue;
                        }
                        color = clearColor;
                        if (dist > penSize)
                        {
                            // handle the case in which another region is present (color it dark)
                            if (grid.GetCellContents(newCell) != null)
                            {
                                color = _edgeColors;
                            }
                        }
                        ColorGridCell(newCell, color);
                    }
                }
            }
            texture.Apply();

            // texture status update
            bool null_check = false;
            null_check = (texture == null);
        }
    }

    private void ColorGridCell(Vector2 newCell, Color[] color) {
        List<float> bounds = grid.GetGridCellBounds(newCell, materialDim);
        Pair lowCoord = WorldPTToTexture(new Vector2(bounds[0], bounds[2]));
        Pair highCoord = WorldPTToTexture(new Vector2(bounds[1], bounds[3]));
        texture.SetPixel(lowCoord.x, lowCoord.y, color[0]);
    }

    public void ColorExistingCells(Vector2[] cells, Color color)
    {
        foreach (Vector2 cell in cells)
        {
            ColorExistingCell(cell, color);
        }
        texture.Apply();
    }

    public void ColorExistingCell(Vector2 cell, Color color)
    {
        Color[] colors = { color };
        ColorGridCell(cell, colors);
    }

    private float distance(Vector2 start, Vector2 end)
    {
        return Mathf.Sqrt(Mathf.Pow(end.x - start.x, 2) + Mathf.Pow(end.y - start.y, 2));
    }

    private Pair WorldPTToTexture(Vector2 pt)
    {
        int x = Mathf.RoundToInt(((pt.x + materialDim.x) / (materialDim.x * 2)) * textureDim.x);
        int y = Mathf.RoundToInt(((pt.y + materialDim.y) / (materialDim.y * 2)) * textureDim.y);
        return new Pair(y, x);
    }

    public Color32Data[] SaveColors()
    {
        Color32[] colors = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;
        Color32Data[] Color32Datas = new Color32Data[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            Color32Datas[i] = new Color32Data();
            Color32Datas[i].a = colors[i].a;
            Color32Datas[i].r = colors[i].r;
            Color32Datas[i].g = colors[i].g;
            Color32Datas[i].b = colors[i].b;
        }

        return Color32Datas;
    }

    public void LoadColors(Color32Data[] cds)
    {
        Color32[] pixels = new Color32[cds.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(cds[i].r, cds[i].g, cds[i].b, cds[i].a);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
    }
}

public class Pair
{
    public int x;
    public int y;
    public Pair(int x, int y)
     {
         this.x = x;
         this.y = y;
     }
}

[System.Serializable]
public class Color32Data
{
    public byte a;
    public byte r;
    public byte g;
    public byte b;
}

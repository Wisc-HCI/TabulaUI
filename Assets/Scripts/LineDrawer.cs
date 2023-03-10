using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;
using Object = System.Object;

/// <summary>
/// Class <c>LineDrawer</c> facilitates the drawing of programs.
///  Also converts lines to Bezier curves when applicable.
/// </summary>
public class LineDrawer : MonoBehaviour
{
    public Camera m_camera;
    public GameObject brush;

    LineRenderer currentLineRenderer;
    GameObject currentBrushInstance;

    public GameObject wpPrefab;
    public GameObject arrowPrefab;
    public GameObject startWpPrefab;

    Vector2 lastPos;
    Vector2 lastWaypointPos;

    private ModeState currModeState;
    private StateStorage stateStorage;
    private GameObject contentPanel;

    private float closeByRadius;

    private float pauseTime = 0.0f;
    private List<GameObject> trajectoryComponents;

    private ZoomController zoomController;
    private float zoomScale;
    
    private float distance; // used for debugging output
    private float offsetConstant; // currently 1, could be adjusted if needed
    private int numLinesDrawn;

    private Color recordingColor;
    private Color previousPrimaryColor;
    private Color32[] colorOptions;
    private int currColorIndex;

    private Dictionary<GameObject, int> waypointToID;
    private int currWpCounter;
    
    const float ERROR_THRESHOLD = 0.1f;
    List<Vector3> samplePoints = new List<Vector3>();
    
    // when selected in the Inspector, simple curves are automatically simplified to a Bezier curve with fewer points
    [SerializeField] private bool applyBezier;

    [SerializeField] private bool drawOriginalLine;

    private void Start()
    {
        // link the mode state
        currModeState = GameObject.Find("ModeState").GetComponent<ModeState>();
        stateStorage = GameObject.Find("StateStorage").GetComponent<StateStorage>();

        // other useful gameobjects
        contentPanel = GameObject.Find("ContentPanel");

        // temporary storage
        trajectoryComponents = new List<GameObject>();
        
        // zoom controller
        GameObject scroll = GameObject.Find("Scroll");
        if (scroll != null)
        {
            zoomController = scroll.GetComponent<ZoomController>();
            if (zoomController != null)
            {
                zoomScale = zoomController.GetCurrentScale();
            }
        }

        // track IDs of waypoints
        waypointToID = new Dictionary<GameObject, int>();
        currWpCounter = 0;
        
        // offset constant
        offsetConstant = 1f;
        
        // closeByRadius
        closeByRadius = 0.25f;

        numLinesDrawn = 0;

        // colors
        recordingColor = Color.red;
        previousPrimaryColor = Color.clear;
        //selection of colorblind safe colors
        colorOptions = new Color32[] { new Color32(0, 119, 187, 255), new Color32(51, 187, 238, 255), new Color32(0, 153, 136, 255), new Color32(238, 119, 51, 255), new Color32(204, 51, 17, 255), new Color32(238, 51, 119, 255), new Color32(187, 187, 187, 255) };
        currColorIndex = 0;
    }

    private void Update()
    {
        if(zoomController != null){ zoomScale = zoomController.GetCurrentScale();}
        if (currModeState.GetMainMode() != ModeState.MainMode.Program)
        {
            return;
        }
        else if (currModeState.GetProgramMode() != ModeState.ProgramMode.Recording)
        {
            return;
        }
        Draw();
    }

    // randomize line color
    private void SetRandomRecordingColor()
    {
        // set r or g or b as primary
        bool pickingColor = true;
        while (pickingColor)
        {
            recordingColor = colorOptions[currColorIndex];
            pickingColor = false;
            currColorIndex++;
            if (currColorIndex == colorOptions.Length)
            {
                currColorIndex = 0;
            }
            //float rgb_picker = Random.Range(0.0f, 3.0f);
            //if (rgb_picker < 1.0f && previousPrimaryColor != Color.red)
            //{
            //    recordingColor = new Color(1.0f, 0.0f, Random.Range(0.0f, 1.0f), 1.0f);
            //    previousPrimaryColor = Color.red;
            //    pickingColor = false;
            //}
            //else if (rgb_picker < 2.0f && previousPrimaryColor != Color.green)
            //{
            //    recordingColor = new Color(0.0f, 1.0f, Random.Range(0.0f, 1.0f), 1.0f);
            //    previousPrimaryColor = Color.green;
            //    pickingColor = false;
            //}
            //else if (rgb_picker < 3.0f && previousPrimaryColor != Color.blue)
            //{
            //    recordingColor = new Color(Random.Range(0.0f, 1.0f), 0.0f, 1.0f, 1.0f);
            //    previousPrimaryColor = Color.blue;
            //    pickingColor = false;
            //}
        }
    }



    // updates the zoomScale in the Inspector
    private void UpdateZoomScale()
    {
        // range of Zoom: 0.6f to 2f
        if(zoomController != null){ zoomScale = zoomController.GetCurrentScale();}
    }

    private bool IsPointerOverBlockingObject()
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
                if (results[i].gameObject.name.Contains("Brush") || results[i].gameObject.name.Contains("Record")
                                                                    || results[i].gameObject.name.Contains("Draw") ||
                                                                    results[i].gameObject.name.Contains("Select") ||
                                                                    results[i].gameObject.name.Contains("Program Mode") ||
                                                                    results[i].gameObject.name.Contains("Mode Toggler"))
                {
                    return true;
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
            return true;
        }
        return false;
    }

    void AddInitialWaypoint()
    {
        Debug.Log("ADDING WAYPOINT");
        GameObject waypoint = Instantiate(startWpPrefab, lastPos, Quaternion.identity);
        waypointToID.Add(waypoint, currWpCounter);
        currWpCounter += 1;
        waypoint.name = stateStorage.GetRecordingCounter().ToString() + "_Waypoint" + stateStorage.GetTrace().Count.ToString();
        waypoint.transform.SetParent(contentPanel.transform);
        waypoint.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        lastWaypointPos = lastPos;
        stateStorage.AddRegion(waypoint);
        trajectoryComponents.Add(waypoint);
        Handheld.Vibrate();
    }

    void AddWaypoint()
    {
        Debug.Log("ADDING WAYPOINT");
        GameObject waypoint = Instantiate(wpPrefab, lastPos, Quaternion.identity);
        waypointToID.Add(waypoint, currWpCounter);
        currWpCounter += 1;
        waypoint.name = stateStorage.GetRecordingCounter().ToString() + "_Waypoint" + stateStorage.GetTrace().Count.ToString();
        waypoint.transform.SetParent(contentPanel.transform);
        waypoint.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        lastWaypointPos = lastPos;
        stateStorage.AddRegion(waypoint);
        trajectoryComponents.Add(waypoint);
        Handheld.Vibrate();
    }

    public int GetWpID(GameObject wp)
    {
        return waypointToID[wp];
    }

    void AddArrow()
    {
        GameObject arrow = Instantiate(arrowPrefab, GetLinePositionAtWaypoint(currentLineRenderer), Quaternion.identity);
        arrow.name = stateStorage.GetRecordingCounter().ToString() + "_" + arrow.name;
        arrow.GetComponent<SpriteRenderer>().color = recordingColor;
        float rot = GetLineRotationAtWaypoint(currentLineRenderer);
        Debug.Log("ROT: " + rot);
        arrow.transform.Rotate(0, 0, rot, Space.Self);
        arrow.transform.SetParent(contentPanel.transform);
        arrow.transform.localScale = new Vector3(0.50f, 0.50f, 1f);
        trajectoryComponents.Add(arrow);
    }

    bool IsFirstWaypoint()
    {
        List<string> ignoreList = new List<string> { "ObjectPanel", "BackgroundMap", "Drawer", "InputTextCanvas" };
        GameObject cp = GameObject.Find("ContentPanel");

        for (int i = 0; i < cp.transform.childCount; i++)
        {
            string childName = cp.transform.GetChild(i).gameObject.name;
            if (childName.Contains("Waypoint"))
            {
                return false;
            }
        }
        return true;
    }

    void Draw()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            SetRandomRecordingColor();
            if (IsPointerOverBlockingObject())
            {
                return;
            }

            //NewBrush(); why is this line here?
            Vector3 rawMousePos = Input.mousePosition;
            rawMousePos.z = 6.0f;
            Vector2 mousePos = m_camera.ScreenToWorldPoint(rawMousePos);
            lastPos = mousePos;
            GameObject closeBy = IsCloseToOtherWaypoint(lastPos);
            trajectoryComponents.Clear();
            if (closeBy == null)
            {
                // if it's the very first waypoint, add the start symbol
                if (IsFirstWaypoint())
                {
                    AddInitialWaypoint();
                }
                else
                {
                    AddWaypoint();
                }
            }
            else {
                Debug.Log("STARTING CLOSEBY");
                stateStorage.AddRegion(closeBy);
                trajectoryComponents.Add(closeBy);
            }
            NewBrush();
        }
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            Debug.Log("trajectoryComponents.Count: " + trajectoryComponents.Count);
            GameObject closeBy = IsCloseToOtherWaypoint(lastPos);
            Debug.Log("closeBy: " + closeBy);
            Debug.Log(GetLineLength(currentLineRenderer)/zoomScale);
            if (closeBy == null && currentLineRenderer != null && (GetLineLength(currentLineRenderer)/zoomScale) > closeByRadius)
            {
                trajectoryComponents.Add(currentBrushInstance);
                AddWaypoint();
                if (applyBezier)
                {
                    ApplyBezier(currentLineRenderer);
                }
                CleanUpLine(IsCloseToOtherWaypoint(lastPos)); // connects lines to new waypoints
                AddArrow();
            }
            else if (closeBy != null && currentLineRenderer != null && (GetLineLength(currentLineRenderer)/zoomScale) > closeByRadius)
            {
                trajectoryComponents.Add(currentBrushInstance);
                trajectoryComponents.Add(closeBy);
                stateStorage.AddRegion(closeBy);
                if (applyBezier)
                {
                    ApplyBezier(currentLineRenderer);
                }
                CleanUpLine(closeBy);
                AddArrow();
            }
            // if there's only one waypoint and no trajectory, delete
            //Debug.Log("COMPONENT COUNT: " + trajectoryComponents.Count.ToString());
            if (trajectoryComponents.Count < 4)
            {
                Debug.Log("DESTROY");
                foreach (GameObject g in trajectoryComponents)
                {
                    Debug.Log("DESTROYING: " + g);
                    Destroy(g);
                }
                trajectoryComponents.Clear();
                Destroy(currentLineRenderer);
            }
        }
        if (Input.GetKey(KeyCode.Mouse0))
        {
            if (IsPointerOverBlockingObject())
            {
                return;
            }

            /*
             * Rules: 
             * - each line must have a starting waypoint and an ending waypoint.
             * - lines w/o either will be discarded.
             */
            Vector3 rawMousePos = Input.mousePosition;
            rawMousePos.z = 6.0f;
            Vector2 mousePos = m_camera.ScreenToWorldPoint(rawMousePos);
            if (currentLineRenderer == null)
            {
                lastPos = mousePos;
            }
            if (GetDistance(mousePos, lastPos) > 0.001 && currentLineRenderer != null)
            {
                pauseTime = -1.0f;
                AddPt(mousePos);
                lastPos = mousePos;
            }
            else
            {
                if (pauseTime == -1.0f)
                {
                    pauseTime = Time.time;
                }
                if (Time.time - pauseTime < 0.1f)
                {
                    return;
                }
                // first see if there is an existing waypoint that is close to our current waypoint
                GameObject closeBy = IsCloseToOtherWaypoint(lastPos);
                if (closeBy == null)
                {
                    Debug.Log("closeBy is null");
                    trajectoryComponents.Add(currentBrushInstance);
                    if (applyBezier)
                    {
                        ApplyBezier(currentLineRenderer);
                    }
                    AddArrow();
                    AddWaypoint();
                    NewBrush(); // may need to comment out
                }
                else
                {
                    // see if it hasn't already been added
                    if (stateStorage.GetEndOfTrace() == null || !GameObject.ReferenceEquals(stateStorage.GetEndOfTrace(), closeBy))
                    {
                        Debug.Log("SAME!");
                        trajectoryComponents.Add(currentBrushInstance);
                        if (applyBezier)
                        {
                            ApplyBezier(currentLineRenderer);
                        }
                        AddArrow();
                        stateStorage.AddRegion(closeBy);
                        trajectoryComponents.Add(closeBy);
                        // NewBrush(); // may need to comment out
                    }
                }
            }
        }
        else
        {
            currentLineRenderer = null;
            pauseTime = -1.0f;
        }

        GameObject IsCloseToOtherWaypoint(Vector3 lastPos)
        {
            GameObject toReturn = null;
            GameObject[] wps = GameObject.FindGameObjectsWithTag("Waypoint");
            
            float toCompare = 0;
            
            for (int i = 0; i < wps.Length; i++)
            {
                Vector3 pos = wps[i].transform.position;
                float currentDistance = GetDistance(pos, lastPos);

                toCompare = (currentDistance/zoomScale) * offsetConstant;
                
                if (toCompare <= closeByRadius)
                {
                    toReturn = wps[i];
                    distance = toCompare;
                    break;
                }
            }
            
            return toReturn;
        }

        void NewBrush()
        {
            GameObject newBrush = Instantiate(brush);
            newBrush.name = stateStorage.GetRecordingCounter().ToString() + "_" + newBrush.name;
            newBrush.transform.SetParent(contentPanel.transform);
            newBrush.transform.SetParent(contentPanel.transform);
            currentLineRenderer = newBrush.GetComponent<LineRenderer>();
            currentBrushInstance = newBrush;

            currentLineRenderer.sortingOrder = 1;
            currentLineRenderer.startColor = currentLineRenderer.endColor = recordingColor;

            Vector3 rawMousePos = Input.mousePosition;
            rawMousePos.z = 6.0f;//m_camera.nearClipPlane;
            Vector2 mousePos = m_camera.ScreenToWorldPoint(rawMousePos);
            currentLineRenderer.SetPosition(0, mousePos);
        }

        void AddPt(Vector2 newPt)
        {
            currentLineRenderer.positionCount++;
            currentLineRenderer.SetPosition(currentLineRenderer.positionCount - 1, newPt);
        } 
        
        // connects end of currentLineRenderer to the closest waypoint's center
        void CleanUpLine(GameObject closestWayPoint)
        {
            Debug.Log("CleanUpLine called");
            int numPoints = currentLineRenderer.positionCount;
            numLinesDrawn++;
            // could we set the position of the line's first point to the original line?
            currentLineRenderer.SetPosition(numPoints-1, closestWayPoint.transform.position);
        }
        
        if (drawOriginalLine)
        {
            DrawOriginalLine(currentBrushInstance);
        }
    }

    /*
     * This function saves the original points in the user-drawn line, fits that line to one or more Bezier curves, and then draws the Bezier curve
     *  when the user-drawn line fits one Bezier curve
     * 
     *  Code inspired by thomas-allen on github (see https://github.com/thomas-allen/curve-fitter) 
     *  The relevant commit hash is 61f0024ec870b03289a1f04b49ead62c9bd8ecd0
     * 
     *  The original thomas-allen code is licensed under the MIT license. The license can be
     *  found in the third_party_licenses.txt under "curve-fitter".
     */
    private void ApplyBezier(LineRenderer line)
    {
        Vector3[] points = new Vector3[line.positionCount];
        line.GetPositions(points);
        Debug.Log("number of Points: " + points.Length);
        List<Vector3> samplePoints = new List<Vector3>(points);
        Debug.Log("number of sample points: " + samplePoints.Count);
        List<BezierCurve> bezierCurves = CurveFitter.FitCurve(samplePoints, ERROR_THRESHOLD);
        // The code below prints Size, number of Bezier curves in the original line
        // Debug.Log("Bezier curves" + bezierCurves + "\tSize: " + bezierCurves.Count);

        // currently, automatic line fitting works when there's one curve to smooth
        if (bezierCurves.Count == 1)
        {
            // Store the old positions in the LineBrush object
            PositionsManager manager = currentBrushInstance.GetComponent<PositionsManager>();
            manager.SetOldPositions(points);
            
            // if bezierCurves.Count > 1, the code below will only draw the last curve in bezierCurves
            foreach (BezierCurve curve in bezierCurves)
            {
                BezierCurve bez_curve = curve;
                List<Vector3> bez_curve_pts_list = curve.GetControlPoints();
                Vector3 pt0 = bez_curve_pts_list[0];
                Vector3 pt1 = bez_curve_pts_list[1];
                Vector3 pt2 = bez_curve_pts_list[2];
                Vector3 pt3 = bez_curve_pts_list[3];
                DrawBezierCurve(line, pt0, pt1, pt2, pt3);
            }
        }
    }
    private float GetDistance(Vector2 newPos, Vector2 lastPos)
    {
        return Mathf.Sqrt(Mathf.Pow(newPos.x - lastPos.x, 2) + Mathf.Pow(newPos.y - lastPos.y, 2));
    }
    
    /*
     * Draws the curve based on the four points generated from the curve fitter
     */
    void DrawBezierCurve(LineRenderer line, Vector3 P1, Vector3 T1, Vector3 T2, Vector3 P2)
    {
        line.positionCount = line.positionCount / 4;
        Vector3 bezier_t = new Vector3(0, 0, 0);
        for (int i = 0; i < line.positionCount; i++)
        {
            float t = i / (float)line.positionCount;
            // parametric bezier curve equation sourced from:
            // https://apoorvaj.io/cubic-bezier-through-four-points/
            bezier_t = P1 * (1 - t)*(1 - t)*(1 - t)
                     + T1 * 3 * (1 - t)*(1 - t) * t
                     + T2 * 3 * (1 - t) * t*t
                     + P2 * t*t*t;
        
            line.SetPosition(i, bezier_t);
        }
        line.startColor = line.endColor = recordingColor;
    }
    
    /*
     * Redraws the original line if ApplyBezier has been applied
     */
    void DrawOriginalLine(GameObject brushInstance)
    {
        PositionsManager manager = brushInstance.GetComponent<PositionsManager>();
        if (manager == null || manager.GetOldPositions().Length == 0) return;

        Vector3[] originalPositions = manager.GetOldPositions();
        LineRenderer lineRenderer = brushInstance.GetComponent<LineRenderer>();
        if (lineRenderer == null) return;

        lineRenderer.positionCount = originalPositions.Length;
        lineRenderer.SetPositions(originalPositions);
        lineRenderer.startColor = lineRenderer.endColor = Color.green;
    }

    /*
     * Returns the length (Euclidean distance) of a line
     */
    float GetLineLength(LineRenderer line)
    {
        if (line == null)
        {
            return -1; // should throw an exception
        }
        
        float totalLength = 0;

        Vector3[] pointsInLine;
        pointsInLine = new Vector3[line.positionCount]; 
        line.GetPositions(pointsInLine);

        for (int i = 0; i < pointsInLine.Length-1; i++)
        {
            totalLength = totalLength + (float) Math.Sqrt((float) Math.Pow((pointsInLine[i + 1].x - pointsInLine[i].x), 2)
                                      + (float) Math.Pow((pointsInLine[i + 1].y - pointsInLine[i].y), 2)); 
        }
        
        return totalLength;
    }

    private Vector3 GetLinePositionAtWaypoint(LineRenderer lineRenderer)
    {
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(positions);
        Vector3 finalPos = positions[positions.Length - 1];
        Vector3 currPos = positions[positions.Length - 1];
        Vector3 prevPos = positions[positions.Length - 1];
        for (int i = positions.Length - 2; i >= 0; i--)
        {
            prevPos = currPos;
            currPos = positions[i];
            float dist = Vector3.Distance(finalPos, currPos);
            if (dist > 0.3f * contentPanel.transform.localScale.x)
            {
                break;
            }
        }
        return Vector3.Lerp(currPos, prevPos, 0.5f);
    }

    private float GetLineRotationAtWaypoint(LineRenderer lineRenderer)
    {
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(positions);
        Vector3 finalPos = positions[positions.Length - 1];
        Vector3 currPos = positions[positions.Length - 1];
        Vector3 prevPos = positions[positions.Length - 1];
        int i;
        float dist;
        for (i = positions.Length - 2; i >= 0; i--)
        {
            prevPos = currPos;
            currPos = positions[i];
            dist = Vector3.Distance(finalPos, currPos);
            if (dist > 0.3f * contentPanel.transform.localScale.x)
            {
                break;
            }
        }
        Vector3 point = Vector3.Lerp(currPos, prevPos, 0.5f);
        Vector3 nextPos = point;
        dist = 0;
        while (dist < 0.1 * contentPanel.transform.localScale.x && i >= 0)
        {
            i -= 1;
            nextPos = positions[i];
            dist = Vector3.Distance(point, nextPos);
        }
        Vector3 relativePos = point - nextPos;
        Debug.Log(Vector3.SignedAngle(nextPos - point, Vector3.up, transform.forward));
        return -1.0f * Vector3.SignedAngle(nextPos - point, Vector3.up, transform.forward);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class <c>ZoomController</c> facilitates zooming in and out
///  in both the editor and an Android tablet. 
/// </summary>
public class ZoomController : MonoBehaviour
{
    public GameObject zoomCanvas;
    float maxZoom = 2.0f;
    float minZoom = 0.6f;
    float currZoom;
#if UNITY_EDITOR

    // Start is called before the first frame update
    void Start()
    {
        currZoom = 1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        float zInc = Input.GetAxis("Mouse ScrollWheel");
        currZoom += zInc;
        currZoom = Mathf.Min(currZoom, maxZoom);
        currZoom = Mathf.Max(currZoom, minZoom);
        zoomCanvas.GetComponent<RectTransform>().localScale = new Vector2(currZoom, currZoom);
    }

    public float GetCurrentScale()
    {
        return currZoom;
    }
#elif UNITY_ANDROID
    float startDistance;
    float startScale;

    // Start is called before the first frame update
    void Start()
    {
        currZoom = 1.0f;
        startDistance = -1.0f;
        startScale = -1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount >= 2)
        {
            Vector2 touch0, touch1;
            float distance;
            touch0 = Input.GetTouch(0).position;
            touch1 = Input.GetTouch(1).position;
            distance = Vector2.Distance(touch0, touch1);
            if (startDistance < 0.0)
            {
                startDistance = distance;
                startScale = currZoom;
                return;
            }

            float ratio = distance / startDistance;
            currZoom = startScale * ratio;
            currZoom = Mathf.Min(currZoom, maxZoom);
            currZoom = Mathf.Max(currZoom, minZoom);
            zoomCanvas.GetComponent<RectTransform>().localScale = new Vector2(currZoom, currZoom);
        }
        else
        {
            startDistance = -1.0f;
            startScale = -1.0f;
        }
        
    }

    public float GetCurrentScale()
    {
        return currZoom;
    }
#endif
}

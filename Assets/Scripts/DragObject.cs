using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragObject : MonoBehaviour
{
    public Objects objects;
    //public GameObject drawer;
    private Renderer renderer;
    // private List<ObjectInWorld> oiw;

    // bool canMove;
    bool dragging;
    Vector3 offset;
    Collider2D collider;

    [SerializeField]
    Collider2D h;

    private ModeState currModeState;
    
    void Start()
    {
        collider = GetComponent<Collider2D>();
        dragging = false;
        offset = new Vector3(0,0,0);

        objects = GameObject.Find("Objects").GetComponent<Objects>();
        currModeState = GameObject.Find("ModeState").GetComponent<ModeState>();

        renderer = GameObject.Find("Drawer").GetComponent<Renderer>();
    }


    // Update is called once per frame
    void Update()
    {
        if (currModeState.GetMainMode() == ModeState.MainMode.Object && currModeState.GetObjectMode() == ModeState.ObjectMode.NotAdding)
        {
            Vector3 screenMousePos = Input.mousePosition;
            screenMousePos.z = 6.0f;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(screenMousePos);
            
            if (Input.GetMouseButtonDown(0) || Input.touchCount == 1)
            {
                if (collider.OverlapPoint(mousePos))
                {
                    Collider2D[] results = Physics2D.OverlapPointAll(mousePos);
                    h = GetHighestObject(results);
                    if (h.gameObject.name == this.name){
                        dragging = true;
                        offset = this.transform.position - mousePos;
                    }
                }
            }


            if (dragging)
            {
                this.transform.position = mousePos + offset;
                objects.UpdateOIWLocation(this.name, GetRegionCoords(mousePos + offset));
            }

            // WILL NEED TO ENABLE THE TOUCH INPUT FOR THE TABLET!!!!!!!
            if (Input.GetMouseButtonUp(0))// || Input.touchCount <= 0)
            {
                dragging = false;
                if (IsPointerOverTrash() && collider.OverlapPoint(mousePos))
                {
		    Debug.Log("Should remove " + this.name);
                    objects.RemoveObjectInWorld(this.name);
                }
            }
        }
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    private bool IsPointerOverTrash()
    {
        // determine if text is touched
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        if (results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.name.Contains("Remove"))
                // determine if button is touched
                // if (results[i].gameObject.name.Contains("Remove") || results[i].gameObject.name.Contains("Brush") 
                //                                                   || results[i].gameObject.name.Contains("Add")
                //                                                   || results[i].gameObject.name.Contains("Place")
                //                                                   || results[i].gameObject.name.Contains("Remove")
                //                                                   || results[i].gameObject.name.Contains("Draw") 
                //                                                   || results[i].gameObject.name.Contains("Region") 
                //                                                   || results[i].gameObject.name.Contains("Program Mode") 
                //                                                   || results[i].gameObject.name.Contains("Mode Toggler"))
                {
		    return true;
                }
            }
        }
        
        return false;
    }

    private List<GameObject> GetOverlappingObjects()
    {
        List<GameObject> objects = new List<GameObject>();

        // determine if text is touched
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        if (results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                GameObject item = results[i].gameObject;
                // determine if button is touched
                if (!item.name.Contains("Panel"))
                {
                    objects.Add(item);
                }
            }
        }
        return objects;
    }

    Collider2D GetHighestObject(Collider2D[] results)
    {
        int highestValue = 0;
        Collider2D highestObject = results[0];
        foreach(Collider2D col in results)
        {
            Renderer ren = col.gameObject.GetComponent<Renderer>();
            if(ren && ren.sortingOrder > highestValue)
            {
                highestValue = ren.sortingOrder;
                highestObject = col;
            }
        }
        return highestObject;
    }

    private Vector2 GetRegionCoords(Vector2 position)
    {
        // now I need to go from "world point" to local space
        Vector3 localMousePosVect3 = renderer.transform.InverseTransformPoint(position);
        Vector2 localMousePos = new Vector2(localMousePosVect3.x, localMousePosVect3.z);

        return localMousePos;
    }

    // private void RemoveItem(GameObject item)
    // {
    //     Destroy(item);
    // }

}

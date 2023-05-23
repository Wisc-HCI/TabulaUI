using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Class <c>PlaceObject</c> enables user to add a new object
///  to the scene.
/// </summary>
public class PlaceObject : MonoBehaviour
{

    public GameObject defaultGO;
    public Objects objects;
    public GameObject drawer;

    private Renderer renderer;

    private GameObject objPanel;
    private List<AvailableObject> ao;
    private ModeState currModeState;
    private RegionGrid grid;
    private Menu menu;

    // Start is called before the first frame update
    void Start()
    {
        currModeState = GameObject.Find("ModeState").GetComponent<ModeState>();
        menu = GameObject.Find("UICanvas").GetComponent<Menu>();
        objPanel = GameObject.Find("ObjectPanel");
        grid = GameObject.Find("Regions").GetComponent<RegionGrid>();

        renderer = drawer.GetComponent<Renderer>();   

        ao = objects.GetAvailableObjects();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.touchCount == 1){
            if (currModeState.GetMainMode() == ModeState.MainMode.Object)// && currModeState.GetObjectMode() == ModeState.ObjectMode.Adding)
            {
                Vector3 screenMousePos = Input.mousePosition;
                screenMousePos.z = 6.0f;
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(screenMousePos);

                if (currModeState.GetPlaceMode() == ModeState.PlaceMode.Groceries && !IsPointerOverBlockingObject()){
                    // Instantiate(groceries, mousePos, Quaternion.identity);
                    ObjectInWorld oiw = AddObject("groceries", mousePos, Quaternion.identity);
                    //objects.ManuallyAddObject(oiw);
                    currModeState.SetPlaceMode(ModeState.PlaceMode.None);
                    currModeState.SetObjectMode(ModeState.ObjectMode.NotAdding);
                    menu.AddTogglerOnClick();
                }
                else if (currModeState.GetPlaceMode() == ModeState.PlaceMode.Person && !IsPointerOverBlockingObject()){
                    // Instantiate(person, mousePos, Quaternion.identity);
                    ObjectInWorld oiw = AddObject("person", mousePos, Quaternion.identity);
                    //objects.ManuallyAddObject(oiw);
                    currModeState.SetPlaceMode(ModeState.PlaceMode.None);
                    currModeState.SetObjectMode(ModeState.ObjectMode.NotAdding);
                    menu.AddTogglerOnClick();
                }
                else if (currModeState.GetPlaceMode() == ModeState.PlaceMode.Cabinets && !IsPointerOverBlockingObject()){
                    // Instantiate(person, mousePos, Quaternion.identity);
                    ObjectInWorld oiw = AddObject("cabinets", mousePos, Quaternion.identity);
                    //objects.ManuallyAddObject(oiw);
                    currModeState.SetPlaceMode(ModeState.PlaceMode.None);
                    currModeState.SetObjectMode(ModeState.ObjectMode.NotAdding);
                    menu.AddTogglerOnClick();
                }
                else if (currModeState.GetPlaceMode() == ModeState.PlaceMode.Toy && !IsPointerOverBlockingObject())
                {
                    // Instantiate(person, mousePos, Quaternion.identity);
                    ObjectInWorld oiw = AddObject("toy", mousePos, Quaternion.identity);
                    //objects.ManuallyAddObject(oiw);
                    currModeState.SetPlaceMode(ModeState.PlaceMode.None);
                    currModeState.SetObjectMode(ModeState.ObjectMode.NotAdding);
                    menu.AddTogglerOnClick();
                }
                else if (currModeState.GetPlaceMode() == ModeState.PlaceMode.Chest && !IsPointerOverBlockingObject())
                {
                    // Instantiate(person, mousePos, Quaternion.identity);
                    ObjectInWorld oiw = AddObject("chest", mousePos, Quaternion.identity);
                    //objects.ManuallyAddObject(oiw);
                    currModeState.SetPlaceMode(ModeState.PlaceMode.None);
                    currModeState.SetObjectMode(ModeState.ObjectMode.NotAdding);
                    menu.AddTogglerOnClick();
                }
            }
        }
    }

    private bool IsPointerOverBlockingObject()
    {
        // determine if text is touched
        Vector2 pos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = pos;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                // determine if button is touched
                if (results[i].gameObject.name.Contains("Brush") || results[i].gameObject.name.Contains("Add")
                                                                    || results[i].gameObject.name.Contains("Place")
                                                                    || results[i].gameObject.name.Contains("Remove")
                                                                    || results[i].gameObject.name.Contains("Draw")
                                                                    || results[i].gameObject.name.Contains("Placement")
                                                                    // || results[i].gameObject.name.Contains("Region")
                                                                    || results[i].gameObject.name.Contains("Program Mode")
                                                                    || results[i].gameObject.name.Contains("Mode Toggler"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public ObjectInWorld AddObject(string itemName, Vector2 location, Quaternion rotation)
    {
        AvailableObject item = ao[0];
        foreach (AvailableObject obj in ao)
        {
            if (obj.name == itemName){
                item = obj;
            }
        }

        defaultGO.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(item.sprite);
        GameObject newItem = Instantiate(defaultGO, location, rotation);
        //newItem.name =  item.name;
        newItem.transform.parent = objPanel.transform;

        ObjectInWorld oiw = new ObjectInWorld(item, GetRegionCoords(location), newItem);
        objects.AddObjectInWorld(oiw);
        newItem.name = oiw.id.ToString(); //needs to happen after AddObjectInWorld due to way ID is assigned

        if (!objects.IsContainer(newItem.name))
        {
            newItem.GetComponent<SpriteRenderer>().sortingOrder = 1;
        }
        return oiw;
    }

    private Vector2 GetRegionCoords(Vector2 position)
    {
        // now I need to go from "world point" to local space
        Vector3 localMousePosVect3 = renderer.transform.InverseTransformPoint(position);
        Vector2 localMousePos = new Vector2(localMousePosVect3.x, localMousePosVect3.z);

        return localMousePos;
    }

}

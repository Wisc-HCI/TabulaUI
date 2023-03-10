using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// Class <c>Objects</c> Stores the objects in the world.
/// </summary>
public class Objects : MonoBehaviour
{
    [SerializeField]
    private List<AvailableObject> ao;
    [SerializeField]
    private List<ObjectInWorld> oiw;

    // keep track of manually-added objects
    private List<ObjectInWorld> manuallyAddedObjects;

    [SerializeField]
    private int id_count;

    // // using this dictionary to track the
    // private Dictionary<String, GameObject> nameToGO;

    // Start is called before the first frame update
    void Start()
    {
        //string path = "Assets/Resources/available_entities.json";
        //StreamReader reader = new StreamReader(path);
        //string jsonString = reader.ReadToEnd();
        TextAsset aeText = Resources.Load<TextAsset>("available_entities");
        string jsonString = aeText.text;
        Debug.Log(jsonString);
        //reader.Close();
        AvailableObjects aos = JsonUtility.FromJson<AvailableObjects>(jsonString);
        ao = aos.aos;
        oiw = new List<ObjectInWorld>();

        id_count = 100;

        manuallyAddedObjects = new List<ObjectInWorld>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<AvailableObject> GetAvailableObjects()
    {
        return ao;
    }


    public List<ObjectInWorld> GetObjectsInWorld()
    {
        return oiw;
    }

    public int GetIdCount()
    {
        return id_count;
    }

    public void ManuallyAddObject(ObjectInWorld item)
    {
        AddObjectInWorld(item);
    }

    public void AddObjectInWorld(ObjectInWorld item)
    {
        item.id = id_count;
        id_count = id_count + 1;
        oiw.Add(item);
    }

    public void RemoveObjectInWorld(string name, bool destroyGO = true)
    {
        foreach (ObjectInWorld obj in oiw)
        {
            if (obj.id.ToString() == name){
                if(destroyGO)
                {
                    Destroy(obj.gameObject);
                }
                oiw.Remove(obj);
                break;
            }
        }
    }

    public void UpdateOIWLocation(string name, Vector2 loc)
    {
        foreach (ObjectInWorld obj in oiw)
        {
            if (obj.id.ToString() == name){
                obj.coordinates = loc;
                break;
            }
        }
    }


    public bool IsContainer(string name)
    {
        bool flag = false;
        foreach (ObjectInWorld obj in oiw)
        {
            if (obj.id.ToString() == name){
                for (int i = 0; i < obj.categories.Length; i++)
                {
                    if (obj.categories[i] == "container")
                    {
                        flag = true;
                    }
                }
            }
        }
        return flag;
    }

    public ObjectInWorld GetOIWByID(string name)
    {
        foreach (ObjectInWorld obj in oiw)
        {
            if (obj.id.ToString() == name){
                return obj;
            }
        }
        return null;
    }

    public List<ObjectInWorld> GetManuallyAddedObjects()
    {
        return manuallyAddedObjects;
    }

}

[System.Serializable]
public class ObjectInWorld : AvailableObject
{
    public Vector2 coordinates;
    public List<ObjectInWorld> children;
    public GameObject gameObject;
    public bool selected;
    public int id;

    public ObjectInWorld(AvailableObject baseObj, Vector2 coords, GameObject go)
    {
        name = baseObj.name;
        categories = baseObj.categories;
        entity_class = baseObj.entity_class;
        sprite = baseObj.sprite;
        children = new List<ObjectInWorld>();

        coordinates = coords;
        gameObject = go;
    }
}

[System.Serializable]
public class AvailableObjects
{
    public List<AvailableObject> aos;
}

[System.Serializable]
public class AvailableObject
{
    public string name;
    public string[] categories;
    public string entity_class;
    public string sprite;
}

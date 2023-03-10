using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Class <c>StateStorage</c> stores the current state of the program. 
/// </summary>
public class StateStorage : MonoBehaviour
{
    // ROS connection
    private RosNode ros;

    List<GameObject> trace;
    private Renderer renderer;
    private Vector2 materialDim;
    private RegionGrid grid;

    // access to the world state (the objects and regions)
    private Objects objs;
    private RegionGrid regions;
    public SerializableUpdate serUpdate;
    private int recordingCounter;
    private int actIDCounter;

    // Start is called before the first frame update
    void Awake()
    {
        ros = GameObject.FindObjectOfType<RosNode>();

        grid = GameObject.Find("Regions").GetComponent<RegionGrid>();
        renderer = GameObject.Find("Drawer").GetComponent<Renderer>();
        materialDim = new Vector2(renderer.bounds.min.x * -1, renderer.bounds.min.y * -1);
        trace = new List<GameObject>();

        // NEW STUFF BELOW
        // access to world state
        objs = GameObject.Find("Objects").GetComponent<Objects>();
        regions = GameObject.Find("Regions").GetComponent<RegionGrid>();

        // access to the actual storage
        serUpdate = new SerializableUpdate();
        recordingCounter = 0;
        actIDCounter = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int GetRecordingCounter()
    {
        return recordingCounter;
    }

    public void AddRegion(GameObject wp)
    {
        trace.Add(wp);
    }

    public List<GameObject> GetTrace()
    {
        return trace;
    }

    public GameObject GetEndOfTrace()
    {
        if (trace.Count == 0)
        {
            return null;
        }
        return trace[trace.Count-1];
    }

    public WpLabelIDPair[] GetStringTrace()
    {
        WpLabelIDPair[] stringTrace = new WpLabelIDPair[trace.Count];
        for (int i = 0; i < trace.Count; i++)
        {
            Vector2 cell = grid.PTToGridCell(new Vector2(trace[i].transform.position.x, trace[i].transform.position.y), materialDim);
            Region reg = grid.GetCellContents(new Vector2(cell.y, cell.x));
            WpLabelIDPair wpData = new WpLabelIDPair();
            wpData.label = reg.GetName();
            wpData.id = GameObject.Find("LineDrawer").GetComponent<LineDrawer>().GetWpID(trace[i]);
            stringTrace[i] = wpData;
        }
        return stringTrace;
    }

    public string[] GetStringTraceSourceRemoved()
    {
        string[] stringTrace;
        if (trace.Count == 0)
        {
            stringTrace = new string[0];
        }
        else
        {
            stringTrace = new string[trace.Count - 1];
        }

        for (int i = 1; i < trace.Count; i++)
        {
            Debug.Log(trace);
            Debug.Log(trace[i].transform.position);
            Vector2 cell = grid.PTToGridCell(new Vector2(trace[i].transform.position.x, trace[i].transform.position.y), materialDim);
            Debug.Log(cell);
            Region reg = grid.GetCellContents(new Vector2(cell.y, cell.x));
            stringTrace[i-1] = reg.GetName();
        }
        return stringTrace;
    }

    public int GetRecordingCount()
    {
        return serUpdate.program.recordings.Length;
    }

    public int GetTraceCount()
    {
        return trace.Count;
    }

    public void ClearStorage()
    {
        trace = new List<GameObject>();
    }

    public void AddWorld()
    {
        serUpdate.AddWorld(regions.GetRegions(), objs.GetObjectsInWorld(), objs.GetManuallyAddedObjects());
    }

    public void AddRecording(string text, WpLabelIDPair[] trace)
    {
        Debug.Log("ADDING A RECORDING");
        SerializableRecording newRec = serUpdate.AddRecording(text, trace);
        recordingCounter += 1;
        // go thru entire recording, assign act ID's to new actions
        //    in the trace
        SerializableRecording[] recs = serUpdate.program.recordings;
        foreach (SerializableRecording rec in recs)
        {
            SerializableSketch sk = rec.sketch;
            SerializableUserSeq us = sk.user_sequence;
            SerializablePlan pl = us.plan;
            SerializableWaypoint[] wps = pl.waypoints;
            if (wps != null)
            {
                foreach (SerializableWaypoint wp in wps)
                {

                }
            }
        }
        SerializableSketch newSK = newRec.sketch;
        SerializableUserSeq newUS = newSK.user_sequence;
        SerializableAction[] newSA = newUS.data;
        foreach (SerializableAction act in newSA)
        {
            //act._id = actIDCounter;
            actIDCounter++;
        }
    }

    public void DeleteRecording(string deleteId)
    {
        // Use this list to track all IDs we will delete.
        List<int> iDsToDelete = new List<int>();
        iDsToDelete.Add(Convert.ToInt32(deleteId));
        //Debug.Log("Okay, delete " + deleteId);

        // First convert SerializableRecording into a list we can use (maybe not most efficient but needed for my coding skills)
        List<SerializableRecording> temp = new List<SerializableRecording>();
        List<int> wp = new List<int>();
        for (int i = 0; i < serUpdate.program.recordings.Length; i++)
        {
            temp.Add(serUpdate.program.recordings[i]);
        }

        // Second identify any attached traces that also need deleted
        List<int> attachedTraceIDsToDelete = GetAttachedTraces(deleteId, temp);
        if (attachedTraceIDsToDelete.Count > 0)
        {
            //Debug.Log("We had attached traces!");
            //Debug.Log("Attached recordings to delete also:");
            foreach (int item in attachedTraceIDsToDelete)
            {
                Debug.Log(item);
                iDsToDelete.Add(item);
            }
        }

        // Third, remove all traces that will be deleted from temp
        temp.RemoveAll(recId => iDsToDelete.Contains(recId._id));

        // Fourth, convert temp to a SerializableRecording so that we can update the serUpdate
        SerializableRecording[] newRecordings = new SerializableRecording[temp.Count];
        for (int i = 0; i < temp.Count; i++)
        {
            newRecordings[i] = temp[i];
        }

        // Fifth, propogate those changes to serUpdate/world state
        serUpdate.program.recordings = newRecordings;
        ros.ProcessRecordings(serUpdate.program);

        // Sixth, remove game objects from the UI
        List<string> ignoreList = new List<string> { "ObjectPanel", "BackgroundMap", "Drawer", "InputTextCanvas" };
        GameObject cp = GameObject.Find("ContentPanel");

        for (int i = 0; i < cp.transform.childCount; i++)
        {
            string childName = cp.transform.GetChild(i).gameObject.name;
            if (!ignoreList.Contains(childName))
            {
                string num = childName.Split('_')[0];
                if (iDsToDelete.Contains(Convert.ToInt32(num)))
                {
                    Destroy(cp.transform.GetChild(i).gameObject);
                }
            }
        }

        // Finally, send ROS update to trigger backend update
        ros.SendDeletionRequest();
    }

    public List<int> GetAttachedTraces(string deleteId, List<SerializableRecording> currRecs)
    {

        // Here we do two things:
        //  (1) make a temporary list of Recordings (in List<SerializableRecording> temp) that does not include the one
        //  we are currently deleting (because we don't need to check if it is attached to anything)
        //  (2) for the recording that we will delete, we need to collect the list of waypoint ids (in List<int> wp)
        //  so that we can check if any recordings placed in temp are attached to any waypoints in the recording we will delete
        //  if any are attached, we will need to delete them too
        List<SerializableRecording> temp = new List<SerializableRecording>();
        List<int> wp = new List<int>();
        for (int i = 0; i < currRecs.Count; i++)
        {
            // if it's not the recording we will delete, add to temp
            if (currRecs[i]._id.ToString() != deleteId)
            {
                temp.Add(currRecs[i]);
            }
            //otherwise, add the waypoints for the recording we will delete in wp
            else
            {
                for (int j = 1; j < currRecs[i].sketch.user_sequence.data.Length; j++)
                {
                    string step = currRecs[i].sketch.user_sequence.data[j].cmd;
                    if (step == "moveTo")
                    {
                        for (int k = 0; k < currRecs[i].sketch.user_sequence.data[j].args.Length; k++)
                        {
                            if (currRecs[i].sketch.user_sequence.data[j].args[k].argname == "destination")
                            {
                                wp.Add(currRecs[i].sketch.user_sequence.data[j]._id);
                                Debug.Log(currRecs[i].sketch.user_sequence.data[j]._id);
                            }

                        }
                    }
                }
            }
        }

        // Now, for each recordings in temp, check if its first waypoint is attached, meaning that the ID of the first waypoint
        // is the same as one of the waypoints in the recording we will delete
        List<int> attachedIds = new List<int>();
        foreach (SerializableRecording rec in temp)
        {
            if (rec.is_main_or_branch == "branch" && wp.Contains(Convert.ToInt32(rec.branch_from_id))) //if (CheckIfAttached(rec, wp))
            {
                attachedIds.Add(rec._id);

                //if we find an attached recording, we need to also check if that reocrding has attached ones that also need deleted.
                List<int> newAttachedIds = GetAttachedTraces(rec._id.ToString(), temp);
                foreach (int alsoAttachedId in newAttachedIds)
                {
                    attachedIds.Add(alsoAttachedId);
                }
            }
        }

        // return the list of all recording IDs that are somehow attached to the recording in question
        return attachedIds;
    }

    public void UpdateFromROS(SerializableUpdate newUpdate)
    {
        Debug.Log("Updating SerializableUpdate from ROS msg");
        serUpdate = newUpdate;
    }

    public string PrepareUpdate()
    {
        string update = serUpdate.SaveToString();
        return update;
    }
}

[System.Serializable]
public class SerializableUpdate
{
    public SerializableWorld world;
    public SerializableProgram program;

    public SerializableUpdate()
    {
        world = new SerializableWorld();
        program = new SerializableProgram();
    }

    public SerializableRecording AddRecording(string text, WpLabelIDPair[] trace)
    {
        return program.AddRecording(text, trace);
    }

    public void AddWorld(List<Region> regionList, List<ObjectInWorld> objects, List<ObjectInWorld> manuallyAddedObjects) {
        world.SetupWorld(regionList, objects, manuallyAddedObjects);
    }

    public string SaveToString()
    {
        return JsonUtility.ToJson(this);
    }
}

[System.Serializable]
public class SerializableProgram
{

    public SerializableRecording[] recordings;

    public SerializableProgram()
    {
        recordings = new SerializableRecording[0];
    }

    public SerializableRecording AddRecording(string text, WpLabelIDPair[] trace)
    {
        SerializableRecording serRec = new SerializableRecording(recordings.Length);
        serRec.AddText(text);
        serRec.AddTrace(trace);
        SerializableRecording[] temp = new SerializableRecording[recordings.Length + 1];
        for (int i = 0; i < recordings.Length; i++)
        {
            Debug.Log(recordings[i]);
            temp[i] = recordings[i];
        }
        temp[temp.Length-1] = serRec;
        recordings = temp;
        return serRec;
    }
}

[System.Serializable]
public class SerializableRecording
{
    public int _id;
    public SerializableText text;
    public string is_main_or_branch;
    public string branch_from;
    public string branch_from_id;
    public SerializableAction branch_condition;
    public SerializableSketch sketch;

    public SerializableRecording(int _id)
    {   
        this._id = _id;
    }

    public void AddText(string text)
    {
        this.text = new SerializableText(text);
    }

    public void AddTrace(WpLabelIDPair[] trace)
    {
        sketch = new SerializableSketch(trace);
    }
}

[System.Serializable]
public class SerializableText
{
    public string content;
    public SerializableTextInterval[] label_intervals;

    public SerializableText(string text)
    {
        content = text;
    }
}

[System.Serializable]
public class SerializableTextInterval
{
    public int start;
    public int end;
    public string label;
}


[System.Serializable]
public class SerializableSketch
{
    public SerializableUserSeq user_sequence;

    public SerializableSketch(WpLabelIDPair[] trace)
    {
        user_sequence = new SerializableUserSeq(trace);
    }
}

[System.Serializable]
public class SerializableUserSeq
{
    public SerializableAction[] data;
    public SerializablePlan plan;

    public SerializableUserSeq(WpLabelIDPair[] trace)
    {
        plan = new SerializablePlan();
        data = new SerializableAction[trace.Length];
        for (int i = 0; i < trace.Length; i++)
        {
            SerializableAction act = new SerializableAction();
            act.AddMoveToAction(trace[i]);
            data[i] = act;
        }
    }
}

[System.Serializable]
public class SerializableAction
{
    public int _id;
    public string cmd;
    public string _type; 
    public SerializableArg[] args;

    public SerializableAction()
    {
        _id = -1;
    }

    public void AddMoveToAction(WpLabelIDPair destination)
    {
        cmd = "moveTo";
        _type = "command";
        _id = destination.id;
        args = new SerializableArg[1];
        args[0] = new SerializableArg("destination", destination.label);
    }
}

[System.Serializable]
public class SerializableArg
{
    public string argname;
    public string argval;

    public SerializableArg(string argname, string argval)
    {
        this.argname = argname;
        this.argval = argval;
    }
}

[System.Serializable]
public class SerializablePlan
{
    public SerializableWaypoint[] waypoints;
}

[System.Serializable]
public class SerializableWaypoint
{
    public int _id;
    public int x;
    public int y;
    public SerializableAction[] task_subsequence;
}

[System.Serializable]
public class SerializableWorld
{
    public SerializableObject[] regions;
    public string[] manuallyAddedObjects;

    public SerializableWorld()
    {

    }

    public void SetupWorld(List<Region> regionList, List<ObjectInWorld> objects, List<ObjectInWorld> manuallyAddedObjects)
    {
        RegionGrid rg = GameObject.Find("Regions").GetComponent<RegionGrid>();
        regions = new SerializableObject[regionList.Count];
        for (int r = 0; r < regionList.Count; r++)
        {
            Region reg = regionList[r];
            SerializableObject so = new SerializableObject();
            so.name = reg.GetName();
            List<SerializableObject> objs = new List<SerializableObject>();

            // the loop below relies on the idea that each object can only exist in one region at a time.
            foreach (ObjectInWorld obj in objects)
            {
                Vector2 cell = rg.PTToGridCell(obj.coordinates, new Vector2());

                //Debug.Log(obj.name + " is in " + rg.GetCellContents(cell).GetName());

                if (rg.GetCellContents(cell) == reg)
                {
                    objs.Add(SerializeNestedObjects(obj));
                }
            }
            so.objects = new SerializableObject[objs.Count];
            for (int i = 0; i < objs.Count; i++)
            {
                so.objects[i] = objs[i];
            }
            regions[r] = so;
        }

        // add the manually-added objects
        this.manuallyAddedObjects = new string[manuallyAddedObjects.Count];
        for (int i = 0; i < manuallyAddedObjects.Count; i++)
        {
            this.manuallyAddedObjects[i] = manuallyAddedObjects[i].name; 
        }

    }

    public SerializableObject SerializeNestedObjects(ObjectInWorld oiw)
    {
        Debug.Log(oiw.name);

        SerializableObject so = new SerializableObject();
        so.name = oiw.name;
        so.objects = new SerializableObject[oiw.children.Count];

        for (int i = 0; i < so.objects.Length; i++)
        {
            so.objects[i] = SerializeNestedObjects(oiw.children[i]);
        }
        return so;
    }

    public string SaveToString()
    {
        return JsonUtility.ToJson(this);
    }
}

public class WpLabelIDPair
{
    public string label;
    public int id;
}
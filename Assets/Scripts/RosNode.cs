using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;
using RosMessageTypes.Tabula;
using System;
using System.IO;
using System.Linq;

/// <summary>
/// Class <c>RosNode</c> communicate with the front back end synthesizer.
/// </summary>
public class RosNode : MonoBehaviour
{

	ROSConnection ros;
	// request and receive a new map
	public string mapRequestPubName = "/ctrl/map_request";
	public string mapReceivedSubName = "/ctrl/map";

	// receive requests for and send entities
	public string entityRequestSubName = "synthesizer/available_entity_request";
	public string sendEntityPubName = "synthesizer/available_entity_receive";

	// send the recording
	public string recordingPubName = "synthesizer/recording";
	// receive update from synthesizer
	public string updateReceivedSubName = "synthesizer/program_update";

	public float publishMessageFrequency = 0.5f;

	// msg id counter
	private int id_counter;

	// whether or not to use internal stt or an external stt ROS node
	private bool outsource_stt;
	private string outsourceSttNodePingTopic = "stt/node_up";
	private string outsourceSttNodeAckTopic = "UI/node_up_ack";
	private string notifySttBeginRecordTopic = "UI/record";
	private string notifySttEndRecordTopic = "UI/end_record";
	private string receiveSttResultTopic = "stt/string";
	private WpLabelIDPair[] trace_storage;

	// serializable
	private StateStorage stateStorage;

	// message types
	const int NO_CHANGE = 0;
	const int NEW_RECORDING = 1;
	const int DELETE_RECORDING = 2;

	public SerializableUpdate su;
	public ReviewRecordings review;
	private ModeState currModeState;
	private RegionGrid regionGrid;
	private Objects objects;
	private PlaceObject placeObj;

	private BackgroundMapDrawer backgroundMapDrawer;

	// Start is called before the first frame update
	void Start()
	{
		// start the ROS connection
		ros = ROSConnection.GetOrCreateInstance();
		

		// map
		ros.RegisterPublisher<EmptyMsg>(mapRequestPubName);
		ros.Subscribe<OccupancyGridMsg>(
			mapReceivedSubName,
			OnMapReceived
		);

		// entities
		ros.RegisterPublisher<AvailableEntitiesMsg>(sendEntityPubName);
		ros.Subscribe<EmptyMsg>(
			entityRequestSubName,
			OnAvailableEntitiesRequested
		);

		// recording
		ros.RegisterPublisher<UpdateMsg>(recordingPubName);
		ros.Subscribe<UpdateMsg>(
			updateReceivedSubName,
			OnUpdateReceived
		);

		// outsourcing stt node
		ros.RegisterPublisher<EmptyMsg>(outsourceSttNodeAckTopic);
		ros.RegisterPublisher<EmptyMsg>(notifySttBeginRecordTopic);
		ros.RegisterPublisher<EmptyMsg>(notifySttEndRecordTopic);
		ros.Subscribe<EmptyMsg>( 
			outsourceSttNodePingTopic,
			OnSttNodePingReceived
		);
		ros.Subscribe<StringMsg>( 
			receiveSttResultTopic,
			OnSttResultReceived
		);

		// start id counter
		id_counter = 0;

		// default for outsourcing stt is false
		outsource_stt = false;
		trace_storage = null;

		// access to state storage
		stateStorage = GameObject.Find("StateStorage").GetComponent<StateStorage>();

		// access review
		review = GameObject.Find("Review").GetComponent<ReviewRecordings>();

		// access current mode
		currModeState = GameObject.Find("ModeState").GetComponent<ModeState>();

		// access regions, objects
		regionGrid = GameObject.Find("Regions").GetComponent<RegionGrid>();
		objects = GameObject.Find("Objects").GetComponent<Objects>();

		// access PlaceObject function
		placeObj = GameObject.Find("ObjectPanel").GetComponent<PlaceObject>();

		// accessing and updating the map
		backgroundMapDrawer = GameObject.Find("BackgroundMap").GetComponent<BackgroundMapDrawer>();

        // debug testing for displaying recordings
        //string jsontest = "{\"world\": {\"regions\": [{\"name\": \"garage\", \"objects\": []}, {\"name\": \"living room\", \"objects\": []}, {\"name\": \"kitchen\", \"objects\": []}, {\"name\": \"hallway\", \"objects\": []}, {\"name\": \"office\", \"objects\": []}, {\"name\": \"bathroom\", \"objects\": []}, {\"name\": \"bedroom\", \"objects\": []}]}, \"program\": {\"recordings\": [{\"_id\": 0, \"text\": {\"content\": \"\", \"label_intervals\": []}, \"is_main_or_branch\": \"main\", \"branch_from\": \"living room\", \"branch_from_id\": 0, \"branch_condition\": null, \"sketch\": {\"user_sequence\": {\"data\": [{\"_id\": 0, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"living room\"}]}, {\"_id\": 1, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"office\"}]}, {\"_id\": 2, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"bedroom\"}]}, {\"_id\": 3, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"kitchen\"}]}, {\"_id\": 1, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"office\"}]}, {\"_id\": 1, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"office\"}]}], \"plan\": {\"waypoints\": [{\"_id\": 1, \"x\": 1, \"y\": 1, \"task_subsequence\": [{\"_id\": 1, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"office\"}]}]}, {\"_id\": 2, \"x\": 1, \"y\": 1, \"task_subsequence\": [{\"_id\": 2, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"office\"}]}]}]}}}}, {\"_id\": 1, \"text\": {\"content\": \"\", \"label_intervals\": []}, \"is_main_or_branch\": \"branch\", \"branch_from\": \"kitchen\", \"branch_from_id\": 3, \"branch_condition\": null, \"sketch\": {\"user_sequence\": {\"data\": [{\"_id\": 3, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"kitchen\"}]}, {\"_id\": 4, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"garage\"}]}], \"plan\": {\"waypoints\": [{\"_id\": 4, \"x\": 1, \"y\": 1, \"task_subsequence\": [{\"_id\": 4, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"garage\"}]}]}]}}}}, {\"_id\": 2, \"text\": {\"content\": \"\", \"label_intervals\": []}, \"is_main_or_branch\": \"branch\", \"branch_from\": \"bedroom\", \"branch_from_id\": 2, \"branch_condition\": null, \"sketch\": {\"user_sequence\": {\"data\": [{\"_id\": 2, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"bedroom\"}]}, {\"_id\": 2, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"bedroom\"}]}], \"plan\": {\"waypoints\": [{\"_id\": 2, \"x\": 1, \"y\": 1, \"task_subsequence\": [{\"_id\": 2, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"bedroom\"}]}]}]}}}}, {\"_id\": 3, \"text\": {\"content\": \"\", \"label_intervals\": []}, \"is_main_or_branch\": \"branch\", \"branch_from\": \"office\", \"branch_from_id\": 1, \"branch_condition\": null, \"sketch\": {\"user_sequence\": {\"data\": [{\"_id\": 1, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"office\"}]}, {\"_id\": 5, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"bathroom\"}]}], \"plan\": {\"waypoints\": [{\"_id\": 5, \"x\": 1, \"y\": 1, \"task_subsequence\": [{\"_id\": 5, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"bathroom\"}]}]}]}}}}, {\"_id\": 4, \"text\": {\"content\": \"\", \"label_intervals\": []}, \"is_main_or_branch\": \"branch\", \"branch_from\": \"bathroom\", \"branch_from_id\": 5, \"branch_condition\": null, \"sketch\": {\"user_sequence\": {\"data\": [{\"_id\": 5, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"bathroom\"}]}, {\"_id\": 6, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"bedroom\"}]}], \"plan\": {\"waypoints\": [{\"_id\": 6, \"x\": 1, \"y\": 1, \"task_subsequence\": [{\"_id\": 6, \"cmd\": \"moveTo\", \"_type\": \"command\", \"args\": [{\"argname\": \"destination\", \"argval\": \"bedroom\"}]}]}]}}}}]}}";
        //su = JsonUtility.FromJson<SerializableUpdate>(jsontest);
        //stateStorage.UpdateFromROS(su);
        //ProcessRecordings(su.program);
        // ProcessWorld(su.world);
    }

	// Update is called once per frame
	void Update()
	{
	}


	public void SetRosIp(string ip)
    {
		Debug.Log("Updating ROS IP to " + ip);
		ros.Connect(ip, 10000);
	}

	//public void SendRecording(string text, string[] trace)

	public void SendRecording(string text, WpLabelIDPair[] trace)
    {
		Debug.Log("CONTENTS OF RECORDING to send:");
		Debug.Log(text);
		// setup trace
		for (int i = 0; i < trace.Length; i++)
        {
			Debug.Log(trace[i].label);
        }

        // setup update json
        stateStorage.AddRecording(text, trace);
        stateStorage.AddWorld();
        string jsonString = stateStorage.PrepareUpdate();
        Debug.Log(jsonString);
        UpdateMsg msg = new UpdateMsg(
        	id_counter,
        	NEW_RECORDING,
        	jsonString
        );
        ros.Publish(recordingPubName, msg);
        id_counter++;

		//// setup world
		//SerializableWorld sw = new SerializableWorld(regions.GetRegions(), objs.GetObjectsInWorld());
		//string worldString = sw.SaveToString();
		//Debug.Log(worldString);
		//RecordingMsg msg = new RecordingMsg(
		//	text,
		//	trace,
		//	worldString
		//);
		//ros.Publish(recordingPubName, msg);
    }

	public void SendDeletionRequest()
    {
		Debug.Log("Sending delete request to ROS.");
		string jsonString = stateStorage.PrepareUpdate();
		Debug.Log(jsonString);
		UpdateMsg msg = new UpdateMsg(
			id_counter,
			DELETE_RECORDING,
			jsonString
		);
		ros.Publish(recordingPubName, msg);
		id_counter++;
	}

	public void SendMapRequest()
    {
		Debug.Log("Sending map request.");
		EmptyMsg msg = new EmptyMsg();
		ros.Publish(mapRequestPubName, msg);
    }

	public void OnSttNodePingReceived(EmptyMsg msg)
	{
		Debug.Log("Outsourcing STT node.");
		outsource_stt = true;
		ros.Publish(outsourceSttNodeAckTopic, new EmptyMsg());
	}

	public void BeginRecord()
	{
		if (outsource_stt)
		{
			ros.Publish(notifySttBeginRecordTopic, new EmptyMsg());
		}
	}

	public void EndRecord(string text, WpLabelIDPair[] trace)
	{
		if (outsource_stt)
		{
			trace_storage = trace;
			ros.Publish(notifySttEndRecordTopic, new EmptyMsg());
			return;
		}
		SendRecording(text, trace);
	}

	private void OnSttResultReceived(StringMsg msg)
	{
		Debug.Log("Received the following speech: " + msg.data);

		SendRecording(msg.data, trace_storage);
	}

	void OnMapReceived(OccupancyGridMsg msg)
    {
		Debug.Log("Map received.");
		MapLayout ml = new MapLayout();
		ml.width = (int) msg.info.width;
		ml.height = (int) msg.info.height;
		ml.resolution = msg.info.resolution;
		ml.originPoint = new double[] { msg.info.origin.position.x, msg.info.origin.position.y };
		ml.originQuat = new double[] { msg.info.origin.orientation.x, msg.info.origin.orientation.y, msg.info.origin.orientation.z, msg.info.origin.orientation.w };
		ml.data = new int[msg.data.Length];
		for (int i = 0; i < ml.data.Length; i++ )
        {
			ml.data[i] = msg.data[i];
        }
		string jsonString = ml.SaveToString();
		//Debug.Log(jsonString);

		string path = "Assets/Resources/rosmap.json";
		StreamWriter writer = new StreamWriter(path, false);
		writer.Write(jsonString);
		writer.Close();

		backgroundMapDrawer.LoadMap(ml);
	} 

	void OnUpdateReceived(UpdateMsg msg)
    {
		Debug.Log("Update received.");
		Debug.Log(msg.update);
		su = JsonUtility.FromJson<SerializableUpdate>(msg.update);
		stateStorage.UpdateFromROS(su);

		// first, go through world and place objects that are added
		Debug.Log("Processing world.");
		ProcessWorld(su.world);

		// then, add recordings to the review
		Debug.Log("Processing Recordings.");
		ProcessRecordings(su.program);
	}

	public void ProcessWorld(SerializableWorld world)
    {
        for (int i = 0; i < world.regions.Length; i++)
        {
			Vector2 regPos = new Vector2(0f, 0f);
			string regName = "";
			foreach (Region reg in regionGrid.regions)
			{
				if (reg.GetName() == world.regions[i].name)
                {
					regName = reg.GetName();
					regPos = regionGrid.GridCellToPT(reg.GetCenter());
                }
			}
			for (int j = 0; j < world.regions[i].objects.Length; j++)
            {
				ProcessNestedObjects(world.regions[i], regName, regPos);
            }
        }

	}

	void ProcessNestedObjects(SerializableObject so, string regName, Vector2 regLoc)
    {
		for (int i = 0; i < so.objects.Length; i++)
		{
			bool needToAddObj = true;

			foreach (ObjectInWorld obj in objects.GetObjectsInWorld())
			{
				if (obj.name == so.objects[i].name)
				{
					Vector2 cell = regionGrid.PTToGridCell(obj.coordinates, new Vector2());
					if (regionGrid.GetCellContents(cell).GetName() == regName)
					{
						needToAddObj = false;
					}
				}
			}

			if(needToAddObj)
            {
				placeObj.AddObject(so.objects[i].name, regLoc, Quaternion.identity);
			}
			ProcessNestedObjects(so.objects[i], regName, regLoc);
		}
	}

	public void ProcessRecordings(SerializableProgram program)
	{
		// first remove old ones
		foreach (Recording rec in review.recordings)
		{
			if (rec.go)
			{
				Destroy(rec.go.gameObject);
			}
		}
		review.recordings.Clear();

		// then add the new ones
		for (int i = 0; i < program.recordings.Length; i++)
		{
			Debug.Log("RECORDING  " + i.ToString());
			List<string> steps = new List<string>();
			int stepNum = 1;
			string indent = "";
			
			// check if there's a trigger, we print it as a special case
			if (!(program.recordings[i].branch_condition.cmd is null))
			{
				steps.Add("Trigger: " + program.recordings[i].branch_condition.cmd + " " + program.recordings[i].branch_condition.args[0].argval);
			}

			// check if it's a branch, we handle it as a special case
			if (program.recordings[i].is_main_or_branch == "branch")
			{
				steps.Add("(Starting from the " + program.recordings[i].branch_from + ")");
			}


			// collect list of waypoint IDs from the sketch.user_sequence.data --> we need to use these to check for loops
			List<int> wpIdList = new List<int>();
			//Debug.Log("---Building wpIdList---");
			for (int k = 1; k < program.recordings[i].sketch.user_sequence.data.Length; k++)
			{
				SerializableAction action = program.recordings[i].sketch.user_sequence.data[k];

				if (action.cmd == "moveTo")
				{
					wpIdList.Add(action._id);
					//Debug.Log("Adding " + action._id + " to wpIdList");
				}
			}
			//Debug.Log("---Done---");

			// for branches, we need to specially handle loops on the branch_from waypoint
			if (program.recordings[i].is_main_or_branch == "branch")
			{
				List<int> branchWpIdList = new List<int>(wpIdList);
				branchWpIdList.Add(Convert.ToInt32(program.recordings[i].branch_from_id));
				if (HasLoop(Convert.ToInt32(program.recordings[i].branch_from_id), branchWpIdList))
				{
					steps.Add("LOOP:");
					indent = "  ";
				}
			}

			for (int k = 0; k < program.recordings[i].sketch.user_sequence.plan.waypoints.Length; k++)
			{
				// first check if it's a loop
				// to check for a loop, we can see if the waypoint ID occurs multiple times within the wpIdList we created earlier
				// if it occurs more than once, it means the user revisited that waypoint and therefore creates a loop
				//bool hasLoop = (wpIdList.Where(id => id == program.recordings[i].sketch.user_sequence.plan.waypoints[k]._id).Count() > 1);

				// if there's a loop, mark the loop and add the indent to the steps
				if (HasLoop(program.recordings[i].sketch.user_sequence.plan.waypoints[k]._id,wpIdList))
                {
					steps.Add("LOOP:");
					indent = "  ";
                }

				List<string> stepList = GetWaypointStepsString(program.recordings[i].sketch.user_sequence.plan.waypoints[k], stepNum);
				foreach (string step in stepList)
                {
					steps.Add(indent + step);
					stepNum++;
				}
			}

			string txt = "";
			if (program.recordings[i].text.content == "")
			{
				txt = "(no speech)";
			}
			else
			{
				txt = "\"" + program.recordings[i].text.content + "\"";

			}
			Color32 color = GetRecordingLineColor(program.recordings[i]._id.ToString());
			Recording rec = new Recording(program.recordings[i]._id.ToString(), txt, steps, color);
			review.recordings.Add(rec);
		}

		if (currModeState.GetMainMode() == ModeState.MainMode.Review)
		{
			review.LoadRecordings();
		}
	}

	bool HasLoop(int idToCheck, List<int> wpIdList)
    {
		if(wpIdList.Where(id => id == idToCheck).Count() > 1)
        {
			Debug.Log("Loop detected!!!!!");
			return true;
		}

		return false;
    }


	List<string> GetWaypointStepsString(SerializableWaypoint wp, int startingStepNum)
    {
		List<string> steps = new List<string>();
		int stepNum = startingStepNum;
		for (int k = 0; k < wp.task_subsequence.Length; k++)
		{
			if (wp.task_subsequence[k]._type != "trigger")
			{
				string num = stepNum.ToString();
				string step = wp.task_subsequence[k].cmd;
				string args = "";
				// currently only the "put" command has two args, so working off that to add preposition for clarity
				if (wp.task_subsequence[k].cmd == "moveTo")
				{
					args += wp.task_subsequence[k].args[0].argval;
				}
				else if (wp.task_subsequence[k].cmd == "put")
				{
					args += wp.task_subsequence[k].args[0].argval + " in " + wp.task_subsequence[k].args[1].argval;
				}
				else
				{
					if (wp.task_subsequence[k].args.Length > 0)
					{
						args += wp.task_subsequence[k].args[0].argval;
					}

				}
				//Debug.Log(num + ". " + step + " " + args);
				steps.Add(num + ". " + step + " " + args);
				stepNum++;
			}
		}

		return steps;
    }

	Color32 GetRecordingLineColor(string recId)
    {
		List<string> ignoreList = new List<string> { "ObjectPanel", "BackgroundMap", "Drawer", "InputTextCanvas" };
		GameObject cp = GameObject.Find("ContentPanel");

		for (int i = 0; i < cp.transform.childCount; i++)
		{
			string childName = cp.transform.GetChild(i).gameObject.name;
			if (!ignoreList.Contains(childName))
			{
				string num = childName.Split('_')[0];
				if (recId == num)
				{
					if (childName.Contains("LineBrush"))
					{
						return cp.transform.GetChild(i).gameObject.GetComponent<LineRenderer>().startColor;
					}
				}
			}
		}

		return new Color32(50,50,50,255);
    }

	void OnAvailableEntitiesRequested(EmptyMsg msg)
    {
		Debug.Log("Received request for entities");
		SendAvailableEntities();
    }

	public void SendAvailableEntities()
    {
		//MAYBE CAN CONDENSE???
		Objects objects = GameObject.Find("Objects").GetComponent<Objects>();
		List<AvailableObject> aos = objects.GetAvailableObjects();
		RegionGrid rg = GameObject.Find("Regions").GetComponent<RegionGrid>();
		List<Region> regions = rg.GetRegions();

		// send the objects
		EntityMsg[] ent_array = new EntityMsg[aos.Count + regions.Count];
		for (int i = 0; i < aos.Count; i++)
        {
			AvailableObject ao = aos[i];
			//Debug.Log(ao.name);
			//Debug.Log(ao.categories);
			//Debug.Log(ao.entity_class);
			EntityMsg emsg = new EntityMsg(
				ao.name,
				ao.categories,
				ao.entity_class
			);
			ent_array[i] = emsg;
        }

		// send the regions as well
		for (int i = aos.Count; i < aos.Count + regions.Count; i++)
        {
			Region reg = regions[i - aos.Count];
			string[] cats = { "location", "room" };
			EntityMsg emsg = new EntityMsg(
				reg.GetName(),
				cats,
				"room"
			);
			ent_array[i] = emsg;
        }
		
		AvailableEntitiesMsg msg = new AvailableEntitiesMsg(
			ent_array	
		);
		ros.Publish(sendEntityPubName, msg);
	}

	void OnMessageReceived(StringMsg msg)
    {
		Handheld.Vibrate();
	}
}

[System.Serializable]
public class SerializableObject
{
	public string name;
	public SerializableObject[] objects;
}

public class MapLayout
{
	public int width;
	public int height;
	public float resolution;
	public double[] originPoint;
	public double[] originQuat;
	public int[] data;

	public string SaveToString()
    {
		return JsonUtility.ToJson(this);
    } 
}

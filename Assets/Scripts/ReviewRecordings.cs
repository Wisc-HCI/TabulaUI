using System.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;

/// <summary>
/// Class <c>Recording</c> stores data relevant to visualizing a recording
/// </summary>
[System.Serializable]
public class Recording
{ 
    public string id;
    public string text;
    public List<string> steps;
    public Color32 color;
    public GameObject go;

    public Recording(string _id, string _text, List<string> _steps, Color32 _color)
    {
        id = _id;
        text = _text;
        steps = _steps;
        color = _color;
    }
}

/// <summary>
/// Class <c>ReviewRecordings</c> displays elements relevant to a recording
///  for a user to review.
/// </summary>
public class ReviewRecordings : MonoBehaviour
{

    public GameObject recordingListParent;
    public GameObject recordingListItem;
    public GameObject stepsListParent;
    public GameObject stepsListItem;
    public Text recordingTxt;
    public List<Recording> recordings;
    private StateStorage stateStorage;

    void Start()
    {
        stateStorage = GameObject.Find("StateStorage").GetComponent<StateStorage>();
    }

    public void LoadRecordings()
    {
        foreach (Transform child in recordingListParent.transform)
        {
            if (child.name != "Label")
            {
                Destroy(child.gameObject);
            }
        }

        RectTransform rt = recordingListParent.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, 35f);

        List<int> idList = new List<int>();
        if (recordings.Count > 0)
        {
            foreach (Recording rec in recordings)
            {
                idList.Add(Convert.ToInt32(rec.id));
                GameObject newRecording = Instantiate(recordingListItem) as GameObject;
                newRecording.transform.GetComponentInChildren<Text>().text = "Recording " + rec.id;
                newRecording.transform.Find("Recording Parent/Recording Color").GetComponent<Image>().color = rec.color;
                newRecording.transform.Find("Recording Parent").GetComponent<Button>().onClick.AddListener(() => SetActiveRecording(rec.id));
                newRecording.transform.Find("Delete Recording").GetComponent<Button>().onClick.AddListener(() => DeleteRecording(rec.id));

                newRecording.transform.parent = recordingListParent.transform;
                newRecording.transform.localScale = Vector3.one;
                newRecording.transform.localPosition = new Vector3(newRecording.transform.localPosition.x, newRecording.transform.localPosition.y, 0f);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + 75f);

                rec.go = newRecording;
            }

            SetActiveRecording(idList.AsQueryable().Min().ToString());
        }
        else
        {
            foreach (Transform child in stepsListParent.transform)
            {
                if (child.name != "Label")
                {
                    Destroy(child.gameObject);
                }
            }

            UpdateRecordingTxt("No recordings to show.");
        }
    }

    //void UpdateRecordingList(string name, Color32 color)
    //{
    //    GameObject newRecording = Instantiate(recordingListItem) as GameObject;
    //    newRecording.transform.GetComponentInChildren<Text>().text = name;
    //    newRecording.transform.Find("Recording Parent/Recording Color").GetComponent<Image>().color = color;
    //    newRecording.transform.Find("Recording Parent").GetComponent<Button>().onClick.AddListener(() => SetActiveRecording(name));
    //    newRecording.transform.Find("Delete Recording").GetComponent<Button>().onClick.AddListener(() => DeleteRecording(name));

    //    newRecording.transform.parent = recordingListParent.transform;
    //    newRecording.transform.localScale = Vector3.one;
    //    newRecording.transform.localPosition = new Vector3(newRecording.transform.localPosition.x, newRecording.transform.localPosition.y, 0f);
    //    RectTransform rt = recordingListParent.GetComponent<RectTransform>();
    //    rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + 75f);

    //    recordingObjDict.Add(name, newRecording);
    //}

    void UpdateStepsList(string step)
    {
        GameObject newStep = Instantiate(stepsListItem) as GameObject;
        newStep.transform.GetComponentInChildren<Text>().text = step;

        newStep.transform.parent = stepsListParent.transform;
        newStep.transform.localScale = Vector3.one;
        newStep.transform.localPosition = new Vector3(newStep.transform.localPosition.x, newStep.transform.localPosition.y, 0f);
    }

    void UpdateRecordingTxt(string txt)
    {
        recordingTxt.text = txt;
    }

    void LoadSteps(string active)
    {
        // first, clear current steps list
        foreach(Transform child in stepsListParent.transform)
        {
            if(child.name != "Label")
            {
                Destroy(child.gameObject);
            }
        }

        // then, add new ones
        foreach (Recording rec in recordings)
        {
            if (rec.id == active)
            {
                foreach (string step in rec.steps)
                UpdateStepsList(step);
            }
        }
    }

    void SetActiveRecording(string active)
    {
        foreach (Recording rec in recordings)
        {
            if(rec.id == active)
            {
                rec.go.transform.GetComponentInChildren<Image>().color = Color.white;
                rec.go.transform.Find("Delete Recording").GetComponent<Image>().color = Color.white;

                UpdateRecordingTxt(rec.text);
                LoadSteps(rec.id);
            }
            else
            {
                rec.go.transform.GetComponentInChildren<Image>().color = new Color32(182, 193, 214, 255);
                rec.go.transform.Find("Delete Recording").GetComponent<Image>().color = new Color32(182, 193, 214, 255);
            }
        }
        SetHighlight(active);
    }

    public void SetHighlight(string active_id)
    {
        // then do the deleting
        List<string> ignoreList = new List<string> { "ObjectPanel", "BackgroundMap", "Drawer", "InputTextCanvas" };
        GameObject cp = GameObject.Find("ContentPanel");

        for (int i = 0; i < cp.transform.childCount; i++)
        {
            string childName = cp.transform.GetChild(i).gameObject.name;
            if (!ignoreList.Contains(childName))
            {
                string num = childName.Split('_')[0];
                string typeOfObj = childName.Split('_')[1];

                // highlight active trace, otherwise, set to normal
                if (num == active_id)
                {
                    if (typeOfObj.Contains("LineArrow"))
                    {
                        cp.transform.GetChild(i).gameObject.GetComponent<RectTransform>().localScale = new Vector3(0.8f, 0.8f, 1f);
                    }
                    else if (typeOfObj.Contains("LineBrush"))
                    { 
                        cp.transform.GetChild(i).gameObject.GetComponent<LineRenderer>().SetWidth(0.05f, 0.05f);
                    }
                }
                else
                {
                    if (typeOfObj.Contains("LineArrow"))
                    {
                        cp.transform.GetChild(i).gameObject.GetComponent<RectTransform>().localScale = new Vector3(0.5f, 0.5f, 1f);
                    }
                    else if (typeOfObj.Contains("LineBrush"))
                    {
                        cp.transform.GetChild(i).gameObject.GetComponent<LineRenderer>().SetWidth(0.02312f, 0.02312f);
                    }
                }
            }
        }
    }

    void DeleteRecording(string active)
    {
        stateStorage.DeleteRecording(active);
    }

}

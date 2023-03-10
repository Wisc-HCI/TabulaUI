using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class <c>ModeState</c> Stores current mode of the system.
/// </summary>
public class ModeState : MonoBehaviour
{

    // main mode
    public enum MainMode { Draw, Object, Program, Review, Settings };
    private MainMode currMainMode;

    // object secondary modes
    public enum ObjectMode { Adding, NotAdding };
    private ObjectMode currObjectMode;

    // object tertiary modes
    public enum PlaceMode { None, Person, Groceries, Counter, Cabinets, Toy, Chest }
    private PlaceMode currPlaceMode;

    // program secondary modes
    public enum ProgramMode { Recording, NotRecording };
    private ProgramMode currProgramMode;

    // draw secondary modes
    public enum DrawMode { Select, Brush, Erase };
    private DrawMode currDrawMode;

    // draw tertiary modes
    public enum BrushMode { Small, Medium, Large };
    public enum EraseMode { Small, Medium, Large, Region };
    private BrushMode currBrushMode;
    private EraseMode currEraseMode;

    // mode visualization
    public GameObject recVis;

    // Start is called before the first frame update
    void Start()
    {
        // modes
        currMainMode = MainMode.Draw;
        currDrawMode = DrawMode.Select;
        currBrushMode = BrushMode.Large;
        currEraseMode = EraseMode.Medium;
        currObjectMode = ObjectMode.NotAdding;
        currPlaceMode = PlaceMode.None; //LAURA TEST
        currProgramMode = ProgramMode.NotRecording;

        // vis
        recVis.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // getters
    public MainMode GetMainMode() {
        return currMainMode;
    }

    public DrawMode GetDrawMode() {
        return currDrawMode;
    }

    public BrushMode GetBrushMode() {
        return currBrushMode;
    }

    public EraseMode GetEraseMode() {
        return currEraseMode;
    }

    public ObjectMode GetObjectMode()
    {
        return currObjectMode;
    }

    //LAURA TEST
    public PlaceMode GetPlaceMode()
    {
        return currPlaceMode;
    }

    public ProgramMode GetProgramMode()
    {
        return currProgramMode;
    }

    // setters
    public void SetMainMode(MainMode mm) {
        currMainMode = mm;
    }

    public void SetDrawMode(DrawMode dm) {
        currDrawMode = dm;
    }

    public void SetBrushMode(BrushMode bm) {
        currBrushMode = bm;
    }

    public void SetEraseMode(EraseMode em) {
        currEraseMode = em;
    }

    public void SetObjectMode(ObjectMode om){
        currObjectMode = om;
    }

    //LAURA TEST
    public void SetPlaceMode(PlaceMode pm){
        currPlaceMode = pm;
    }

    public void SetProgramMode(ProgramMode pm)
    {
        currProgramMode = pm;
        if (currProgramMode == ProgramMode.Recording)
        {
            recVis.SetActive(true);
        }
        else
        {
            recVis.SetActive(false);
        }
    }
}

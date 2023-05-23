using System.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Class <c>Menu</c> contains logic for interacting with
///  menu items.
/// </summary>
public class Menu : MonoBehaviour
{
    /*
     * This class will need direct access to ROS
     */
    private RosNode ros;

    /*
     * Public sprites
     */
    public Sprite startRecordingSprite;
    public Sprite stopRecordingSprite;

    /*
     * Track if map was loaded, to know what regions to load
     */
    public Boolean map_loaded;

    /*
     * MAIN MENU
     * At the top of the screen
     */
    // buttons
    private Button loadMapButton;
    private Button loadButton;
    private Button saveButton;
    private Button regionDrawMode;
    private Button objectMode;
    private Button programMode;
    private Button reviewMode;
    private Button settingsMode;
    private Image regionImg;
    private Image objectImg;
    private Image programImg;
    private Image reviewImg;
    private Image settingsImg;
    private Text regionTxt;
    private Text objectTxt;
    private Text programTxt;
    private Text reviewTxt;
    
    // toolbars
    private GameObject mainMenuToolbar;
    private GameObject modeSelectorToolbar;
    private GameObject speechFeedback;
    private GameObject recordingStepsPanel;
    private GameObject recordingDisplayPanel;
    private GameObject settingsPanel;

    // the state of the buttons is stored here
    private enum ButtonState { MSToolbarHidden, MSToolbarShown,
                               DrawModeToggled};
    private ButtonState currMenuState;
    private ButtonState currToggledState;

    /*
     * DRAW MENU
     * At the bottom of the screen
     */
    // toolbar
    private GameObject drawMenuToolbar;
    private GameObject addObjectsToolbar;
    private Button removeObjectButton;
    private GameObject reviewToolbar;
    
    // drawing buttons
    private Button selectToggler;
    private Button brushToggler;
    private Button eraseToggler;

    // recording buttons
    private Button recordToggler;
    private Image recordTogglerImg;

    private RectTransform contentPanel;

    // submenu buttons
    private GameObject brushLargeToggler;
    private GameObject brushMediumToggler;
    private GameObject brushSmallToggler;
    private GameObject eraseRegionToggler;
    private GameObject eraseLargeToggler;
    private GameObject eraseMediumToggler;
    private GameObject eraseSmallToggler;

    // object submenu buttons
    private GameObject groceriesToggler;
    private Button groceriesTogglerButton;
    private GameObject personToggler;
    private Button personTogglerButton;
    private GameObject cabinetsToggler;
    private Button cabinetsTogglerButton;
    private GameObject toyToggler;
    private Button toyTogglerButton;
    private GameObject chestToggler;
    private Button chestTogglerButton;

    // settings options
    private GameObject RosIpInputText;


    // the state of the draw buttons is stored here
    private enum DrawMenuState { DrawToolbarHidden, DrawToolbarShown,
                                   SelectToggled, BrushToggled, EraseToggled,
                                   BrushSubmenuToggled, EraseSubmenuToggled, SubmenusUntoggled ,
                                   RecordingToggled, RecordingNotToggled };
    private DrawMenuState currDrawMenuState;
    private DrawMenuState currDrawButtonState;
    private DrawMenuState currSubmenuState;
    private enum ObjectMenuState { AddToggled, AddNotToggled, 
                                    AddPersonToggled, AddGroceriesToggled, AddCabinetsToggled, AddToyToggled, AddChestToggled, SubmenuUntoggled 
                                };
    private ObjectMenuState currObjectMenuState;
    private ObjectMenuState currObjectSubmenuState;
    private enum ProgramMenuState
    {
       RecordingToggled, RecordingNotToggled
    };
    private ProgramMenuState currProgramMenuState;

    // different gameobjects
    private ModeState currModeState;
    private StateStorage stateStorage;
    private AudioInput audioInput;
    private ReviewRecordings review;

    // colors
    private Color32 buttonSelected;
    private Color32 textLight;
    private Color32 textDark;

    // Start is called before the first frame update
    void Start()
    {
        /*
         * ROS
         */
        ros = GameObject.FindObjectOfType<RosNode>();

        /*
         * Corner menu
         */
        loadMapButton = GameObject.Find("Request Map").GetComponent<Button>();
        loadMapButton.onClick.AddListener(LoadMap);
        loadButton = GameObject.Find("Load").GetComponent<Button>();
        loadButton.onClick.AddListener(Load);
        saveButton = GameObject.Find("Save").GetComponent<Button>();
        saveButton.onClick.AddListener(Save);
        map_loaded = false;

        /*
         * MAIN MENU
         */
        currModeState = GameObject.Find("ModeState").GetComponent<ModeState>();
        regionDrawMode = GameObject.Find("Region Draw Mode").GetComponent<Button>();
        regionDrawMode.onClick.AddListener(RegionDrawModeToggled);
        regionImg = GameObject.Find("Region Img").GetComponent<Image>();
        regionTxt = GameObject.Find("Region Txt").GetComponent<Text>();
        objectMode = GameObject.Find("Object Placement Mode").GetComponent<Button>();
        objectMode.onClick.AddListener(ObjectModeToggled);
        objectImg = GameObject.Find("Object Img").GetComponent<Image>();
        objectTxt = GameObject.Find("Object Txt").GetComponent<Text>();
        programMode = GameObject.Find("Program Mode").GetComponent<Button>();
        programMode.onClick.AddListener(ProgramModeToggled);
        programImg = GameObject.Find("Program Img").GetComponent<Image>();
        programTxt = GameObject.Find("Program Txt").GetComponent<Text>(); 
        reviewMode = GameObject.Find("Review Mode").GetComponent<Button>();
        reviewMode.onClick.AddListener(ReviewModeToggled);
        reviewImg = GameObject.Find("Review Img").GetComponent<Image>();
        reviewTxt = GameObject.Find("Review Txt").GetComponent<Text>();
        settingsMode = GameObject.Find("Settings Mode").GetComponent<Button>();
        settingsImg = GameObject.Find("Settings Img").GetComponent<Image>();
        settingsMode.onClick.AddListener(SettingsModeToggled);
        modeSelectorToolbar = GameObject.Find("Mode Selector Menu");
        modeSelectorToolbar.SetActive(true);
        mainMenuToolbar = GameObject.Find("Main Tool Menu");
        speechFeedback = GameObject.Find("Speech Feedback");
        currMenuState = ButtonState.MSToolbarShown;
        currToggledState = ButtonState.DrawModeToggled;

        /*
         * io
         */
        audioInput = GameObject.Find("Audio Source").GetComponent<AudioInput>();
        stateStorage = GameObject.Find("StateStorage").GetComponent<StateStorage>();

        /*
         * DRAW MENU
         */
        drawMenuToolbar = GameObject.Find("Draw Panel Main");
        selectToggler = GameObject.Find("Select").GetComponent<Button>();
        brushToggler = GameObject.Find("Draw").GetComponent<Button>();
        eraseToggler = GameObject.Find("Erase").GetComponent<Button>();
        brushLargeToggler = GameObject.Find("Brush Large");
        brushLargeToggler.SetActive(false);
        brushMediumToggler = GameObject.Find("Brush Medium");
        brushMediumToggler.SetActive(false);
        brushSmallToggler = GameObject.Find("Brush Small");
        brushSmallToggler.SetActive(false);
        eraseRegionToggler = GameObject.Find("Erase Region");
        eraseRegionToggler.SetActive(false);
        eraseLargeToggler = GameObject.Find("Erase Large");
        eraseLargeToggler.SetActive(false);
        eraseMediumToggler = GameObject.Find("Erase Medium");
        eraseMediumToggler.SetActive(false);
        eraseSmallToggler = GameObject.Find("Erase Small");
        eraseSmallToggler.SetActive(false);
        selectToggler.onClick.AddListener(SelectTogglerOnClick);
        brushToggler.onClick.AddListener(BrushTogglerOnClick);
        eraseToggler.onClick.AddListener(EraseTogglerOnClick);
        brushLargeToggler.GetComponent<Button>().onClick.AddListener(BrushLargeTogglerOnClick);
        brushMediumToggler.GetComponent<Button>().onClick.AddListener(BrushMediumTogglerOnClick);
        brushSmallToggler.GetComponent<Button>().onClick.AddListener(BrushSmallTogglerOnClick);
        eraseRegionToggler.GetComponent<Button>().onClick.AddListener(EraseRegionTogglerOnClick);
        eraseLargeToggler.GetComponent<Button>().onClick.AddListener(EraseLargeTogglerOnClick);
        eraseMediumToggler.GetComponent<Button>().onClick.AddListener(EraseMediumTogglerOnClick);
        eraseSmallToggler.GetComponent<Button>().onClick.AddListener(EraseSmallTogglerOnClick);
        currDrawMenuState = DrawMenuState.DrawToolbarShown;
        currDrawButtonState = DrawMenuState.SelectToggled;
        currSubmenuState = DrawMenuState.SubmenusUntoggled;
        currProgramMenuState = ProgramMenuState.RecordingNotToggled;

        /*
         * OBJECT MENU
         */
        addObjectsToolbar = GameObject.Find("Add Object Panel");
        removeObjectButton = GameObject.Find("Remove").GetComponent<Button>();

        currObjectMenuState = ObjectMenuState.AddNotToggled;
        currObjectSubmenuState = ObjectMenuState.SubmenuUntoggled;  
        groceriesToggler = GameObject.Find("Place Groceries");
        groceriesTogglerButton = groceriesToggler.GetComponent<Button>();
        groceriesTogglerButton.onClick.AddListener(GroceriesTogglerOnClick);
        personToggler = GameObject.Find("Place Person");
        personTogglerButton = personToggler.GetComponent<Button>();
        personTogglerButton.onClick.AddListener(PersonTogglerOnClick);
        cabinetsToggler = GameObject.Find("Place Cabinets");
        cabinetsTogglerButton = cabinetsToggler.GetComponent<Button>();
        cabinetsTogglerButton.onClick.AddListener(CabinetsTogglerOnClick);
        toyToggler = GameObject.Find("Place Toy");
        toyTogglerButton = toyToggler.GetComponent<Button>(); ;
        toyTogglerButton.onClick.AddListener(ToyTogglerOnClick);
        chestToggler = GameObject.Find("Place Chest");
        chestTogglerButton = chestToggler.GetComponent<Button>(); ;
        chestTogglerButton.onClick.AddListener(ChestTogglerOnClick);


        /*
         * PROGRAMMING MENU
         */
        recordToggler = GameObject.Find("Record").GetComponent<Button>();
        recordToggler.onClick.AddListener(RecordTogglerOnClick);
        recordTogglerImg = GameObject.Find("Record").GetComponent<Image>();
        contentPanel = GameObject.Find("ContentPanel").GetComponent<RectTransform>();


        /*
         * REVIEW MENU
         */
        reviewToolbar = GameObject.Find("Recording List");
        recordingStepsPanel = GameObject.Find("Recording Steps");
        recordingDisplayPanel = GameObject.Find("Recording Text Canvas");
        review = GameObject.Find("Review").GetComponent<ReviewRecordings>();

        /*
         * SETTINGS MENU
         */
        settingsPanel = GameObject.Find("Settings");
        RosIpInputText = GameObject.Find("ROS IP Input");
        RosIpInputText.GetComponent<InputField>().onEndEdit.AddListener(delegate { UpdateRosIp(); });
        //UpdateRosIpButton = GameObject.Find("ROS IP Input").GetComponent<Button>();
        //UpdateRosIpButton.onClick.AddListener(ChangeIpOnClick);


        // color
        buttonSelected = new Color32(182, 193, 214, 255);
        textLight = new Color32(80,80,80,255);
        textDark = new Color32(50,50,50,255);

        // set everything correctly and then update the menu state
        RegionDrawModeToggled();
        ChangeButtonColor(selectToggler, true);
        ChangeButtonColor(brushLargeToggler.GetComponent<Button>(), true);
        ChangeButtonColor(eraseMediumToggler.GetComponent<Button>(), true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LoadMap()
    {
        //ros.SendMapRequest();
        ros.LoadStoreMap();
        map_loaded = true;
    }

    void Save()
    {
        Debug.Log("SAVING");
        SaveData sd = new SaveData();
        RegionGrid rg = GameObject.Find("Regions").GetComponent<RegionGrid>();
        drawer d = GameObject.Find("Drawer").GetComponent<drawer>();
        sd.regions = rg.SaveRegions();
        sd.colors = d.SaveColors();
        Debug.Log(rg.SaveRegions().Length);
        Debug.Log(rg.SaveRegions()[0]);
        string jsonString = sd.SaveToString();
        Debug.Log(jsonString);
        string path = "Assets/Resources/regions.json";
        StreamWriter writer = new StreamWriter(path, false);
        writer.Write(jsonString);
        writer.Close();
    }

    void Load()
    {
        //string path = "Assets/Resources/regions.json";
        //StreamReader reader = new StreamReader(path);
        //string jsonString = reader.ReadToEnd();
        TextAsset regText = Resources.Load<TextAsset>("regions_default");
        if (map_loaded)
        {
            regText = Resources.Load<TextAsset>("regions_user_study_store");
        }
        string jsonString = regText.text;
        Debug.Log(jsonString);
        //reader.Close();
        SaveData sd = JsonUtility.FromJson<SaveData>(jsonString);
        RegionGrid rg = GameObject.Find("Regions").GetComponent<RegionGrid>();
        rg.LoadRegions(sd.regions);
        drawer d = GameObject.Find("Drawer").GetComponent<drawer>();
        d.LoadColors(sd.colors);
    }

    void RegionDrawModeToggled()
    {
        ChangeButtonColor(regionDrawMode, false);
        ChangeButtonColor(objectMode, true);
        ChangeButtonColor(programMode, true);
        ChangeButtonColor(reviewMode, true);
        ChangeButtonColor(settingsMode, true);
        regionImg.color = Color.white;
        regionTxt.color = textDark;
        objectImg.color = buttonSelected;
        objectTxt.color = textLight;
        programImg.color = buttonSelected;
        programTxt.color = textLight;
        reviewImg.color = buttonSelected;
        reviewTxt.color = textLight;
        settingsImg.color = buttonSelected;

        drawMenuToolbar.SetActive(true);
        addObjectsToolbar.SetActive(false);
        removeObjectButton.gameObject.SetActive(false);
        recordToggler.gameObject.SetActive(false);
        loadButton.gameObject.SetActive(true);
        saveButton.gameObject.SetActive(true);
        loadMapButton.gameObject.SetActive(true);
        reviewToolbar.SetActive(false);
        recordingStepsPanel.SetActive(false);
        recordingDisplayPanel.SetActive(false);
        settingsPanel.SetActive(false);
        contentPanel.localScale = new Vector3(1f, 1f, 0f);
        review.SetHighlight("None");

        currModeState.SetMainMode(ModeState.MainMode.Draw);
    }

    void ObjectModeToggled()
    {
        ChangeButtonColor(regionDrawMode, true);
        ChangeButtonColor(objectMode, false);
        ChangeButtonColor(programMode, true);
        ChangeButtonColor(reviewMode, true);
        ChangeButtonColor(settingsMode, true);
        regionImg.color = buttonSelected;
        regionTxt.color = textLight;
        objectImg.color = Color.white;
        objectTxt.color = textDark;
        programImg.color = buttonSelected;
        programTxt.color = textLight;
        reviewImg.color = buttonSelected;
        reviewTxt.color = textLight;
        settingsImg.color = buttonSelected;

        drawMenuToolbar.SetActive(false);
        addObjectsToolbar.SetActive(true);
        removeObjectButton.gameObject.SetActive(true);
        recordToggler.gameObject.SetActive(false);
        loadButton.gameObject.SetActive(false);
        saveButton.gameObject.SetActive(false);
        loadMapButton.gameObject.SetActive(false);
        reviewToolbar.SetActive(false);
        recordingStepsPanel.SetActive(false);
        recordingDisplayPanel.SetActive(false);
        settingsPanel.SetActive(false);
        contentPanel.localScale = new Vector3(1f, 1f, 0f);
        review.SetHighlight("None");

        currModeState.SetMainMode(ModeState.MainMode.Object);
    }

    void ProgramModeToggled()
    {
        ChangeButtonColor(regionDrawMode, true);
        ChangeButtonColor(objectMode, true);
        ChangeButtonColor(programMode, false);
        ChangeButtonColor(reviewMode, true);
        ChangeButtonColor(settingsMode, true);
        regionImg.color = buttonSelected;
        regionTxt.color = textLight;
        objectImg.color = buttonSelected;
        objectTxt.color = textLight;
        programImg.color = Color.white;
        programTxt.color = textDark;
        reviewImg.color = buttonSelected;
        reviewTxt.color = textLight;
        settingsImg.color = buttonSelected;

        drawMenuToolbar.SetActive(false);
        addObjectsToolbar.SetActive(false);
        removeObjectButton.gameObject.SetActive(false);
        recordToggler.gameObject.SetActive(true);
        loadButton.gameObject.SetActive(false);
        saveButton.gameObject.SetActive(false);
        loadMapButton.gameObject.SetActive(false);
        reviewToolbar.SetActive(false);
        recordingStepsPanel.SetActive(false);
        recordingDisplayPanel.SetActive(false);
        settingsPanel.SetActive(false);
        contentPanel.localScale = new Vector3(1f, 1f, 0f);
        review.SetHighlight("None");

        currModeState.SetMainMode(ModeState.MainMode.Program);
    }

    void ReviewModeToggled()
    {
        ChangeButtonColor(regionDrawMode, true);
        ChangeButtonColor(objectMode, true);
        ChangeButtonColor(programMode, true);
        ChangeButtonColor(reviewMode, false);
        ChangeButtonColor(settingsMode, true);
        regionImg.color = buttonSelected;
        regionTxt.color = textLight;
        objectImg.color = buttonSelected;
        objectTxt.color = textLight;
        programImg.color = buttonSelected;
        programTxt.color = textLight;
        reviewImg.color = Color.white;
        reviewTxt.color = textDark;
        settingsImg.color = buttonSelected;

        drawMenuToolbar.SetActive(false);
        addObjectsToolbar.SetActive(false);
        removeObjectButton.gameObject.SetActive(false);
        recordToggler.gameObject.SetActive(false);
        loadButton.gameObject.SetActive(false);
        saveButton.gameObject.SetActive(false);
        loadMapButton.gameObject.SetActive(false);
        reviewToolbar.SetActive(true);
        recordingStepsPanel.SetActive(true);
        recordingDisplayPanel.SetActive(true);
        settingsPanel.SetActive(false);
        contentPanel.localScale = new Vector3(0.8f, 0.8f, 0f);

        review.recordingListParent = GameObject.Find("Recording List");
        review.stepsListParent = GameObject.Find("Recording Steps");
        review.recordingTxt = GameObject.Find("Recording Txt").GetComponent<Text>();

        review.LoadRecordings();
        //ros.ProcessWorld(ros.su.world);

        currModeState.SetMainMode(ModeState.MainMode.Review);

    }

    void SettingsModeToggled()
    {
        ChangeButtonColor(regionDrawMode, true);
        ChangeButtonColor(objectMode, true);
        ChangeButtonColor(programMode, true);
        ChangeButtonColor(reviewMode, true);
        ChangeButtonColor(settingsMode, false);

        regionImg.color = buttonSelected;
        regionTxt.color = textLight;
        objectImg.color = buttonSelected;
        objectTxt.color = textLight;
        programImg.color = buttonSelected;
        programTxt.color = textLight;
        reviewImg.color = buttonSelected;
        reviewTxt.color = textLight;
        settingsImg.color = Color.white;

        drawMenuToolbar.SetActive(false);
        addObjectsToolbar.SetActive(false);
        removeObjectButton.gameObject.SetActive(false);
        recordToggler.gameObject.SetActive(false);
        loadButton.gameObject.SetActive(false);
        saveButton.gameObject.SetActive(false);
        loadMapButton.gameObject.SetActive(false);
        reviewToolbar.SetActive(false);
        recordingStepsPanel.SetActive(false);
        recordingDisplayPanel.SetActive(false);
        settingsPanel.SetActive(true);
        contentPanel.localScale = new Vector3(1f, 1f, 0f);
        review.SetHighlight("None");

        currModeState.SetMainMode(ModeState.MainMode.Settings);

    }

    public void UpdateRosIp()
    {
        String newIP = RosIpInputText.GetComponent<InputField>().text;

        //first somehow check for valid IP
        System.Net.IPAddress ipAddress = null;

        bool isValidIp = System.Net.IPAddress.TryParse(newIP, out ipAddress);

        if(isValidIp)
        {
            //if it is, send it to ROS
            Debug.Log(newIP);

            //then send to ROS
            ros.SetRosIp(newIP);
        }
        else
        {
            RosIpInputText.GetComponent<InputField>().text = "Please enter valid IP";
        }

    }

    public void AddTogglerOnClick()
    {
        if (currObjectMenuState == ObjectMenuState.AddNotToggled)
        {
            ChangeButtonColor(removeObjectButton, true);
            currObjectMenuState = ObjectMenuState.AddToggled;
            currModeState.SetObjectMode(ModeState.ObjectMode.Adding);
        }
        else
        {
            ChangeButtonColor(removeObjectButton, false);
            ChangeButtonColor(groceriesTogglerButton, false);
            ChangeButtonColor(personTogglerButton, false);
            ChangeButtonColor(cabinetsTogglerButton, false);
            ChangeButtonColor(toyTogglerButton, false);
            ChangeButtonColor(chestTogglerButton, false);
            currObjectMenuState = ObjectMenuState.AddNotToggled;
            currModeState.SetObjectMode(ModeState.ObjectMode.NotAdding);
        }
    }

    void GroceriesTogglerOnClick()
    {
        if (currObjectSubmenuState == ObjectMenuState.AddGroceriesToggled)
        {
            currObjectMenuState = ObjectMenuState.SubmenuUntoggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.None);
            ChangeButtonColor(groceriesTogglerButton, false);
            ChangeButtonColor(personTogglerButton, false);
            ChangeButtonColor(cabinetsTogglerButton, false);
            ChangeButtonColor(toyTogglerButton, false);
            ChangeButtonColor(chestTogglerButton, false);
        }
        else{
            currObjectMenuState = ObjectMenuState.AddGroceriesToggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.Groceries);
            ChangeButtonColor(groceriesTogglerButton, false);
            ChangeButtonColor(personTogglerButton, true);
            ChangeButtonColor(cabinetsTogglerButton, true);
            ChangeButtonColor(toyTogglerButton, true);
            ChangeButtonColor(chestTogglerButton, true);
        }
    }

    void PersonTogglerOnClick()
    {
        if (currObjectSubmenuState == ObjectMenuState.AddPersonToggled)
        {
            currObjectMenuState = ObjectMenuState.SubmenuUntoggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.None);
            ChangeButtonColor(groceriesTogglerButton, false);
            ChangeButtonColor(personTogglerButton, false);
            ChangeButtonColor(cabinetsTogglerButton, false);
            ChangeButtonColor(toyTogglerButton, false);
            ChangeButtonColor(chestTogglerButton, false);
        }
        else{
            currObjectMenuState = ObjectMenuState.AddPersonToggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.Person);
            ChangeButtonColor(groceriesTogglerButton, true);
            ChangeButtonColor(personTogglerButton, false);
            ChangeButtonColor(cabinetsTogglerButton, true);
            ChangeButtonColor(toyTogglerButton, true);
            ChangeButtonColor(chestTogglerButton, true);
        }
    }

    void CabinetsTogglerOnClick()
    {
        if (currObjectSubmenuState == ObjectMenuState.AddCabinetsToggled)
        {
            currObjectMenuState = ObjectMenuState.SubmenuUntoggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.None);
            ChangeButtonColor(groceriesTogglerButton, false);
            ChangeButtonColor(personTogglerButton, false);
            ChangeButtonColor(cabinetsTogglerButton, false);
            ChangeButtonColor(toyTogglerButton, false);
            ChangeButtonColor(chestTogglerButton, false);
        }
        else{
            currObjectMenuState = ObjectMenuState.AddCabinetsToggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.Cabinets);
            ChangeButtonColor(groceriesTogglerButton, true);
            ChangeButtonColor(personTogglerButton, true);
            ChangeButtonColor(cabinetsTogglerButton, false);
            ChangeButtonColor(toyTogglerButton, true);
            ChangeButtonColor(chestTogglerButton, true);
        }
    }

    void ToyTogglerOnClick()
    {
        if (currObjectSubmenuState == ObjectMenuState.AddToyToggled)
        {
            currObjectMenuState = ObjectMenuState.SubmenuUntoggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.None);
            ChangeButtonColor(groceriesTogglerButton, false);
            ChangeButtonColor(personTogglerButton, false);
            ChangeButtonColor(cabinetsTogglerButton, false);
            ChangeButtonColor(toyTogglerButton, false);
            ChangeButtonColor(chestTogglerButton, false);
        }
        else
        {
            currObjectMenuState = ObjectMenuState.AddToyToggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.Toy);
            ChangeButtonColor(groceriesTogglerButton, true);
            ChangeButtonColor(personTogglerButton, true);
            ChangeButtonColor(cabinetsTogglerButton, true);
            ChangeButtonColor(toyTogglerButton, false);
            ChangeButtonColor(chestTogglerButton, true);
        }
    }

    void ChestTogglerOnClick()
    {
        if (currObjectSubmenuState == ObjectMenuState.AddChestToggled)
        {
            currObjectMenuState = ObjectMenuState.SubmenuUntoggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.None);
            ChangeButtonColor(groceriesTogglerButton, false);
            ChangeButtonColor(personTogglerButton, false);
            ChangeButtonColor(cabinetsTogglerButton, false);
            ChangeButtonColor(toyTogglerButton, false);
            ChangeButtonColor(chestTogglerButton, false);
        }
        else
        {
            currObjectMenuState = ObjectMenuState.AddChestToggled;
            currModeState.SetPlaceMode(ModeState.PlaceMode.Chest);
            ChangeButtonColor(groceriesTogglerButton, true);
            ChangeButtonColor(personTogglerButton, true);
            ChangeButtonColor(cabinetsTogglerButton, true);
            ChangeButtonColor(toyTogglerButton, true);
            ChangeButtonColor(chestTogglerButton, false);
        }
    }

    void RecordTogglerOnClick()
    {
        if (currProgramMenuState == ProgramMenuState.RecordingNotToggled)
        {
            recordTogglerImg.sprite = stopRecordingSprite;
            contentPanel.localScale = new Vector3(0.8f, 0.8f, 0f);
            currProgramMenuState = ProgramMenuState.RecordingToggled;
            regionDrawMode.interactable = false;
            objectMode.interactable = false;
            programMode.interactable = false;
            reviewMode.interactable = false;

            StartCoroutine(SlideDown(0.25f));
            currModeState.SetProgramMode(ModeState.ProgramMode.Recording);
            ros.BeginRecord();
            audioInput.Record();
        }
        else
        {
            recordTogglerImg.sprite = startRecordingSprite;
            regionDrawMode.interactable = true;
            objectMode.interactable = true;
            programMode.interactable = true;
            reviewMode.interactable = true;

            contentPanel.localScale = new Vector3(1f, 1f, 0f);
            StartCoroutine(SlideUp(0.25f));
            currProgramMenuState = ProgramMenuState.RecordingNotToggled;
            currModeState.SetProgramMode(ModeState.ProgramMode.NotRecording);
            string text = audioInput.Stop();
            WpLabelIDPair[] trace;
            trace = stateStorage.GetStringTrace();
            stateStorage.ClearStorage();
            if (trace.Length > 0)  // only send a non-empty trace
            {
                //ros.SendRecording(text, trace);
                ros.EndRecord(text, trace);
            }
        }
    }

    void SelectTogglerOnClick()
    {
        // toggle select, untoggle all else
        ChangeButtonColor(selectToggler, true);
        ChangeButtonColor(brushToggler, false);
        ChangeButtonColor(eraseToggler, false);
        currDrawButtonState = DrawMenuState.SelectToggled;
        currSubmenuState = DrawMenuState.SubmenusUntoggled;
        ToggleBrushSubmenu(false);
        ToggleEraseSubmenu(false);
        currModeState.SetDrawMode(ModeState.DrawMode.Select);
    }

    void BrushTogglerOnClick()
    {
        // toggle brush, untoggle all else
        ChangeButtonColor(selectToggler, false);
        ChangeButtonColor(brushToggler, true);
        ChangeButtonColor(eraseToggler, false);
        if (currDrawButtonState == DrawMenuState.BrushToggled)
        {
            if (currSubmenuState == DrawMenuState.BrushSubmenuToggled)
            {
                currSubmenuState = DrawMenuState.SubmenusUntoggled;
                ToggleBrushSubmenu(false);
            } else
            {
                currSubmenuState = DrawMenuState.BrushSubmenuToggled;
                ToggleBrushSubmenu(true);
                ToggleEraseSubmenu(false);
            }
        } else
        {
            currSubmenuState = DrawMenuState.BrushSubmenuToggled;
            ToggleBrushSubmenu(true);
            ToggleEraseSubmenu(false);
        }
        currDrawButtonState = DrawMenuState.BrushToggled;
        currModeState.SetDrawMode(ModeState.DrawMode.Brush);
    }

    void EraseTogglerOnClick()
    {
        // toggle erase, untoggle all else
        ChangeButtonColor(selectToggler, false);
        ChangeButtonColor(brushToggler, false);
        ChangeButtonColor(eraseToggler, true);
        if (currDrawButtonState == DrawMenuState.EraseToggled)
        {
            if (currSubmenuState == DrawMenuState.EraseSubmenuToggled)
            {
                currSubmenuState = DrawMenuState.SubmenusUntoggled;
                ToggleEraseSubmenu(false);
            }
            else
            {
                currSubmenuState = DrawMenuState.EraseSubmenuToggled;
                ToggleEraseSubmenu(true);
                ToggleBrushSubmenu(false);
            }
        }
        else
        {
            currSubmenuState = DrawMenuState.EraseSubmenuToggled;
            ToggleEraseSubmenu(true);
            ToggleBrushSubmenu(false);
        }
        currDrawButtonState = DrawMenuState.EraseToggled;
        currModeState.SetDrawMode(ModeState.DrawMode.Erase);
    }

    void ToggleBrushSubmenu(bool show)
    {
        if (show)
        {
            brushLargeToggler.SetActive(true);
            brushMediumToggler.SetActive(true);
            brushSmallToggler.SetActive(true);
            brushToggler.transform.parent.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, 0.3f);
        } else
        {
            brushLargeToggler.SetActive(false);
            brushMediumToggler.SetActive(false);
            brushSmallToggler.SetActive(false);
            brushToggler.transform.parent.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        }
    }

    void ToggleEraseSubmenu(bool show)
    {
        if (show)
        {
            eraseRegionToggler.SetActive(true);
            eraseLargeToggler.SetActive(true);
            eraseMediumToggler.SetActive(true);
            eraseSmallToggler.SetActive(true);
            eraseToggler.transform.parent.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, 0.3f);
        }
        else
        {
            eraseRegionToggler.SetActive(false);
            eraseLargeToggler.SetActive(false);
            eraseMediumToggler.SetActive(false);
            eraseSmallToggler.SetActive(false);
            eraseToggler.transform.parent.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        }
    }

    void BrushLargeTogglerOnClick()
    {
        ChangeButtonColor(brushLargeToggler.GetComponent<Button>(), true);
        ChangeButtonColor(brushMediumToggler.GetComponent<Button>(), false);
        ChangeButtonColor(brushSmallToggler.GetComponent<Button>(), false);
        currModeState.SetBrushMode(ModeState.BrushMode.Large);
    }

    void BrushMediumTogglerOnClick()
    {
        ChangeButtonColor(brushLargeToggler.GetComponent<Button>(), false);
        ChangeButtonColor(brushMediumToggler.GetComponent<Button>(), true);
        ChangeButtonColor(brushSmallToggler.GetComponent<Button>(), false);
        currModeState.SetBrushMode(ModeState.BrushMode.Medium);
    }

    void BrushSmallTogglerOnClick()
    {
        ChangeButtonColor(brushLargeToggler.GetComponent<Button>(), false);
        ChangeButtonColor(brushMediumToggler.GetComponent<Button>(), false);
        ChangeButtonColor(brushSmallToggler.GetComponent<Button>(), true);
        currModeState.SetBrushMode(ModeState.BrushMode.Small);
    }

    void EraseRegionTogglerOnClick()
    {
        ChangeButtonColor(eraseRegionToggler.GetComponent<Button>(), true);
        ChangeButtonColor(eraseLargeToggler.GetComponent<Button>(), false);
        ChangeButtonColor(eraseMediumToggler.GetComponent<Button>(), false);
        ChangeButtonColor(eraseSmallToggler.GetComponent<Button>(), false);
        currModeState.SetEraseMode(ModeState.EraseMode.Region);
    }

    void EraseLargeTogglerOnClick()
    {
        ChangeButtonColor(eraseRegionToggler.GetComponent<Button>(), false);
        ChangeButtonColor(eraseLargeToggler.GetComponent<Button>(), true);
        ChangeButtonColor(eraseMediumToggler.GetComponent<Button>(), false);
        ChangeButtonColor(eraseSmallToggler.GetComponent<Button>(), false);
        currModeState.SetEraseMode(ModeState.EraseMode.Large);
    }

    void EraseMediumTogglerOnClick()
    {
        ChangeButtonColor(eraseRegionToggler.GetComponent<Button>(), false);
        ChangeButtonColor(eraseLargeToggler.GetComponent<Button>(), false);
        ChangeButtonColor(eraseMediumToggler.GetComponent<Button>(), true);
        ChangeButtonColor(eraseSmallToggler.GetComponent<Button>(), false);
        currModeState.SetEraseMode(ModeState.EraseMode.Medium);
    }

    void EraseSmallTogglerOnClick()
    {
        ChangeButtonColor(eraseRegionToggler.GetComponent<Button>(), false);
        ChangeButtonColor(eraseLargeToggler.GetComponent<Button>(), false);
        ChangeButtonColor(eraseMediumToggler.GetComponent<Button>(), false);
        ChangeButtonColor(eraseSmallToggler.GetComponent<Button>(), true);
        currModeState.SetEraseMode(ModeState.EraseMode.Small);
    }

    void ChangeButtonColor(Button bt, bool selected)
    {
        if (selected)
        {
            var colors = bt.colors;
            colors.normalColor = buttonSelected;
            colors.highlightedColor = buttonSelected;
            colors.selectedColor = buttonSelected;
            bt.colors = colors;
        } else
        {
            var colors = bt.colors;
            colors.normalColor = Color.white;
            colors.selectedColor = Color.white;
            colors.highlightedColor = Color.white;
            bt.colors = colors;
        }
    }

    /*
     * These functions control the speech hypothesis text box animation
     * SlideDown() and SlideUp() only differ in direction of animation
     */

    IEnumerator SlideDown(float duration)
    {
        Vector3 pos = new Vector3(
            speechFeedback.transform.localPosition.x,
            speechFeedback.transform.localPosition.y,
            speechFeedback.transform.localPosition.z
        );
        yield return Slide(pos, new Vector3(pos.x, pos.y - 210, pos.z), duration);
    }

    IEnumerator SlideUp(float duration)
    {
        Vector3 pos = new Vector3(
            speechFeedback.transform.localPosition.x,
            speechFeedback.transform.localPosition.y,
            speechFeedback.transform.localPosition.z
        );
        yield return Slide(pos, new Vector3(pos.x, pos.y + 210, pos.z), duration);
    }

    IEnumerator Slide(Vector3 start, Vector3 end, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            speechFeedback.transform.localPosition = Vector3.Lerp(start, end, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        speechFeedback.transform.localPosition = end;
    }
}

public class SaveData
{
    [SerializeField] public RegionSaveData[] regions;
    [SerializeField] public Color32Data[] colors;

    public string SaveToString()
    {
        return JsonUtility.ToJson(this);
    }
}

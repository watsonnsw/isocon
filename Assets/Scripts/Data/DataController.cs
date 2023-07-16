using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UIElements;

public enum FileOp {
    Save,
    Load
}

public class DataController : MonoBehaviour
{
    public static string currentFileName;
    public static FileOp currentOp;
    
    // Start is called before the first frame update
    void Start()
    {
        setup();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void setup() {
        List<(string, string, EventCallback<ClickEvent>)> uiConfig = new List<(string, string, EventCallback<ClickEvent>)>{
            ("SaveMapButton", "Save the current map", SaveMap),
            ("SaveMapAsButton", "Save a copy of the current map", SaveMapCopy),
            ("LoadButton", "Load a map from the disk", LoadMap),
            ("ResetButton", "Reset to the base map", ResetMap),
            ("ExitButton", "Exit the program", ExitProgram),
            ("FilenameConfirm", "Save with this filename", FilenameConfirm),
            ("FilenameCancel", "Do not save", FilenameCancel),
        };   

        foreach((string, string, EventCallback<ClickEvent>) item in uiConfig) {
            UI.AttachHelp(UI.System, item.Item1, item.Item2);
            UI.System.Q(item.Item1).RegisterCallback<ClickEvent>(item.Item3);
        }
    }

    private void SaveMap(ClickEvent evt) {
        DataController.currentOp = FileOp.Save;
        if (DataController.currentFileName != null && DataController.currentFileName.Length > 0) {
            State.SaveState(DataController.currentFileName);
            UI.SetHelpText("Map saved", HelpType.Success);
        }
        else {
            GetComponent<ModeController>().ActivateElementByName("FilenameModal");
        }
    }

    private void SaveMapCopy(ClickEvent evt) {
        DataController.currentOp = FileOp.Save;
        GetComponent<ModeController>().ActivateElementByName("FilenameModal");
    }

    private void LoadMap(ClickEvent evt) {
        InitializeFileList();
        GetComponent<ModeController>().ActivateElementByName("LoadFileModal");
    }

    private void ResetMap(ClickEvent evt) {
        DataController.currentFileName = null;
        TerrainController.ResetTerrain();
    }

    private void ExitProgram(ClickEvent evt) {
        UI.System.Q("ExitButton").RegisterCallback<ClickEvent>((evt) => {
            #if UNITY_STANDALONE
                Application.Quit();
            #endif
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #endif
        });   
    }

    public void FilenameConfirm(ClickEvent evt) {    
        string value = UI.System.Q<TextField>("Filename").value;
        if (value.Length > 0) {
            DataController.currentFileName = value;
            State.SaveState(value);
            GetComponent<ModeController>().DeactivateByName("FilenameModal");
        }
        else {
            UI.SetHelpText("Filename cannot be empty.", HelpType.Error);
        }
    }

    public void FilenameCancel(ClickEvent evt) {    
        GetComponent<ModeController>().DeactivateByName("FilenameModal");
    }

    public void InitializeFileList() {
        DirectoryInfo info = new DirectoryInfo(Application.persistentDataPath + "/maps/");
        FileInfo[] fileInfo = info.GetFiles();
        List<string> items = new List<string>();
        for (int i = 0; i < fileInfo.Length; i++) {
            items.Add(fileInfo[i].Name);
        }        

        // The "makeItem" function is called when the
        // ListView needs more items to render.
        Func<VisualElement> makeItem = () => new Label();

        // As the user scrolls through the list, the ListView object
        // recycles elements created by the "makeItem" function,
        // and invoke the "bindItem" callback to associate
        // the element with the matching data item (specified as an index in the list).
        Action<VisualElement, int> bindItem = (e, i) => (e as Label).text = items[i];

        // Provide the list view with an explict height for every row
        // so it can calculate how many items to actually display
        const int itemHeight = 16;

        // var listView = new ListView(items, itemHeight, makeItem, bindItem);

        // listView.selectionType = SelectionType.Multiple;

        // listView.onItemsChosen += objects => Debug.Log(objects);
        // listView.onSelectionChange += objects => Debug.Log(objects);

        // listView.style.flexGrow = 1.0f;

        ListView lv = UI.System.Q("FileList") as ListView;
        lv.makeItem = makeItem;
        lv.bindItem = bindItem;
        lv.fixedItemHeight = itemHeight;
        lv.itemsSource = items;
        lv.itemsChosen += OnItemsChosen;
        lv.selectionChanged += OnSelectionChanged;
    }

    public void OnItemsChosen(IEnumerable<object> objects)
    {
        foreach (string s in objects) {
            currentFileName = s;
            State.LoadState(s);
            GetComponent<ModeController>().DeactivateByName("LoadFileModal");
            return;
        }
    }

    // Custom event handler for selectionChanged
    public void OnSelectionChanged(IEnumerable<object> objects)
    {
    }

}

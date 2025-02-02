using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using TMPro;

public class TerrainController : MonoBehaviour
{
    private List<Label> identifierLabels = new List<Label>();

    public static bool Indicators = false;
    public static bool Editing = false;

    public static bool ReorgNeeded = false;

    // void Start() {
    //     // registerCallbacks();
    //     // disableIndicators();
    //     // InitializeTerrain(8, 8, 1);
    // }

    // private void editTerrainSelect(ChangeEvent<string> evt) {
    //     resetConditionalElements();
    //     switch (evt.newValue) {
    //         case "EDIT TERRAIN":
    //             showConditionalElement("EditTerrainOptions");
    //             break;
    //         case "EDIT BLOCK":
    //             showConditionalElement("EditBlockOptions");
    //             break;
    //         case "MARK BLOCK":
    //             showConditionalElement("MarkBlockOptions");
    //             break;
    //         case "APPEARANCE":
    //             showConditionalElement("AppearanceOptions");
    //             break;
    //     }
    // }

    void LateUpdate() {
        if (ReorgNeeded) {
            Reorg();
            ReorgNeeded = false;
        }
    }

    private void resetConditionalElements() {
        UI.System.Q("EditTerrainOptions").style.display = DisplayStyle.None;
        UI.System.Q("EditBlockOptions").style.display = DisplayStyle.None;
        UI.System.Q("MarkBlockOptions").style.display = DisplayStyle.None;
        UI.System.Q("AppearanceOptions").style.display = DisplayStyle.None;
    }

    private void showConditionalElement(string name) {
        UI.System.Q(name).style.display = DisplayStyle.Flex;
    }

    private string getValue(RadioButtonGroup g, int i) {
        IEnumerable<string> choices = g.choices;
        string[] s = choices.ToArray();
        return s[i];
    }

    public void Edit(Block block) {
        List<string> ops = ToolsSidebar.GetOps();
        foreach(string op in ops) {
            switch (op) {
                case "AddBlock":
                    AddBlocks();
                    break;
                case "RemoveBlock":
                    RemoveBlocks();
                    break;
                case "CloneRow":
                    CloneRow();
                    break;
                case "CloneCol":
                    CloneColumn();
                    break;
                case "RemoveRow":
                    DeleteRow();
                    break;
                case "RemoveCol":
                    DeleteColumn();
                    break;
                case "Solid":
                    ChangeType(BlockType.Solid);
                    break;
                case "Slope":
                    ChangeType(BlockType.Slope);
                    break;
                case "Hidden":
                    ChangeType(BlockType.Spacer);
                    break;
                case "AddMark":
                    ChangeEffect(ToolsSidebar.MarkerEffect);
                    break;
                case "ClearMarks":
                    ChangeEffect("Clear");
                    break;
                case "CenterView":
                    CameraControl.GoToBlock(block);
                    break;
                case "Paintbrush":
                    PaintBlocks();
                    break;
                case "Eyedropper":
                    Eyedropper();
                    break;
            }
        }
    }

    public static void DestroyAllBlocks() {
        GameObject terrain = GameObject.Find("Terrain");
        terrain.name = "_Terrain";
        GameObject.DestroyImmediate(terrain);
        new GameObject("Terrain").transform.localScale = new Vector3(1, .5f, 1);
    }

    public static void InitializeTerrain(int length, int width, int height) {
        DestroyAllBlocks();
        GameObject map = GameObject.Find("Terrain");
        for (int y = 0; y < width; y++) {
            for (int x = 0; x < length; x++) {
                try {
                    string columnName = x + "," + y;
                    GameObject column = GameObject.Find(columnName);
                    if (column == null) {
                        column = new GameObject();
                        column.tag = "Column";
                        column.name = columnName;
                        column.transform.parent = map.transform;
                        column.transform.localPosition = new Vector3(x, 0, y);
                        column.transform.localScale = Vector3.one;
                        column.AddComponent<Column>().Set(x, y);
                    }
                    for (int z = 0; z < height; z++) {
                        GameObject block = Instantiate(Resources.Load("Prefabs/Block") as GameObject);
                        block.transform.parent = column.transform;
                        block.transform.localPosition = new Vector3(0, z, 0);
                        block.transform.localScale = Vector3.one;
                        if (z == 0) {
                            block.GetComponent<Block>().Destroyable = false;
                        }
                        Block b = block.GetComponent<Block>();
                        block.name = "block " + b.getX() + "," + b.getY() + "," + b.getZ();
                    }
                }
                catch (Exception e) {
                    Debug.LogError(e.Message);
                }
            }
        }
        ReorgNeeded = true;
    }

    public static void ResetTerrain() {
        // DestroyAllBlocks();
        // InitializeTerrain(8, 8, 3);
    }

    public static Vector3 Center() {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");
        float lowX = float.MaxValue;
        float highX = float.MinValue;
        float lowZ = float.MaxValue;
        float highZ = float.MinValue;
        for (int i = 0; i < blocks.Length; i++) {
            float x = blocks[i].transform.position.x;
            float z = blocks[i].transform.position.z;
            if (x < lowX) {
                lowX = x;
            }
            if (x > highX) {
                highX = x;
            }
            if (z < lowZ) {
                lowZ = z;
            }
            if (z > highZ) {
                highZ = z;
            }
        }
        float centerX = (highX - lowX) / 2f;
        float centerZ = (highZ - lowZ) / 2f;
        return new Vector3(lowX + centerX, 0, lowZ + centerZ);

    }

    public static Vector2 Size() {
        Vector2 size = Vector2.zero;
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");
        for (int i = 0; i < blocks.Length; i++) {
            size.x = Mathf.Max(size.x, blocks[i].GetComponent<Block>().getX()+1);
            size.y = Mathf.Max(size.y, blocks[i].GetComponent<Block>().getY()+1);
        }
        return size;
    }

    public static void AddBlocks() {        
        List<GameObject> selected = Block.GetAllSelected();
        List<Column> markedCols = new List<Column>();
        selected.ForEach(block => {
            Column column = block.transform.parent.GetComponent<Column>();
            if (!markedCols.Contains(column)) {
                markedCols.Add(column); 
                GameObject currentTop = TopBlock(column.gameObject);
                GameObject newblock = Instantiate(Resources.Load("Prefabs/Block") as GameObject);                 
                newblock.transform.parent = block.transform.parent;
                newblock.transform.localPosition = new Vector3(0, currentTop.transform.localPosition.y + 1, 0);
                newblock.transform.localScale = block.transform.localScale;
            }
        });
        ReorgNeeded = true;
    }

    public static void RemoveBlocks() {
        List<GameObject> selected = Block.GetAllSelected();
        selected.ForEach(block =>  {
            if (block.GetComponent<Block>().Destroyable) {
                GravityDrop(block.transform.parent.gameObject, block.transform.localPosition.y);
                GameObject.DestroyImmediate(block);
            }
            else {
                Toast.Add("Foundation blocks cannot be deleted (but can be hidden).", ToastType.Error);
            }
        });
        ReorgNeeded = true;
    }

    public static void RotateBlocks() {
        List<GameObject> selected = Block.GetAllSelected();
        selected.ForEach(block => {
            block.transform.Rotate(0, 90f, 0);
        });
    }

    public static void PaintBlocks() {
        List<GameObject> selected = Block.GetAllSelected();
        selected.ForEach(block => {
            block.GetComponent<Block>().Paint(Environment.Color5, Environment.Color6);
        });
    }

    public static void DepaintBlocks() {
        List<GameObject> selected = Block.GetAllSelected();
        selected.ForEach(block => {
            block.GetComponent<Block>().Depaint();
        });
    }

    public static void Eyedropper() {
        List<GameObject> selected = Block.GetAllSelected();
        selected.ForEach(block => {
            Color[] colors = block.GetComponent<Block>().SamplePaint();
            Environment.Color5 = colors[0];
            Environment.Color6 = colors[1];
            UI.System.Q("Color5").style.backgroundColor = colors[0];
            UI.System.Q("Color6").style.backgroundColor = colors[1];
        });
    }

    public static void CloneRow() {
        List<GameObject> selected = Block.GetAllSelected();
        GameObject[] columns = GameObject.FindGameObjectsWithTag("Column");
        selected.ForEach(selectedBlock => {
            for (int i = 0; i < columns.Length; i++) {
                GameObject column = columns[i];
                int x = column.GetComponent<Column>().X;
                int y = column.GetComponent<Column>().Y;
                if (x == selectedBlock.transform.parent.GetComponent<Column>().X) {
                    GameObject clone = GameObject.Instantiate(column);
                    clone.transform.parent = column.transform.parent;
                    clone.transform.localScale = Vector3.one;
                    clone.transform.localPosition += new Vector3(1, 0, 0);
                    clone.name = (x+1) + "," + y;
                    clone.GetComponent<Column>().Set(x + 1, y);
                }
                if (x > selectedBlock.transform.parent.GetComponent<Column>().X) {
                    column.transform.localPosition += new Vector3(1, 0, 0);
                    column.name = (x+1) + "," + y;
                    column.GetComponent<Column>().Set((x+1), y);
                }
            }
        });
        ReorgNeeded = true;
    }

    public static void DeleteRow() {
        List<GameObject> selected = Block.GetAllSelected();
        GameObject[] columns = GameObject.FindGameObjectsWithTag("Column");
        selected.ForEach(selectedBlock => {
            int selectedX = selectedBlock.transform.parent.GetComponent<Column>().X;
            for (int i = 0; i < columns.Length; i++) {
                GameObject column = columns[i];
                int x = column.GetComponent<Column>().X;
                int y = column.GetComponent<Column>().Y;
                if (x == selectedX) {
                    GameObject.Destroy(columns[i]);
                }
                if (x > selectedX) {
                    column.transform.localPosition -= new Vector3(1, 0, 0);
                    column.name = (x-1) + "," + y;
                    column.GetComponent<Column>().Set((x-1), y);
                }
            }
        });
        ReorgNeeded = true;
    }

    public static void CloneColumn() {
        List<GameObject> selected = Block.GetAllSelected();
        GameObject[] columns = GameObject.FindGameObjectsWithTag("Column");
        selected.ForEach(selectedBlock => {
            for (int i = 0; i < columns.Length; i++) {
                GameObject column = columns[i];
                int x = column.GetComponent<Column>().X;
                int y = column.GetComponent<Column>().Y;
                if (y == selectedBlock.transform.parent.GetComponent<Column>().Y) {
                    GameObject clone = GameObject.Instantiate(column);
                    clone.transform.parent = column.transform.parent;
                    clone.transform.localScale = Vector3.one;
                    clone.transform.localPosition += new Vector3(0, 0, 1);
                    clone.name = x + "," + (y+1);
                    clone.GetComponent<Column>().Set(x, y + 1);
                }
                if (y > selectedBlock.transform.parent.GetComponent<Column>().Y) {
                    column.transform.localPosition += new Vector3(0, 0, 1);
                    column.name = x + "," + (y+1);
                    column.GetComponent<Column>().Set(x, (y+1));
                }
            }
        });
        ReorgNeeded = true;
    }

    public static void DeleteColumn() {
        List<GameObject> selected = Block.GetAllSelected();
        GameObject[] columns = GameObject.FindGameObjectsWithTag("Column");
        selected.ForEach(selectedBlock => {
            int selectedY = selectedBlock.transform.parent.GetComponent<Column>().Y;
            for (int i = 0; i < columns.Length; i++) {
                GameObject column = columns[i];
                int x = column.GetComponent<Column>().X;
                int y = column.GetComponent<Column>().Y;
                if (y == selectedY) {
                    GameObject.Destroy(columns[i]);
                }
                if (y > selectedY) {
                    column.transform.localPosition -= new Vector3(0, 0, 1);
                    column.name = x + "," + (y-1);
                    column.GetComponent<Column>().Set(x, (y-1));
                }
            }
        });
        ReorgNeeded = true;
    }

    public static void ChangeType(BlockType type) {
        List<GameObject> selected = Block.GetAllSelected();
        selected.ForEach(block => {
            block.GetComponent<Block>().TypeChange(type);
            if (type == BlockType.Slope) {
                block.transform.Rotate(0, 90f, 0);
                // counter-rotate indicator
                block.transform.Find("Indicator").transform.eulerAngles = new Vector3(90, -90, 0);
            }
        });
    }

    public static void ChangeEffect(string effect) {
        List<GameObject> selected = Block.GetAllSelected();
        selected.ForEach((Action<GameObject>)(block => {
            block.GetComponent<Block>().EffectChange(effect);
        }));
    }

    private static GameObject TopBlock(GameObject column) {
        Block[] blocks = column.GetComponentsInChildren<Block>();
        GameObject top = null;
        float highest = float.MinValue;
        for (int i = 0; i < blocks.Length; i++)
        {
            float height = blocks[i].transform.localPosition.y;
            if (height > highest) {
                top = blocks[i].gameObject;
            }
        }
        return top;
    }

    private static void GravityDrop(GameObject column, float threshold) {
        Block[] blocks = column.GetComponentsInChildren<Block>();
        for (int i = 0; i < blocks.Length; i++) {
            if (blocks[i].transform.localPosition.y > threshold) {
                blocks[i].transform.localPosition -= new Vector3(0, 1, 0);
            }
        }
    }

    public static void Reorg() {
        UpdateIndicators();
        HideObscuredBlocks();
    }

    private static void UpdateIndicators() {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");
        for (int i = 0; i < blocks.Length; i++) {
            Block b = blocks[i].GetComponent<Block>();
            TextMeshPro tm = blocks[i].transform.Find("Indicator").GetComponent<TextMeshPro>();
            tm.text = b.getAlphaY() + (b.getX()+1);
            blocks[i].name = "block " + b.getX() + "," + b.getY() + "," + b.getZ();

        }         
    }

    private static void HideObscuredBlocks() {
        int count = 0;
        int hiding = 0;
        Vector2 size = Size();
        bool[,,] solids = new bool[(int)size.x,(int)size.y,30];
        GameObject[,,] blks = new GameObject[(int)size.x,(int)size.y,30];
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");
        for (int i = 0; i < blocks.Length; i++) {
            Block b = blocks[i].GetComponent<Block>();
            if (b.Type == BlockType.Solid) {
                count++;
                solids[b.getX(), b.getY(), b.getZ()] = true;
                blks[b.getX(), b.getY(), b.getZ()] = b.gameObject;
            }
        }
        for (int i = 0; i < blocks.Length; i++) {
            Block b = blocks[i].GetComponent<Block>();
            int x = b.getX();
            int y = b.getY();
            int z = b.getZ();
            try {
                if (
                    solids[x,y,z+1] && // obscured above
                    solids[x+1,y,z] && // obscured west
                    solids[x-1,y,z] && // obscured east
                    solids[x,y-1,z] && // obscured south
                    solids[x,y+1,z]    //obscured north
                ) {
                    hiding++;
                    hideBlock(b);
                }
                else {
                    showBlock(b);
                }
            }
            #pragma warning disable
            catch(Exception e) {
                // Exceptions are fine, it means there's no block because it's out of bounds
                showBlock(b);
            }
            #pragma warning restore

        }
    }

    private static void hideBlock(Block b) {
        b.GetComponent<MeshRenderer>().enabled = false;
        b.transform.Find("Indicator").GetComponent<TextMeshPro>().enabled = false;
    }

    private static void showBlock(Block b) {
        b.GetComponent<MeshRenderer>().enabled = true;
        b.transform.Find("Indicator").GetComponent<TextMeshPro>().enabled = true;
    }
}

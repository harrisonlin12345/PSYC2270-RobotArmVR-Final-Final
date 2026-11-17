
using UnityEngine;

public class SortingTableBuilder : MonoBehaviour
{
    [Header("Table Dimensions")]
    public float tableWidth = 1.4f;
    public float tableDepth = 0.8f;
    public float tableHeight = 0.8f;
    public float tabletopThickness = 0.06f;

    [Header("Zone Settings")]
    public float zoneThickness = 0.012f;
    public float zoneGap = 0.025f;

    [Header("Color Assignment")]
    public float assignmentDelay = 2f;
    public float triggerHeight = 0.15f;

    [Header("Materials")]
    public Color tableColor =
        new Color(0.3f, 0.3f, 0.3f);

    public Color assignerColor = Color.yellow;
    public Color redColor = Color.red;
    public Color blueColor = Color.blue;
    public Color greenColor = Color.green;

    private GameObject tableRoot;

    private void Start()
    {
        BuildTable();
    }

    private void BuildTable()
    {
        // Remove any previously generated table.
        if (tableRoot != null)
        {
            Destroy(tableRoot);
        }

        tableRoot = new GameObject("SortingTable");
        tableRoot.transform.SetParent(transform, false);

        // Main tabletop.
        CreateCube(
            "Tabletop",
            new Vector3(0f, tableHeight, 0f),
            new Vector3(
                tableWidth,
                tabletopThickness,
                tableDepth
            ),
            tableColor
        );

        // Four table legs.
        float legX = tableWidth / 2f - 0.08f;
        float legZ = tableDepth / 2f - 0.08f;

        CreateLeg(-legX, -legZ);
        CreateLeg(legX, -legZ);
        CreateLeg(-legX, legZ);
        CreateLeg(legX, legZ);

        // Calculate height of colored sections.
        float surfaceY =
            tableHeight +
            tabletopThickness / 2f +
            zoneThickness / 2f +
            0.001f;

        float margin = 0.05f;

        float usableWidth =
            tableWidth - 2f * margin;

        float usableDepth =
            tableDepth - 2f * margin;

        // ========================================
        // YELLOW COLOR ASSIGNER
        // ========================================

        float assignerDepth =
            usableDepth / 2f - zoneGap / 2f;

        float assignerZ =
            usableDepth / 4f + zoneGap / 4f;

        GameObject assigner = CreateCube(
            "ColorAssigner",
            new Vector3(
                0f,
                surfaceY,
                assignerZ
            ),
            new Vector3(
                usableWidth,
                zoneThickness,
                assignerDepth
            ),
            assignerColor
        );

        // Add invisible assignment trigger.
        CreateAssignmentTrigger(
            assigner,
            usableWidth,
            assignerDepth
        );

        // ========================================
        // RED, BLUE, GREEN SORTING SECTIONS
        // ========================================

        float sortingDepth =
            usableDepth / 2f - zoneGap / 2f;

        float sortingZ =
            -usableDepth / 4f - zoneGap / 4f;

        float sortingWidth =
            (usableWidth - 2f * zoneGap) / 3f;

        float spacing =
            sortingWidth + zoneGap;

        // Red section.
        GameObject redZone = CreateCube(
            "RedZone",
            new Vector3(
                -spacing,
                surfaceY,
                sortingZ
            ),
            new Vector3(
                sortingWidth,
                zoneThickness,
                sortingDepth
            ),
            redColor
        );

        // Blue section.
        GameObject blueZone = CreateCube(
            "BlueZone",
            new Vector3(
                0f,
                surfaceY,
                sortingZ
            ),
            new Vector3(
                sortingWidth,
                zoneThickness,
                sortingDepth
            ),
            blueColor
        );

        // Green section.
        GameObject greenZone = CreateCube(
            "GreenZone",
            new Vector3(
                spacing,
                surfaceY,
                sortingZ
            ),
            new Vector3(
                sortingWidth,
                zoneThickness,
                sortingDepth
            ),
            greenColor
        );

        // ========================================
        // SORTING TRIGGERS
        // ========================================

        CreateSortingTrigger(
            redZone,
            sortingWidth,
            sortingDepth,
            ColoredBlock.BlockColor.Red
        );

        CreateSortingTrigger(
            blueZone,
            sortingWidth,
            sortingDepth,
            ColoredBlock.BlockColor.Blue
        );

        CreateSortingTrigger(
            greenZone,
            sortingWidth,
            sortingDepth,
            ColoredBlock.BlockColor.Green
        );

        Debug.Log(
            "Sorting table created successfully."
        );
    }

    // ========================================
    // COLOR ASSIGNMENT TRIGGER
    // ========================================

    private void CreateAssignmentTrigger(
        GameObject assigner,
        float width,
        float depth)
    {
        GameObject trigger =
            new GameObject("AssignmentTrigger");

        trigger.transform.SetParent(
            tableRoot.transform,
            false
        );

        float surfaceTop =
            assigner.transform.localPosition.y +
            zoneThickness / 2f;

        trigger.transform.localPosition =
            new Vector3(
                assigner.transform.localPosition.x,
                surfaceTop + triggerHeight / 2f,
                assigner.transform.localPosition.z
            );

        trigger.transform.localScale =
            Vector3.one;

        BoxCollider collider =
            trigger.AddComponent<BoxCollider>();

        collider.isTrigger = true;

        collider.size = new Vector3(
            width,
            triggerHeight,
            depth
        );

        // Attach ColorAssigner script.
        ColorAssigner colorAssigner =
            trigger.AddComponent<ColorAssigner>();

        // Only assignmentDelay is required.
        // Colors are handled by ColoredBlock.cs.
        colorAssigner.assignmentDelay =
            assignmentDelay;
    }

    // ========================================
    // SORTING ZONE TRIGGERS
    // ========================================

    private void CreateSortingTrigger(
        GameObject zone,
        float width,
        float depth,
        ColoredBlock.BlockColor color)
    {
        GameObject trigger =
            new GameObject(
                zone.name + "Trigger"
            );

        trigger.transform.SetParent(
            tableRoot.transform,
            false
        );

        float surfaceTop =
            zone.transform.localPosition.y +
            zoneThickness / 2f;

        trigger.transform.localPosition =
            new Vector3(
                zone.transform.localPosition.x,
                surfaceTop + triggerHeight / 2f,
                zone.transform.localPosition.z
            );

        trigger.transform.localScale =
            Vector3.one;

        BoxCollider collider =
            trigger.AddComponent<BoxCollider>();

        collider.isTrigger = true;

        collider.size = new Vector3(
            width,
            triggerHeight,
            depth
        );

        // Attach sorting behavior.
        SortingZone sortingZone =
            trigger.AddComponent<SortingZone>();

        sortingZone.zoneColor = color;
    }

    // ========================================
    // TABLE LEGS
    // ========================================

    private void CreateLeg(float x, float z)
    {
        CreateCube(
            "TableLeg",
            new Vector3(
                x,
                tableHeight / 2f,
                z
            ),
            new Vector3(
                0.08f,
                tableHeight,
                0.08f
            ),
            tableColor
        );
    }

    // ========================================
    // CUBE CREATION
    // ========================================

    private GameObject CreateCube(
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        GameObject obj =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        obj.name = objectName;

        obj.transform.SetParent(
            tableRoot.transform,
            false
        );

        obj.transform.localPosition =
            localPosition;

        obj.transform.localScale =
            localScale;

        Renderer renderer =
            obj.GetComponent<Renderer>();

        Shader shader = Shader.Find(
            "Universal Render Pipeline/Lit"
        );

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material =
            new Material(shader);

        material.color = color;
        renderer.material = material;

        return obj;
    }
}

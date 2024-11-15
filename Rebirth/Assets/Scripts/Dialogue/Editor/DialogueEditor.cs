using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class DialogueEditorWindow : EditorWindow
{
    private Dictionary<string, Vector2> nodeScrollPositions = new Dictionary<string, Vector2>();
    private Dictionary<string, bool> nodeFoldouts = new Dictionary<string, bool>();
    private const float MAX_NODE_HEIGHT = 400f;
    private const float MIN_NODE_HEIGHT = 100f;
    private DialogueDataSO currentDialogueData;
    private Vector2 scrollPosition;
    private Dictionary<string, Rect> nodePositions = new Dictionary<string, Rect>();
    private const float NODE_WIDTH = 300f;
    private const float NODE_HEIGHT = 100f;
    private const float SPACING = 20f;
    private Rect canvasRect;
    private const float CANVAS_MARGIN = 1000f;
    private Vector2 minCanvasPosition = Vector2.zero;
    private Vector2 maxCanvasPosition = Vector2.zero;

    private const float DEFAULT_CANVAS_WIDTH = NODE_WIDTH * 10; // Default width for 10 nodes
    private const float DEFAULT_CANVAS_HEIGHT = NODE_HEIGHT * 5; // Default height for 5 nodes
    private const float CANVAS_PADDING = 200f; // Smaller padding instead of large margin
     private DialogueNode nodePendingDeletion;


    [MenuItem("Window/Dialogue Editor")]
    public static void ShowWindow()
    {
        GetWindow<DialogueEditorWindow>("Dialogue Editor");
    }

    private void OnEnable()
    {
        // Add these lines to ensure proper initialization
        if (currentDialogueData != null)
        {
            InitializeNodeData();
        }
        wantsMouseMove = true;
        UpdateCanvasSize();
    }

    private void InitializeNodeData()
    {
        if (currentDialogueData?.dialogueNodes == null) return;

        foreach (var node in currentDialogueData.dialogueNodes)
        {
            // Initialize positions if not already set
            if (!nodePositions.ContainsKey(node.dialogueID))
            {
                int index = currentDialogueData.dialogueNodes.IndexOf(node);
                int row = index / 5;
                int col = index % 5;
                
                nodePositions[node.dialogueID] = new Rect(
                    CANVAS_PADDING + (col * (NODE_WIDTH + SPACING)),
                    CANVAS_PADDING + (row * (NODE_HEIGHT + SPACING)),
                    NODE_WIDTH,
                    NODE_HEIGHT
                );
            }

            // Initialize scroll positions
            if (!nodeScrollPositions.ContainsKey(node.dialogueID))
            {
                nodeScrollPositions[node.dialogueID] = Vector2.zero;
            }
            if (!nodeScrollPositions.ContainsKey(node.dialogueID + "_conditions"))
            {
                nodeScrollPositions[node.dialogueID + "_conditions"] = Vector2.zero;
            }
            if (!nodeScrollPositions.ContainsKey(node.dialogueID + "_choices"))
            {
                nodeScrollPositions[node.dialogueID + "_choices"] = Vector2.zero;
            }

            // Initialize foldouts
            if (!nodeFoldouts.ContainsKey(node.dialogueID))
            {
                nodeFoldouts[node.dialogueID] = true;
            }
            if (!nodeFoldouts.ContainsKey(node.dialogueID + "_conditions"))
            {
                nodeFoldouts[node.dialogueID + "_conditions"] = true;
            }
            if (!nodeFoldouts.ContainsKey(node.dialogueID + "_choices"))
            {
                nodeFoldouts[node.dialogueID + "_choices"] = true;
            }

            // Initialize talk node specific data
            if (node is DialogueTalkNode talkNode)
            {
                if (talkNode.nextNodeIDs == null)
                {
                    talkNode.nextNodeIDs = new List<ConditionStringPair>();
                }
            }
            // Initialize end node specific data
            else if (node is DialogueEndNode endNode)
            {
                if (endNode.choices == null)
                {
                    endNode.choices = new List<DialogueChoice>();
                }
            }
        }
    }

    private void UpdateCanvasSize()
    {
        if (currentDialogueData?.dialogueNodes == null || currentDialogueData.dialogueNodes.Count == 0)
        {
            // Set default canvas size when no nodes exist
            minCanvasPosition = Vector2.zero;
            maxCanvasPosition = new Vector2(DEFAULT_CANVAS_WIDTH, DEFAULT_CANVAS_HEIGHT);
            canvasRect = new Rect(0, 0, DEFAULT_CANVAS_WIDTH, DEFAULT_CANVAS_HEIGHT);
            return;
        }

        // Initialize with the first node's position
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        // Find actual bounds of all nodes
        foreach (var nodePos in nodePositions)
        {
            minX = Mathf.Min(minX, nodePos.Value.x);
            minY = Mathf.Min(minY, nodePos.Value.y);
            maxX = Mathf.Max(maxX, nodePos.Value.x + nodePos.Value.width);
            maxY = Mathf.Max(maxY, nodePos.Value.y + nodePos.Value.height);
        }

        // Add padding to the bounds
        minCanvasPosition = new Vector2(
            Mathf.Min(0, minX - CANVAS_PADDING),
            Mathf.Min(0, minY - CANVAS_PADDING)
        );

        maxCanvasPosition = new Vector2(
            Mathf.Max(DEFAULT_CANVAS_WIDTH, maxX + CANVAS_PADDING),
            Mathf.Max(DEFAULT_CANVAS_HEIGHT, maxY + CANVAS_PADDING)
        );

        canvasRect = new Rect(
            minCanvasPosition.x,
            minCanvasPosition.y,
            maxCanvasPosition.x - minCanvasPosition.x,
            maxCanvasPosition.y - minCanvasPosition.y
        );
    }

    private void OnGUI()
    {
        if (currentDialogueData == null)
        {
            DrawNoDataGUI();
            return;
        }

        // Add this check to reinitialize data when needed
        if (Event.current.type == EventType.Layout)
        {
            InitializeNodeData();
        }

        DrawToolbar();
        
        Rect editorRect = new Rect(0, EditorStyles.toolbar.fixedHeight, position.width, position.height - EditorStyles.toolbar.fixedHeight);
        
        scrollPosition = GUI.BeginScrollView(
            editorRect,
            scrollPosition,
            canvasRect,
            true,
            true
        );

        BeginWindows();
        DrawNodes();
        EndWindows();
        
        GUI.EndScrollView();

        if (nodePendingDeletion != null)
        {
            DeleteNode(nodePendingDeletion);
            nodePendingDeletion = null;
            Repaint();
        }

        UpdateCanvasSize();
    }

    private void InitializeNodePositions()
    {
        if (currentDialogueData?.dialogueNodes == null) return;

        for (int i = 0; i < currentDialogueData.dialogueNodes.Count; i++)
        {
            var node = currentDialogueData.dialogueNodes[i];
            if (!nodePositions.ContainsKey(node.dialogueID))
            {
                // Place nodes in a grid layout within the default canvas size
                int row = i / 5; // 5 nodes per row
                int col = i % 5;
                
                nodePositions[node.dialogueID] = new Rect(
                    CANVAS_PADDING + (col * (NODE_WIDTH + SPACING)),
                    CANVAS_PADDING + (row * (NODE_HEIGHT + SPACING)),
                    NODE_WIDTH,
                    NODE_HEIGHT
                );
            }
        }
        UpdateCanvasSize();
    }
    private void DrawNoDataGUI()
    {
        EditorGUILayout.HelpBox("Please select a DialogueDataSO asset", MessageType.Info);
        
        if (GUILayout.Button("Create New Dialogue Data"))
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Dialogue Data",
                "NewDialogueData",
                "asset",
                "Please enter a file name to save the dialogue data"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                DialogueDataSO asset = CreateInstance<DialogueDataSO>();
                asset.dialogueNodes = new List<DialogueNode>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                currentDialogueData = asset;
            }
        }

        currentDialogueData = EditorGUILayout.ObjectField(
            "Dialogue Data",
            currentDialogueData,
            typeof(DialogueDataSO),
            false
        ) as DialogueDataSO;
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("Add Talk Node", EditorStyles.toolbarButton))
        {
            AddNode(DialogueNodeType.Talk);
        }
        
        if (GUILayout.Button("Add End Node", EditorStyles.toolbarButton))
        {
            AddNode(DialogueNodeType.End);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    private void AddNode(DialogueNodeType type)
    {
        DialogueNode newNode;
        
        if (type == DialogueNodeType.Talk)
        {
            var talkNode = CreateInstance<DialogueTalkNode>();
            talkNode.nextNodeIDs = new List<ConditionStringPair>();
            newNode = talkNode;
        }
        else
        {
            var endNode = CreateInstance<DialogueEndNode>();
            endNode.choices = new List<DialogueChoice>();
            newNode = endNode;
        }

        newNode.dialogueID = "Node_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        newNode.dialogueText = "New dialogue text";

        AssetDatabase.AddObjectToAsset(newNode, currentDialogueData);
        
        if (currentDialogueData.dialogueNodes == null)
            currentDialogueData.dialogueNodes = new List<DialogueNode>();
            
        currentDialogueData.dialogueNodes.Add(newNode);

        // Get mouse position and adjust for scroll
        Vector2 mousePosition = Event.current.mousePosition;
        mousePosition.x += scrollPosition.x;
        mousePosition.y += scrollPosition.y;

        // Place the node at mouse position + 100 units down
        nodePositions[newNode.dialogueID] = new Rect(
            mousePosition.x - (NODE_WIDTH / 2),
            mousePosition.y + 100, // 마우스 위치에서 100 아래로
            NODE_WIDTH,
            NODE_HEIGHT
        );
        
        EditorUtility.SetDirty(currentDialogueData);
        AssetDatabase.SaveAssets();
    }

    private void DrawNodes()
    {
        if (currentDialogueData.dialogueNodes == null) return;

        for (int i = 0; i < currentDialogueData.dialogueNodes.Count; i++)
        {
            DialogueNode node = currentDialogueData.dialogueNodes[i];
            if (!nodePositions.ContainsKey(node.dialogueID))
            {
                nodePositions[node.dialogueID] = new Rect(SPACING + (i * (NODE_WIDTH + SPACING)), SPACING, NODE_WIDTH, NODE_HEIGHT);
            }

            Rect nodeRect = nodePositions[node.dialogueID];
            nodeRect = GUI.Window(i, nodeRect, (id) => DrawNodeWindow(node), $"Node: {node.dialogueID}");
            nodePositions[node.dialogueID] = nodeRect;
        }
    }

    private void DrawNodeWindow(DialogueNode node)
    {
        string originalID = node.dialogueID;
    
    // Store the current position before any changes
    Rect currentPosition = nodePositions[originalID];
    
    // Initialize dictionaries if needed
    if (!nodeScrollPositions.ContainsKey(originalID))
    {
        nodeScrollPositions[originalID] = Vector2.zero;
    }
    if (!nodeFoldouts.ContainsKey(originalID))
    {
        nodeFoldouts[originalID] = true;
    }

    EditorGUI.BeginChangeCheck();

    // Header section with ID and Delete button
    EditorGUILayout.BeginHorizontal();
    string newID = EditorGUILayout.TextField("ID", originalID);
    
    bool deletePressed = false;
    if (GUILayout.Button("Delete Node", GUILayout.Width(80)))
    {
        deletePressed = true;
    }
    EditorGUILayout.EndHorizontal();

    // Handle ID changes with proper position preservation
    if (newID != originalID)
    {
        // Check if the new ID is unique
        if (!nodePositions.ContainsKey(newID))
        {
            // Update all references to this node in other nodes
            foreach (var dialogueNode in currentDialogueData.dialogueNodes)
            {
                if (dialogueNode is DialogueTalkNode talkNode)
                {
                    // Update nextNodeIDs
                    for (int i = 0; i < talkNode.nextNodeIDs.Count; i++)
                    {
                        if (talkNode.nextNodeIDs[i].nodeID == originalID)
                        {
                            talkNode.nextNodeIDs[i] = new ConditionStringPair(
                                talkNode.nextNodeIDs[i].condition,
                                newID
                            );
                        }
                    }
                    
                    // Update defaultNextNodeID
                    if (talkNode.defaultNextNodeID == originalID)
                    {
                        talkNode.defaultNextNodeID = newID;
                    }
                }
            }

            // Update node positions with the exact same position
            nodePositions.Add(newID, currentPosition);
            nodePositions.Remove(originalID);
            
            // Update scroll positions
            if (nodeScrollPositions.ContainsKey(originalID))
            {
                nodeScrollPositions.Add(newID, nodeScrollPositions[originalID]);
                nodeScrollPositions.Remove(originalID);
            }
            
            // Update foldouts
            if (nodeFoldouts.ContainsKey(originalID))
            {
                nodeFoldouts.Add(newID, nodeFoldouts[originalID]);
                nodeFoldouts.Remove(originalID);
            }

            // Additional dictionary updates for conditions/choices
            if (nodeScrollPositions.ContainsKey(originalID + "_conditions"))
            {
                nodeScrollPositions.Add(newID + "_conditions", nodeScrollPositions[originalID + "_conditions"]);
                nodeScrollPositions.Remove(originalID + "_conditions");
            }
            if (nodeScrollPositions.ContainsKey(originalID + "_choices"))
            {
                nodeScrollPositions.Add(newID + "_choices", nodeScrollPositions[originalID + "_choices"]);
                nodeScrollPositions.Remove(originalID + "_choices");
            }
            if (nodeFoldouts.ContainsKey(originalID + "_conditions"))
            {
                nodeFoldouts.Add(newID + "_conditions", nodeFoldouts[originalID + "_conditions"]);
                nodeFoldouts.Remove(originalID + "_conditions");
            }
            if (nodeFoldouts.ContainsKey(originalID + "_choices"))
            {
                nodeFoldouts.Add(newID + "_choices", nodeFoldouts[originalID + "_choices"]);
                nodeFoldouts.Remove(originalID + "_choices");
            }
            
            // Finally update the node's ID
            node.dialogueID = newID;
        }
        else
        {
            // If the new ID already exists, revert back to original ID
            EditorUtility.DisplayDialog("Invalid ID", 
                "A node with this ID already exists. Please choose a different ID.", "OK");
            newID = originalID;
        }
    }

    // Handle window dragging
    if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
    {
        Rect titleBarRect = new Rect(0, 0, NODE_WIDTH, EditorGUIUtility.singleLineHeight);
        if (titleBarRect.Contains(Event.current.mousePosition))
        {
            GUI.DragWindow();
        }
    }

        nodeScrollPositions[node.dialogueID] = EditorGUILayout.BeginScrollView(
        nodeScrollPositions[node.dialogueID],
        false,
        true,
        GUILayout.Height(nodeFoldouts[node.dialogueID] ? MAX_NODE_HEIGHT : MIN_NODE_HEIGHT)
    );

        // Main content foldout
        nodeFoldouts[node.dialogueID] = EditorGUILayout.Foldout(
            nodeFoldouts[node.dialogueID], 
            "Dialogue Content",
            true
        );

        if (nodeFoldouts[node.dialogueID])
        {
            // Dialogue text area
            EditorGUILayout.LabelField("Dialogue Text");
            node.dialogueText = EditorGUILayout.TextArea(
                node.dialogueText, 
                GUILayout.Height(60)
            );
            
            EditorGUILayout.Space();

            // Draw specific node type content
            if (node is DialogueTalkNode talkNode)
            {
                DrawTalkNode(talkNode);
            }
            else if (node is DialogueEndNode endNode)
            {
                DrawEndNode(endNode);
            }

            // Update node size based on content
            nodePositions[node.dialogueID] = new Rect(
                nodePositions[node.dialogueID].position,
                new Vector2(NODE_WIDTH, CalculateNodeHeight(node))
            );
        }

        EditorGUILayout.EndScrollView();

        // Handle changes
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(currentDialogueData);
        }

        // Update node height if needed
        float newHeight = CalculateNodeHeight(node);
    if (Mathf.Abs(nodePositions[node.dialogueID].height - newHeight) > 1f)
    {
        nodePositions[node.dialogueID] = new Rect(
            nodePositions[node.dialogueID].x,
            nodePositions[node.dialogueID].y,
            NODE_WIDTH,
            newHeight
        );
        Repaint();
    }

        // Enable window dragging
        GUI.DragWindow();

        // Handle deletion after all GUI rendering
        if (deletePressed)
        {
            if (EditorUtility.DisplayDialog("Delete Node", 
                "Are you sure you want to delete this node?", "Yes", "No"))
            {
                nodePendingDeletion = node;
            }
        }
    }

    private float CalculateNodeHeight(DialogueNode node)
    {
        if (!nodeFoldouts.ContainsKey(node.dialogueID)) return MIN_NODE_HEIGHT;
        if (!nodeFoldouts[node.dialogueID]) return MIN_NODE_HEIGHT;

        float height = MIN_NODE_HEIGHT;

        // Base content height
        height += EditorGUIUtility.singleLineHeight * 2; // ID field + foldout
        height += 60; // Text area height
        height += EditorGUIUtility.standardVerticalSpacing * 3; // Spacing

        // Node-specific content height
        if (node is DialogueTalkNode)
        {
            height += EditorGUIUtility.singleLineHeight * 2; // Default Next Node field + label
            
            if (nodeFoldouts.ContainsKey(node.dialogueID + "_conditions") && 
                nodeFoldouts[node.dialogueID + "_conditions"])
            {
                height += EditorGUIUtility.singleLineHeight; // Conditions label
                height += 150f; // Fixed scroll view height
                height += EditorGUIUtility.singleLineHeight * 2; // Add button + spacing
            }
        }
        else if (node is DialogueEndNode && 
                nodeFoldouts.ContainsKey(node.dialogueID + "_choices") && 
                nodeFoldouts[node.dialogueID + "_choices"])
        {
            height += EditorGUIUtility.singleLineHeight; // Choices label
            height += 150f; // Fixed scroll view height
            height += EditorGUIUtility.singleLineHeight * 2; // Add button + spacing
        }

        return Mathf.Clamp(height, MIN_NODE_HEIGHT, MAX_NODE_HEIGHT);
    }

    
    private void DrawTalkNode(DialogueTalkNode node)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Default Next Node", EditorStyles.boldLabel);
        
        // Add the defaultNextNodeID field
        node.defaultNextNodeID = EditorGUILayout.TextField("Default Next Node ID", node.defaultNextNodeID);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Conditions and Next Nodes", EditorStyles.boldLabel);

        if (!nodeFoldouts.ContainsKey(node.dialogueID + "_conditions"))
        {
            nodeFoldouts[node.dialogueID + "_conditions"] = true;
        }

        nodeFoldouts[node.dialogueID + "_conditions"] = EditorGUILayout.Foldout(
            nodeFoldouts[node.dialogueID + "_conditions"], 
            "Conditions",
            true
        );

        if (nodeFoldouts[node.dialogueID + "_conditions"])
        {
            // Scrollable area for conditions
            nodeScrollPositions[node.dialogueID + "_conditions"] = EditorGUILayout.BeginScrollView(
                nodeScrollPositions.ContainsKey(node.dialogueID + "_conditions") ? 
                nodeScrollPositions[node.dialogueID + "_conditions"] : 
                Vector2.zero,
                GUILayout.Height(Mathf.Max(150, node.nextNodeIDs.Count * 50 + 50))
            );

            if (node.nextNodeIDs == null)
                node.nextNodeIDs = new List<ConditionStringPair>();

            for (int i = 0; i < node.nextNodeIDs.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                
                var condition = node.nextNodeIDs[i].condition;
                var newCondition = EditorGUILayout.ObjectField(
                    condition,
                    typeof(Condition),
                    false
                ) as Condition;

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    DeleteCondition(node, i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                if (condition != null)
                {
                    EditorGUI.indentLevel++;
                    DrawConditionParameters(condition);
                    EditorGUI.indentLevel--;
                }

                string nextNodeId = node.nextNodeIDs[i].nodeID;
                nextNodeId = EditorGUILayout.TextField("Next Node ID", nextNodeId);

                node.nextNodeIDs[i] = new ConditionStringPair(newCondition, nextNodeId);

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Condition"))
            {
                CreateNewCondition(node);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    
    private void DrawEndNode(DialogueEndNode node)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel);

        if (!nodeFoldouts.ContainsKey(node.dialogueID + "_choices"))
        {
            nodeFoldouts[node.dialogueID + "_choices"] = true;
        }

        nodeFoldouts[node.dialogueID + "_choices"] = EditorGUILayout.Foldout(
            nodeFoldouts[node.dialogueID + "_choices"], 
            "Choices",
            true
        );

        if (nodeFoldouts[node.dialogueID + "_choices"])
        {
            // 스크롤 뷰의 높이를 고정
            float scrollViewHeight = 150f;  // TalkNode의 condition 영역과 비슷한 높이로 설정

            // 스크롤 위치 초기화
            if (!nodeScrollPositions.ContainsKey(node.dialogueID + "_choices"))
            {
                nodeScrollPositions[node.dialogueID + "_choices"] = Vector2.zero;
            }

            // 고정된 높이의 스크롤 뷰 시작
            nodeScrollPositions[node.dialogueID + "_choices"] = EditorGUILayout.BeginScrollView(
                nodeScrollPositions[node.dialogueID + "_choices"],
                false, 
                true,
                GUILayout.Height(scrollViewHeight)
            );

            if (node.choices == null)
                node.choices = new List<DialogueChoice>();

            for (int i = 0; i < node.choices.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                node.choices[i].choiceText = EditorGUILayout.TextField("Choice Text", node.choices[i].choiceText);
                node.choices[i].eventType = (DialogueEventType)EditorGUILayout.EnumPopup("Event Type", node.choices[i].eventType);
                node.choices[i].eventParameter = EditorGUILayout.TextField("Event Parameter", node.choices[i].eventParameter);
                node.choices[i].eventObjectParameter = EditorGUILayout.ObjectField(
                    "Event Object",
                    node.choices[i].eventObjectParameter,
                    typeof(UnityEngine.Object),
                    false
                );

                if (GUILayout.Button("Remove Choice"))
                {
                    node.choices.RemoveAt(i);
                    i--; // Adjust index after removal
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Add Choice"))
            {
                node.choices.Add(new DialogueChoice());
            }
        }
    }


    private void CreateNewCondition(DialogueTalkNode node)
    {
        // Create condition menu
        GenericMenu menu = new GenericMenu();
        
        // Get all condition types
        var conditionTypes = typeof(Condition).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Condition)));

        foreach (var type in conditionTypes)
        {
            var conditionNameAttr = type.GetCustomAttributes(typeof(ConditionNameAttribute), false)
                .FirstOrDefault() as ConditionNameAttribute;
                
            string menuName = conditionNameAttr != null ? conditionNameAttr.DisplayName : type.Name;
            
            menu.AddItem(new GUIContent(menuName), false, () => {
                var condition = CreateInstance(type) as Condition;
                AssetDatabase.AddObjectToAsset(condition, currentDialogueData);
                
                node.nextNodeIDs.Add(new ConditionStringPair(condition, ""));
                
                EditorUtility.SetDirty(currentDialogueData);
                AssetDatabase.SaveAssets();
            });
        }
        
        menu.ShowAsContext();
    }

    private void DrawConditionParameters(Condition condition)
    {
        var type = condition.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(int))
            {
                int value = (int)field.GetValue(condition);
                value = EditorGUILayout.IntField(field.Name, value);
                field.SetValue(condition, value);
            }
            else if (field.FieldType == typeof(bool))
            {
                bool value = (bool)field.GetValue(condition);
                value = EditorGUILayout.Toggle(field.Name, value);
                field.SetValue(condition, value);
            }
            else if (field.FieldType == typeof(string))
            {
                string value = (string)field.GetValue(condition);
                value = EditorGUILayout.TextField(field.Name, value);
                field.SetValue(condition, value);
            }
            else if (field.FieldType.IsEnum)  // Add support for enums
            {
                System.Enum value = (System.Enum)field.GetValue(condition);
                value = EditorGUILayout.EnumPopup(field.Name, value);
                field.SetValue(condition, value);
            }
            else if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                UnityEngine.Object value = (UnityEngine.Object)field.GetValue(condition);
                value = EditorGUILayout.ObjectField(field.Name, value, field.FieldType, false);
                field.SetValue(condition, value);
            }
        }
    }

    private void DeleteNode(DialogueNode node)
    {
        // Check if node exists in the dialogue data
        if (!currentDialogueData.dialogueNodes.Contains(node)) return;

        // Remove all conditions associated with the node if it's a talk node
        if (node is DialogueTalkNode talkNode)
        {
            foreach (var pair in talkNode.nextNodeIDs)
            {
                if (pair.condition != null)
                {
                    AssetDatabase.RemoveObjectFromAsset(pair.condition);
                }
            }
        }

        // Clean up all dictionary entries
        nodePositions.Remove(node.dialogueID);
        nodeScrollPositions.Remove(node.dialogueID);
        nodeFoldouts.Remove(node.dialogueID);
        
        // Additional cleanup for condition/choice foldouts
        nodeScrollPositions.Remove(node.dialogueID + "_conditions");
        nodeScrollPositions.Remove(node.dialogueID + "_choices");
        nodeFoldouts.Remove(node.dialogueID + "_conditions");
        nodeFoldouts.Remove(node.dialogueID + "_choices");

        // Remove from dialogue data and asset database
        currentDialogueData.dialogueNodes.Remove(node);
        AssetDatabase.RemoveObjectFromAsset(node);
        EditorUtility.SetDirty(currentDialogueData);
        AssetDatabase.SaveAssets();
        
        // Update the canvas size
        UpdateCanvasSize();
    }

    private void DeleteCondition(DialogueTalkNode node, int index)
    {
        var condition = node.nextNodeIDs[index].condition;
        if (condition != null)
        {
            AssetDatabase.RemoveObjectFromAsset(condition);
        }
        node.nextNodeIDs.RemoveAt(index);
        EditorUtility.SetDirty(currentDialogueData);
        AssetDatabase.SaveAssets();
    }
}

// Extension method for Rect
public static class RectExtensions
{
    public static Vector2 TopLeft(this Rect rect)
    {
        return new Vector2(rect.xMin, rect.yMin);
    }

    public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
    {
        Rect result = rect;
        result.x -= pivotPoint.x;
        result.y -= pivotPoint.y;
        result.xMin *= scale;
        result.xMax *= scale;
        result.yMin *= scale;
        result.yMax *= scale;
        result.x += pivotPoint.x;
        result.y += pivotPoint.y;
        return result;
    }
}

public class DialogueDataSOInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DialogueDataSO data = (DialogueDataSO)target;

        // Ensure conditions are properly saved as subassets
        if (data.dialogueNodes != null)
        {
            foreach (var node in data.dialogueNodes)
            {
                if (node is DialogueTalkNode talkNode && talkNode.nextNodeIDs != null)
                {
                    foreach (var pair in talkNode.nextNodeIDs)
                    {
                        if (pair.condition != null && AssetDatabase.GetAssetPath(pair.condition) != AssetDatabase.GetAssetPath(data))
                        {
                            AssetDatabase.AddObjectToAsset(pair.condition, data);
                            EditorUtility.SetDirty(data);
                        }
                    }
                }
            }
        }

        DrawDefaultInspector();
    }
}
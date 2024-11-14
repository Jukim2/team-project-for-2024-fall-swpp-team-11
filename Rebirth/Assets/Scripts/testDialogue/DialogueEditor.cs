using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

[CustomEditor(typeof(DialogueDataSO))]
public class DialogueDataSOEditor : Editor
{
    private DialogueDataSO dialogueData;
    private Vector2 scrollPosition;
    private bool[] nodeFoldouts;
    private bool[] choiceFoldouts;
    private DialogueCondition[] availableConditions;
    private Dictionary<DialogueCondition, bool> conditionFoldouts = new Dictionary<DialogueCondition, bool>();
    private Dictionary<DialogueCondition, List<int>> conditionGroups = new Dictionary<DialogueCondition, List<int>>();

    private void OnEnable()
    {
        dialogueData = (DialogueDataSO)target;
        InitializeFoldouts();
        InitializeConditions();
        InitializeConditionGroups();
    }

    private void InitializeConditions()
    {
        availableConditions = Enum.GetValues(typeof(DialogueCondition))
            .Cast<DialogueCondition>()
            .Where(c => c != DialogueCondition.MAX && c != DialogueCondition.None)
            .ToArray();

        foreach (var condition in availableConditions)
        {
            if (!conditionFoldouts.ContainsKey(condition))
            {
                conditionFoldouts[condition] = true;
            }
        }
    }

    private void InitializeConditionGroups()
    {
        conditionGroups.Clear();
        foreach (var condition in availableConditions)
        {
            conditionGroups[condition] = new List<int>();
        }

        for (int i = 0; i < dialogueData.dialogueNodes.Count; i++)
        {
            var node = dialogueData.dialogueNodes[i];
            if (node != null && conditionGroups.ContainsKey(node.dialogueCondition))
            {
                conditionGroups[node.dialogueCondition].Add(i);
            }
        }
    }

    private void InitializeFoldouts()
    {
        if (dialogueData.dialogueNodes == null)
            dialogueData.dialogueNodes = new List<DialogueNode>();

        nodeFoldouts = new bool[dialogueData.dialogueNodes.Count];
        choiceFoldouts = new bool[dialogueData.dialogueNodes.Count];
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Dialogue Data Editor", EditorStyles.boldLabel);

        DrawCreateNodeButtons();
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawNodesGroupedByCondition();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void DrawNodesGroupedByCondition()
    {
        foreach (var condition in availableConditions)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            conditionFoldouts[condition] = EditorGUILayout.Foldout(
                conditionFoldouts[condition], 
                $"{condition} Group ({conditionGroups[condition].Count} nodes)",
                true
            );

            if (conditionFoldouts[condition])
            {
                foreach (int nodeIndex in conditionGroups[condition].ToList())
                {
                    if (nodeIndex >= 0 && nodeIndex < dialogueData.dialogueNodes.Count)
                    {
                        DrawNodeInGroup(nodeIndex);
                    }
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }

    private void DrawNodeInGroup(int index)
    {
        DialogueNode node = dialogueData.dialogueNodes[index];
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        nodeFoldouts[index] = EditorGUILayout.Foldout(
            nodeFoldouts[index],
            $"{node.GetType().Name.Replace("Dialogue", "")}"
        );

        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            DeleteNode(index);
            InitializeConditionGroups();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (nodeFoldouts[index])
        {
            DrawNodeDetails(node, index);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawNodeDetails(DialogueNode node, int index)
    {
        EditorGUI.indentLevel++;

        node.dialogueID = EditorGUILayout.TextField("Dialogue ID", node.dialogueID);
        
        // 컨디션 변경 시 즉시 업데이트
        EditorGUI.BeginChangeCheck();
        DialogueCondition newCondition = (DialogueCondition)EditorGUILayout.EnumPopup("Condition", node.dialogueCondition);
        if (EditorGUI.EndChangeCheck() && newCondition != node.dialogueCondition)
        {
            Undo.RecordObject(dialogueData, "Change Node Condition");
            node.dialogueCondition = newCondition;
            InitializeConditionGroups();
            EditorUtility.SetDirty(dialogueData);
        }

        node.dialogueText = EditorGUILayout.TextArea(node.dialogueText, GUILayout.Height(60));

        if (node is DialogueTalkNode talkNode)
        {
            DrawTalkNodeDetails(talkNode);
        }
        else if (node is DialogueEndNode endNode)
        {
            DrawEndNodeDetails(endNode, index);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawCreateNodeButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Add Talk Node", GUILayout.Height(30)))
        {
            CreateNode<DialogueTalkNode>();
        }

        if (GUILayout.Button("Add End Node", GUILayout.Height(30)))
        {
            CreateNode<DialogueEndNode>();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void CreateNode<T>() where T : DialogueNode
    {
        T newNode = ScriptableObject.CreateInstance<T>();
        string nodeType = typeof(T).Name.Replace("Dialogue", "");
        newNode.dialogueID = $"{nodeType}_{dialogueData.dialogueNodes.Count}";
        newNode.dialogueCondition = DialogueCondition.Normal;
        
        if (newNode is DialogueTalkNode talkNode)
        {
            int conditionCount = Enum.GetValues(typeof(DialogueCondition)).Length;
            talkNode.nextNodeIDs = new string[conditionCount];
        }
        else if (newNode is DialogueEndNode endNode)
        {
            endNode.choices = new List<DialogueChoice>();
        }

        dialogueData.dialogueNodes.Add(newNode);
        AssetDatabase.AddObjectToAsset(newNode, dialogueData);
        
        InitializeFoldouts();
        InitializeConditionGroups();
        EditorUtility.SetDirty(target);
    }

    private void DrawTalkNodeDetails(DialogueTalkNode talkNode)
    {
        EditorGUILayout.LabelField("Next Node IDs", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        if (talkNode.nextNodeIDs == null || talkNode.nextNodeIDs.Length != Enum.GetValues(typeof(DialogueCondition)).Length)
        {
            Array.Resize(ref talkNode.nextNodeIDs, Enum.GetValues(typeof(DialogueCondition)).Length);
        }

        foreach (var condition in availableConditions)
        {
            if (condition != DialogueCondition.None)
            {
                int index = (int)condition;
                talkNode.nextNodeIDs[index] = EditorGUILayout.TextField(
                    condition.ToString(), 
                    talkNode.nextNodeIDs[index] ?? string.Empty
                );
            }
        }

        EditorGUI.indentLevel--;
    }

    private void DrawEndNodeDetails(DialogueEndNode endNode, int nodeIndex)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Choice"))
        {
            if (endNode.choices == null)
                endNode.choices = new List<DialogueChoice>();
            
            endNode.choices.Add(new DialogueChoice());
        }

        if (endNode.choices != null)
        {
            for (int i = 0; i < endNode.choices.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                choiceFoldouts[i] = EditorGUILayout.Foldout(choiceFoldouts[i], $"Choice {i}");
                
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    endNode.choices.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (choiceFoldouts[i])
                {
                    DialogueChoice choice = endNode.choices[i];
                    
                    EditorGUI.indentLevel++;
                    choice.choiceText = EditorGUILayout.TextField("Choice Text", choice.choiceText);
                    choice.eventType = (DialogueEventType)EditorGUILayout.EnumPopup("Event Type", choice.eventType);
                    choice.eventParameter = EditorGUILayout.TextField("Event Parameter", choice.eventParameter);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DeleteNode(int index)
    {
        DialogueNode nodeToDelete = dialogueData.dialogueNodes[index];
        dialogueData.dialogueNodes.RemoveAt(index);
        DestroyImmediate(nodeToDelete, true);
        InitializeFoldouts();
        EditorUtility.SetDirty(target);
    }
}
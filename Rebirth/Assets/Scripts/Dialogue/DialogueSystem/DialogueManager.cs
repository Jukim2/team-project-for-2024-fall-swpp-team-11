using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Button nextButton;
    public DialogueState speakerState;
    private DialogueDataSO currentDialogueData;
    private DialogueNode currentNode;

    // Modified event system to support multiple parameter types
    private Dictionary<DialogueEventType, List<Delegate>> eventHandlers 
        = new Dictionary<DialogueEventType, List<Delegate>>();

    public event Action OnDialogueStart;
    public event Action OnDialogueEnd;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        InitializeEventSystem();
        nextButton.onClick.AddListener(HandleNextButton);
    }

    private void InitializeEventSystem()
    {
        foreach (DialogueEventType eventType in Enum.GetValues(typeof(DialogueEventType)))
        {
            eventHandlers[eventType] = new List<Delegate>();
        }
    }

    // Generic subscription methods
    public void SubscribeToEvent<T>(DialogueEventType eventType, Action<T> handler)
    {
        if (!eventHandlers.ContainsKey(eventType))
        {
            eventHandlers[eventType] = new List<Delegate>();
        }
        if (!eventHandlers[eventType].Contains(handler))
        {
            Debug.Log($"Add handler {handler}");
            eventHandlers[eventType].Add(handler);
        }
    }

    // Keep the string version for backward compatibility
    public void SubscribeToEvent(DialogueEventType eventType, Action<string> handler)
    {
        SubscribeToEvent<string>(eventType, handler);
    }

    // Generic unsubscription methods
    public void UnsubscribeFromEvent<T>(DialogueEventType eventType, Action<T> handler)
    {
        if (eventHandlers.ContainsKey(eventType) && eventHandlers[eventType].Contains(handler))
        {
            eventHandlers[eventType].Remove(handler);
        }
    }

    // Keep the string version for backward compatibility
    public void UnsubscribeFromEvent(DialogueEventType eventType, Action<string> handler)
    {
        UnsubscribeFromEvent<string>(eventType, handler);
    }

    // Modified event trigger method to handle different parameter types
    private void TriggerEvent<T>(DialogueEventType eventType, T parameter)
{
    if (eventHandlers.ContainsKey(eventType))
    {
        foreach (var handler in eventHandlers[eventType])
        {
            if (handler is Action<T> typedHandler)
            {
                typedHandler.Invoke(parameter);
            }
        }
    }
}


    public void StartDialogue(DialogueDataSO dialogueData, DialogueState condition)
    {
        currentDialogueData = dialogueData;
        speakerState = condition;
        currentNode = currentDialogueData.dialogueNodes.Find(n => n.dialogueID == "Default");
        
        dialoguePanel.SetActive(true);
        OnDialogueStart?.Invoke();
        
        DisplayCurrentNode();
    }

    private void DisplayCurrentNode()
    {
        ClearButtons();
        dialogueText.text = currentNode.dialogueText;

        switch (currentNode.NodeType)
        {
            case DialogueNodeType.Talk:
                nextButton.gameObject.SetActive(true);
                break;
                
            case DialogueNodeType.End:
                nextButton.gameObject.SetActive(false);
                DisplayChoices(currentNode as DialogueEndNode);
                break;
        }
    }

    private void HandleNextButton()
    {
        if (currentNode is DialogueTalkNode talkNode)
        {
            string nextNodeId = talkNode.GetNextNodeID();
            currentNode = currentDialogueData.dialogueNodes.Find(n => n.dialogueID == nextNodeId);
            DisplayCurrentNode();
        }
    }

    private void DisplayChoices(DialogueEndNode endNode)
    {
        foreach (var choice in endNode.choices)
        {
            CreateChoiceButton(choice);
        }
    }

    private void CreateChoiceButton(DialogueChoice choice)
    {
        Button newButton = Instantiate(buttonPrefab, buttonContainer);
        TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
        if (choice.choiceText == null)
        {
            Debug.Log("no text");
            return;
        }
        
        buttonText.text = choice.choiceText;

        newButton.onClick.AddListener(() => {
            HandleChoice(choice);
            EndDialogue();
        });
    }

    private void HandleChoice(DialogueChoice choice)
    {
        // Handle both string and object parameters
        if (choice.eventObjectParameter != null)
        {
            // Try to handle the specific type of the object parameter
            switch (choice.eventObjectParameter)
            {
                case Item obj:
                    TriggerEvent(choice.eventType, obj);
                    break;

                case ItemData obj:
                    TriggerEvent(choice.eventType, obj);
                    break;
                // Add more cases for other types as needed
                default:
                    TriggerEvent(choice.eventType, choice.eventObjectParameter);
                    break;
            }
        }
        else
        {
            // Fallback to string parameter
            TriggerEvent(choice.eventType, choice.eventParameter);
        }
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        OnDialogueEnd?.Invoke();
        currentDialogueData = null;
        currentNode = null;
    }

    private void ClearButtons()
    {
        foreach (Transform child in buttonContainer)
        {
            if (child.gameObject != nextButton.gameObject)
                Destroy(child.gameObject);
        }
    }
}
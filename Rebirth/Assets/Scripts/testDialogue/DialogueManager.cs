using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Button nextButton;

    private DialogueDataSO currentDialogueData;
    private DialogueNode currentNode;
    private IDialogueEventHandler currentEventHandler;
    private DialogueCondition dialogueCondition;

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

        nextButton.onClick.AddListener(HandleNextButton);
    }

    public void StartDialogue(DialogueDataSO dialogueData, IDialogueEventHandler eventHandler, DialogueCondition condition)
    {
        currentDialogueData = dialogueData;
        currentEventHandler = eventHandler;
        dialogueCondition = condition;
        Debug.Log(((int)dialogueCondition).ToString());
        currentNode = currentDialogueData.dialogueNodes.Find(n => n.dialogueID == ((int)dialogueCondition).ToString());
        
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
            currentNode = currentDialogueData.dialogueNodes.
            Find(n => n.dialogueID == talkNode.nextNodeIDs[(int)dialogueCondition]);
            DisplayCurrentNode();
        }
    }

    private void DisplayChoices(DialogueEndNode endNode)
    {
        Debug.Log(endNode.choices.Count);
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
        currentEventHandler?.HandleDialogueEvent(choice.eventType, choice.eventParameter);
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        OnDialogueEnd?.Invoke();
        currentDialogueData = null;
        currentNode = null;
        currentEventHandler = null;
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

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/DialogueData")]
public class DialogueDataSO : ScriptableObject
{
    public List<DialogueNode> dialogueNodes;
}

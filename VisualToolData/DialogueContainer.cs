using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class DialogueContainer : ScriptableObject
{
    public List<DialogueNodeData> DialogueNodesData = new List<DialogueNodeData>();
    public List<NodeLinkData> NodeLinksData = new List<NodeLinkData>();
}

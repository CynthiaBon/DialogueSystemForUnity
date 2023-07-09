using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class BoxDialogueDataTemplate
{
    public ulong ID = 0;
    public ulong FirstSentenceID = 0;

    public BoxDialogueDataTemplate() { }

    public BoxDialogueDataTemplate(DialogueDataContainer data)
    {
        ID = IDEncoder.Encode(data.ID);
        FirstSentenceID = IDEncoder.Encode(data.FirstSentenceID);
    }

    public DialogueDataContainer ToDataContainer()
    {
        DialogueDataContainer data = new DialogueDataContainer(IDEncoder.Decode(ID), IDEncoder.Decode(FirstSentenceID));
        return data;
    }
}
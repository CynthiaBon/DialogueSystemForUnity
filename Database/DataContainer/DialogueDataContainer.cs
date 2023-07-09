using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueDataContainer
{
    public string ID = "";
    public string FirstSentenceID = "";

    public DialogueDataContainer() { }

    public DialogueDataContainer(string id, string firstSentenceId)
    {
        ID = id;
        FirstSentenceID = firstSentenceId;
    }

    public void AddToDatabase()
    {
        DatabaseManager.Database.AddDialogueData(this);
    }
}

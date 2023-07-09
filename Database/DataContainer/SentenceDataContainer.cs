using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SentenceDataContainer
{
    public string ID = "";
    public string Speaker = null;
    public string Text = null;
    public bool HasChoice = false;
    public List<string> ChoiceIDs;
    public List<EventMethodContainer> OnDialogueEvents = null;
    public List<EventMethodContainer> OnEndEvents = null;
    public string NextSentenceID = "";
    public List<QueryValueContainer> Query = null;

    public SentenceDataContainer()
    {
        ChoiceIDs = new List<string>();
        OnDialogueEvents = new List<EventMethodContainer>();
        OnEndEvents = new List<EventMethodContainer>();
        Query = new List<QueryValueContainer>();
    }

    public SentenceDataContainer(string id, string speaker, string text, bool hasChoice, List<string> choiceIds, List<EventMethodContainer> onDialogueEvents, List<EventMethodContainer> onEndEvents, string nextSentenceId, List<QueryValueContainer> query)
    {
        ID = id;
        Speaker = speaker;
        Text = text;
        HasChoice = hasChoice;
        ChoiceIDs = choiceIds;
        OnDialogueEvents = onDialogueEvents;
        OnEndEvents = onEndEvents;
        NextSentenceID = nextSentenceId;
        Query = query;
    }

    public void AddToDatabase()
    {
        DatabaseManager.Database.AddSentenceData(this);
    }
}

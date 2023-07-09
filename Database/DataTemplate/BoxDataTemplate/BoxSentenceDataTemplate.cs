using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[Serializable]
public class BoxSentenceDataTemplate
{
    public ulong ID = 0;
    public string Speaker = null;
    public string Text = null;
    public int HasChoice = 0;
    public string ChoiceIDs;
    public string OnDialogueEvents = null;
    public string OnEndEvents = null;
    public ulong NextSentenceID = 0;
    public Dictionary<string, object> Query = null;

    public BoxSentenceDataTemplate() { }

    public BoxSentenceDataTemplate(SentenceDataContainer data)
    {
        ID = IDEncoder.Encode(data.ID);
        Speaker = data.Speaker;
        Text = data.Text;
        HasChoice = data.HasChoice ? 1 : 0;
        OnDialogueEvents = ConcatEventMethod(data.OnDialogueEvents);
        OnEndEvents = ConcatEventMethod(data.OnEndEvents);
        ChoiceIDs = ConcatChoiceIds(data.ChoiceIDs);
        NextSentenceID = IDEncoder.Encode(data.NextSentenceID);
        Query = data.Query.ToDictionary(k => $"{k.Type}:{IDEncoder.Encode(k.Name).ToString()}", v => (object)v.Value);
    }

    private string ConcatEventMethod(List<EventMethodContainer> eventMethodContainers)
    {
        if (eventMethodContainers == null || eventMethodContainers.Count == 0)
            return null;

        string concatEventMethod = eventMethodContainers[0].MethodName + "|" + eventMethodContainers[0].Argument;
        for (int i = 1; i < eventMethodContainers.Count; i++)
        {
            concatEventMethod += " " + eventMethodContainers[i].MethodName + "|" + eventMethodContainers[i].Argument;
        }

        return concatEventMethod;
    }

    private string ConcatChoiceIds(List<string> choiceIds)
    {
        if (choiceIds == null || choiceIds.Count == 0)
            return null;

        string concatChoiceIds = IDEncoder.Encode(choiceIds[0]).ToString();
        for (int i = 1; i < choiceIds.Count; i++)
        {
            concatChoiceIds += "|" + IDEncoder.Encode(choiceIds[i]).ToString();
        }

        return concatChoiceIds;
    }

    public SentenceDataContainer ToDataContainer()
    {
        bool hasChoice = HasChoice == 1;

        List<string> choiceIDs = new List<string>();
        if (ChoiceIDs != null && ChoiceIDs != "")
        {
            string[] ids = ChoiceIDs.Split('|');
            for (int i = 0; i < ids.Length; i++)
            {
                choiceIDs.Add(IDEncoder.Decode(ulong.Parse(ids[i])));
            }
        }

        List<EventMethodContainer> onDialogueEvents = new List<EventMethodContainer>();
        if (OnDialogueEvents != null && OnDialogueEvents != "")
        {
            string[] dialogueEvents = OnDialogueEvents.Split(' ');
            for (int i = 0; i < dialogueEvents.Length; i++)
            {
                onDialogueEvents.Add(new EventMethodContainer(dialogueEvents[i]));
            }
        }

        List<EventMethodContainer> onEndEvents = new List<EventMethodContainer>();
        if (OnEndEvents != null && OnEndEvents != "")
        {
            string[] endEvents = OnEndEvents.Split(' ');
            for (int i = 0; i < endEvents.Length; i++)
            {
                onEndEvents.Add(new EventMethodContainer(endEvents[i]));
            }
        }

        List<QueryValueContainer> query = new List<QueryValueContainer>();
        if (Query != null && Query.Count != 0)
        {
            foreach (string key in Query.Keys)
            {
                string[] splitId = key.Split(':');
                ulong k = IDEncoder.Parse(splitId[1]);
                query.Add(new QueryValueContainer(splitId[0], IDEncoder.Decode(k), (int)Query[key]));
            }
        }

        return new SentenceDataContainer(IDEncoder.Decode(ID), Speaker, Text, hasChoice, choiceIDs, onDialogueEvents, onEndEvents, IDEncoder.Decode(NextSentenceID), query);
    }
}

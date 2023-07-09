using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sentence
{
    public Sentence(string speaker, string text, bool hasChoice, List<ulong> choiceIds, List<EventMethod> onDialogueEvents, List<EventMethod> onEndEvents, ulong nextSentenceId)
    {
        Speaker = speaker;
        Text = text;
        HasChoice = hasChoice;
        ChoiceIDs = choiceIds;
        OnDialogueEvents = onDialogueEvents;
        OnEndEvents = onEndEvents;
        NextSentenceID = nextSentenceId;
    }

    public string Speaker { get; private set; } = null;
    public string Text { get; private set; } = null;
    public bool HasChoice { get; private set; } = false;
    public List<ulong> ChoiceIDs { get; private set; } = null;
    public List<EventMethod> OnDialogueEvents { get; private set; } = null;
    public List<EventMethod> OnEndEvents { get; private set; } = null;
    public ulong NextSentenceID { get; private set; } = 0;
}

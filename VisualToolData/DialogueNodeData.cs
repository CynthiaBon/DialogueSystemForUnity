using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class DialogueNodeData
{
    public DialogueNodeData(ulong id, string title, string text, bool isChoice, Vector2 position)
    {
        ID = id;
        Title = title;
        Text = text;
        IsChoice = isChoice;
        Position = position;
    }

    public ulong ID = 0;
    public string Title = null;
    public string Text = null;
    public bool IsChoice = false;
    public Vector2 Position = Vector2.zero;
}

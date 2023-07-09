using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventMethod
{
    public EventMethod(Action<string, DialogueManager> method, string argument)
    {
        Method = method;
        Argument = argument;
    }

    public Action<string, DialogueManager> Method { get; private set; } = null;
    public string Argument { get; private set; } = null;
}

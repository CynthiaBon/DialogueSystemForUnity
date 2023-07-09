using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventMethodContainer
{
    public EventMethodContainer() { }

    public EventMethodContainer(string methodAndArgument)
    {
        string[] splitString = methodAndArgument.Split('|');
        MethodName = splitString[0];
        Argument = splitString[1];
    }

    public EventMethodContainer(string methodName, string argument)
    {
        MethodName = methodName;
        Argument = argument;
    }

    public string MethodName = "";
    public string Argument = "";
}

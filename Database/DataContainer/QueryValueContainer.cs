using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueryValueContainer
{
    public QueryValueContainer() { }

    public QueryValueContainer(string type, string name, int value)
    {
        Type = type;
        Name = name;
        Value = value;
    }

    public string Type = "";
    public string Name = "";
    public int Value = 0;

    public override string ToString()
    {
        return $"{Type}.{Name}";
    }
}

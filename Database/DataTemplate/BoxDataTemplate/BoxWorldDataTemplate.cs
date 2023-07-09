using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class BoxWorldDataTemplate
{
    public ulong ID = 0;
    public int Value = 0;

    public BoxWorldDataTemplate() { }

    public BoxWorldDataTemplate(WorldDataContainer data)
    {
        ID = IDEncoder.Encode(data.ID);
        Value = data.Value;
    }

    public BoxWorldDataTemplate(ulong id, int value)
    {
        ID = id;
        Value = value;
    }

    public WorldDataContainer ToDataContainer()
    {
        WorldDataContainer data = new WorldDataContainer(IDEncoder.Decode(ID), Value);
        return data;
    }
}

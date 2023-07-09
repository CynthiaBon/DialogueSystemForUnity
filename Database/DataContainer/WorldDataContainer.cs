using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldDataContainer
{
    public string ID = "";
    public int Value = 0;

    public WorldDataContainer() { }

    public WorldDataContainer(string id, int value)
    {
        ID = id;
        Value = value;
    }

    public void AddToDatabase()
    {
        DatabaseManager.Database.AddWorldData(this);
    }
}

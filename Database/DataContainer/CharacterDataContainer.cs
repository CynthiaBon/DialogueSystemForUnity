using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDataContainer
{
    public string CharacterID = "";
    public string ID = "";
    public int Value = 0;

    public CharacterDataContainer() { }

    public CharacterDataContainer(string characterId, string id, int value)
    {
        CharacterID = characterId;
        ID = id;
        Value = value;
    }

    public void AddToDatabase()
    {
        DatabaseManager.Database.AddCharacterData(this);
    }
}

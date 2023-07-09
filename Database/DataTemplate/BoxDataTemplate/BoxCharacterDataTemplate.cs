using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class BoxCharacterDataTemplate
{
    public ulong CharacterID = 0;
    public ulong ID = 0;
    public int Value = 0;

    public BoxCharacterDataTemplate() { }

    public BoxCharacterDataTemplate(CharacterDataContainer data)
    {
        CharacterID = IDEncoder.Encode(data.CharacterID);
        ID = IDEncoder.Encode(data.ID);
        Value = data.Value;
    }

    public CharacterDataContainer ToDataContainer()
    {
        CharacterDataContainer data = new CharacterDataContainer(IDEncoder.Decode(CharacterID), IDEncoder.Decode(ID), Value);
        return data;
    }
}

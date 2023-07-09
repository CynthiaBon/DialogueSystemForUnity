using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[Serializable]
public class BoxFilterDataTemplate
{
    public ulong CharacterID = 0;
    public ulong DialogueID = 0;
    public Dictionary<string, object> Query = null;

    public BoxFilterDataTemplate() { }

    public BoxFilterDataTemplate(FilterDataContainer data)
    {
        CharacterID = IDEncoder.Encode(data.CharacterID);
        DialogueID = IDEncoder.Encode(data.DialogueID);
        Query = data.Query.ToDictionary(k => k.Type + ":" + IDEncoder.Encode(k.Name).ToString(), v => (object)v.Value);
    }

    public FilterDataContainer ToDataContainer()
    {
        List<QueryValueContainer> query = new List<QueryValueContainer>();
        foreach (string key in Query.Keys)
        {
            string[] splitId = key.Split(':');
            ulong k = IDEncoder.Parse(splitId[1]);
            query.Add(new QueryValueContainer(splitId[0], IDEncoder.Decode(k), (int)Query[key]));
        }
        FilterDataContainer data = new FilterDataContainer(IDEncoder.Decode(CharacterID), IDEncoder.Decode(DialogueID), query);
        return data;
    }
}

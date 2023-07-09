using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FilterDataContainer
{
    public string CharacterID = "";
    public string DialogueID = "";
    public List<QueryValueContainer> Query = null;

    public FilterDataContainer()
    {
        Query = new List<QueryValueContainer>();
    }

    public FilterDataContainer(string characterId, string dialogueId, List<QueryValueContainer> query)
    {
        CharacterID = characterId;
        DialogueID = dialogueId;
        Query = query;
    }

    public void AddToDatabase()
    {
        DatabaseManager.Database.AddFilterData(this);
    }
}

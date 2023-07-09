using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDatabase
{
    public ulong GetFirstSentenceID(string characterName);
    public Sentence GetSentence(ulong id);
    public bool IsChoiceValid(ulong id, ulong characterId);

    public bool AddDialogueData(DialogueDataContainer dialogueData);
    public bool AddSentenceData(SentenceDataContainer sentenceData);
    public bool AddFilterData(FilterDataContainer filterData);
    public bool AddWorldData(WorldDataContainer worldData);
    public bool AddCharacterData(CharacterDataContainer characterData);

    public bool RemoveDialogueData(ulong id);
    public bool RemoveSentenceData(ulong id);
    public bool RemoveFilterData(ulong characterId, ulong dialogueId);
    public bool RemoveWorldData(ulong id);
    public bool RemoveCharacterData(ulong characterId, ulong id);

    public List<DialogueDataContainer> GetDialogueData();
    public List<SentenceDataContainer> GetSentenceData();
    public SentenceDataContainer GetSentenceData(ulong id);
    public List<FilterDataContainer> GetFilterData();
    public FilterDataContainer GetFilterData(ulong characterId, ulong id);
    public List<WorldDataContainer> GetWorldData();
    public List<CharacterDataContainer> GetCharacterData();

    public int GetWorldDataValue(ulong id);
    public int GetCharacterDataValue(ulong characterId, ulong id);

    public bool IsWorldQueryValid(WorldDataContainer worldData);
    public bool IsCharacterQueryValid(CharacterDataContainer characterData);
    public bool DoFilterExist(ulong characterId, ulong dialogueId);
    public bool DoSentenceExist(ulong id);
    public bool DoDialogueExist(ulong id);
    public bool DoWorldQueryExist(ulong id);
    public bool DoCharacterQueryExist(ulong characterId, ulong id);

    public void CloseDatabase();
}

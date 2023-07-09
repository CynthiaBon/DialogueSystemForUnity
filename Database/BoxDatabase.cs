using IBoxDB.LocalServer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.IO;

public class BoxDatabase : IDatabase
{
    private DB _database = null;
    private AutoBox _autoBox = null;
    private IBox _cube = null;

    private const string DIALOGUES_TABLE = "Dialogues";
    private const string SENTENCES_TABLE = "Sentences";
    private const string FILTERS_TABLE = "Filters";
    private const string WORLD_TABLE = "World";
    private const string CHARACTERS_TABLE = "Characters";

    public BoxDatabase()
    {
        InitializeDatabase();
    }

    public void InitializeDatabase()
    {
        string bddPath = Application.dataPath + "/Scripts/DialogueSystem/Database/Data";
        DB.Root(bddPath);

        if (Application.isPlaying && File.Exists(bddPath + "/db1.box"))
        {
            if (File.Exists(bddPath + "/db2.box"))
            {
                File.Delete(bddPath + "/db2.box");
                File.Delete(bddPath + "/db2.box.swp");
            }
            File.Copy(bddPath + "/db1.box", bddPath + "/db2.box");
            File.Copy(bddPath + "/db1.box.swp", bddPath + "/db2.box.swp");
            _database = new DB(2);
        }
        else
        {
            _database = new DB(1);
        }

        _database.GetConfig().EnsureTable<BoxDialogueDataTemplate>(DIALOGUES_TABLE, "ID");
        _database.GetConfig().EnsureTable<BoxSentenceDataTemplate>(SENTENCES_TABLE, "ID");
        _database.GetConfig().EnsureTable<BoxFilterDataTemplate>(FILTERS_TABLE, "CharacterID", "DialogueID");
        _database.GetConfig().EnsureTable<BoxWorldDataTemplate>(WORLD_TABLE, "ID");
        _database.GetConfig().EnsureTable<BoxCharacterDataTemplate>(CHARACTERS_TABLE, "CharacterID", "ID");
        _autoBox = _database.Open();
    }

    #region Sentence

    public Sentence GetSentence(ulong id)
    {
        BoxSentenceDataTemplate sentenceData = _autoBox.Select<BoxSentenceDataTemplate>($"from {SENTENCES_TABLE} where ID == ? limit 0, 1", id).FirstOrDefault();
        if (sentenceData == null)
            return null;

        List<ulong> choiceIds = GetChoiceIds(sentenceData.ChoiceIDs);
        List<EventMethod> onDialogueEvents = GetEventMethods(sentenceData.OnDialogueEvents);
        List<EventMethod> onEndEvents = GetEventMethods(sentenceData.OnEndEvents);
        List<QueryValueContainer> query = new List<QueryValueContainer>();

        Sentence sentence = new Sentence(sentenceData.Speaker, sentenceData.Text, sentenceData.HasChoice == 1, choiceIds, onDialogueEvents, onEndEvents, sentenceData.NextSentenceID);
        return sentence;
    }

    private List<ulong> GetChoiceIds(string choiceIdsString)
    {
        if (choiceIdsString == null || choiceIdsString == "")
            return null;

        List<ulong> ids = new List<ulong>();
        string[] idsString = choiceIdsString.Split('|');
        for (int i = 0; i < idsString.Length; i++)
        {
            ids.Add(ulong.Parse(idsString[i]));
        }

        return ids;
    }

    private List<EventMethod> GetEventMethods(string eventMethodsString)
    {
        if (eventMethodsString == null || eventMethodsString == "")
            return null;

        string[] splitEventMethods = eventMethodsString.Split(' ');

        List<EventMethod> eventMethods = new List<EventMethod>();
        for (int i = 0; i < splitEventMethods.Length; i++)
        {
            EventMethod eventMethod = GetMethod(splitEventMethods[i]);
            if (eventMethod != null)
                eventMethods.Add(eventMethod);
        }

        return eventMethods;
    }

    private EventMethod GetMethod(string methodAndArguments)
    {
        string[] splitMethodAndArguments = methodAndArguments.Split('|');
        Action<string, DialogueManager> method = (Action<string, DialogueManager>)Delegate.CreateDelegate(typeof(Action<string, DialogueManager>), null, typeof(DialogueManager).GetMethod(splitMethodAndArguments[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
        EventMethod eventMethod = new EventMethod(method, splitMethodAndArguments[1]);

        return eventMethod;
    }

    public bool IsChoiceValid(ulong id, ulong characterId)
    {
        Dictionary<string, object> stringQuery = _autoBox.Select<BoxSentenceDataTemplate>($"from {SENTENCES_TABLE} where ID == ? limit 0, 1", id).FirstOrDefault()?.Query;
        if (stringQuery == null)
            return true;

        Dictionary<ulong, int> query = new Dictionary<ulong, int>();
        foreach (string key in stringQuery.Keys)
        {
            query[IDEncoder.Parse(key.Split(':')[1])] = (int)stringQuery[key];
        }

        Dictionary<ulong, int> characterData = _autoBox.Select<BoxCharacterDataTemplate>($"from {CHARACTERS_TABLE} where characterID == ?", characterId).AsEnumerable().Select(d => new BoxWorldDataTemplate(d.ID, d.Value)).ToList().ToDictionary(k => k.ID, v => v.Value);
        Dictionary<ulong, int> worldData = _autoBox.Select<BoxWorldDataTemplate>($"from {WORLD_TABLE}").AsEnumerable().ToDictionary(k => k.ID, v => v.Value);

        foreach (ulong key in query.Keys)
        {
            if (characterData.ContainsKey(key) && characterData[key] != query[key])
                return false;
            if (worldData.ContainsKey(key) && worldData[key] != query[key])
                return false;
        }

        return true;
    }

    #endregion Sentence

    #region GetFirstID

    public ulong GetFirstSentenceID(string characterName)
    {
        ulong characterId = IDEncoder.Encode(characterName);

        List<(Dictionary<string, object>, ulong)> stringQueries = _autoBox.Select<BoxFilterDataTemplate>($"from {FILTERS_TABLE} where CharacterID == ?", characterId).AsEnumerable().Select(filter => (filter.Query, filter.DialogueID)).ToList();
        List<(Dictionary<ulong, int>, ulong)> queries = new List<(Dictionary<ulong, int>, ulong)>();
        stringQueries.ForEach(stringQuery =>
        {
            Dictionary<ulong, int> query = new Dictionary<ulong, int>();
            foreach (string key in stringQuery.Item1.Keys)
            {
                query[IDEncoder.Parse(key.Split(':')[1])] = (int)stringQuery.Item1[key];
            }
            queries.Add((query, stringQuery.Item2));
        });
        List<ulong> characterIds = GetCorrespondingIdsFromCharactersTable(characterId, queries);
        List<ulong> worldIds = GetCorrespondingIdsFromWorldTable(queries);
        List<ulong> ids = IntersectIds(characterIds, worldIds);

        if (ids == null)
            return 0;


        List<ulong> bestIds = GetBestIds(ids, characterId);

        ulong randomDialogueId = bestIds[UnityEngine.Random.Range(0, bestIds.Count)];

        ulong firstSentenceId = _autoBox.Select<BoxDialogueDataTemplate>($"from {DIALOGUES_TABLE} where ID == ? limit 0, 1", randomDialogueId).FirstOrDefault().FirstSentenceID;

        return firstSentenceId;
    }

    private List<ulong> GetBestIds(List<ulong> ids, ulong characterId)
    {
        List<ulong> bestIds = new List<ulong>();
        int maxQuery = int.MinValue;

        for (int i = 0; i < ids.Count; i++)
        {
            FilterDataContainer data = GetFilterData(characterId, ids[i]);
            if (data != null && data.Query.Count == maxQuery)
            {
                bestIds.Add(ids[i]);
            }
            else if (data != null && data.Query.Count > maxQuery)
            {
                bestIds = new List<ulong>();
                maxQuery = data.Query.Count;
                bestIds.Add(ids[i]);
            }
        }
        return bestIds;
    }

    private List<ulong> GetCorrespondingIdsFromCharactersTable(ulong characterId, List<(Dictionary<ulong, int>, ulong)> queries)
    {
        Dictionary<ulong, int> characterData = _autoBox.Select<BoxCharacterDataTemplate>($"from {CHARACTERS_TABLE} where CharacterID == ?", characterId).AsEnumerable().Select(data => new BoxWorldDataTemplate(data.ID, data.Value)).ToDictionary(k => k.ID, v => v.Value);
        return GetCorrespondingIds(characterData, queries);
    }

    private List<ulong> GetCorrespondingIdsFromWorldTable(List<(Dictionary<ulong, int>, ulong)> queries)
    {
        Dictionary<ulong, int> worldData = _autoBox.Select<BoxWorldDataTemplate>($"from {WORLD_TABLE}").AsEnumerable().ToDictionary(k => k.ID, v => v.Value);
        return GetCorrespondingIds(worldData, queries);
    }

    private List<ulong> GetCorrespondingIds(Dictionary<ulong, int> data, List<(Dictionary<ulong, int>, ulong)> queries)
    {
        if (data.Count == 0)
            return queries.Select(query => query.Item2).ToList();

        List<ulong> correspondingIds = new List<ulong>();
        for (int i = 0; i < queries.Count; i++)
        {
            bool isValid = true;
            if (queries[i].Item1.Count != 0)
            {
                foreach (ulong key in queries[i].Item1.Keys)
                {
                    if (data.ContainsKey(key) && data[key] != queries[i].Item1[key])
                    {
                        isValid = false;
                        break;
                    }
                }
            }
            if (isValid)
                correspondingIds.Add(queries[i].Item2);
        }
        return correspondingIds;
    }

    private List<ulong> IntersectIds(List<ulong> currentIds, List<ulong> newIds)
    {
        if (currentIds == null && newIds != null)
            return newIds;
        else if (currentIds != null && newIds == null)
            return currentIds;
        else if (currentIds == null && newIds == null)
            return null;

        IEnumerable<ulong> intersectIds = currentIds.Intersect(newIds);
        if (intersectIds == null)
            return null;
        List<ulong> ids = intersectIds.ToList();
        if (ids.Count == 0)
            return null;
        return ids;
    }

    #endregion GetFirstID

    #region AddData

    public bool AddDialogueData(DialogueDataContainer dialogueData)
    {
        BoxDialogueDataTemplate data = new BoxDialogueDataTemplate(dialogueData);
        long count = _autoBox.Count($"from {DIALOGUES_TABLE} where ID == ? limit 0,1", data.ID);
        _cube = _autoBox.Cube();
        if (count == 0)
            _cube[DIALOGUES_TABLE].Insert(data);
        else
            _cube[DIALOGUES_TABLE].Update(data);

        CommitResult result = _cube.Commit();
        if (result == CommitResult.OK)
            return true;
        return false;
    }

    public bool AddSentenceData(SentenceDataContainer sentenceData)
    {
        BoxSentenceDataTemplate data = new BoxSentenceDataTemplate(sentenceData);
        long count = _autoBox.Count($"from {SENTENCES_TABLE} where ID == ? limit 0,1", data.ID);
        _cube = _autoBox.Cube();
        if (count == 0)
            _cube[SENTENCES_TABLE].Insert(data);
        else
            _cube[SENTENCES_TABLE].Update(data);
        CommitResult result = _cube.Commit();
        if (result == CommitResult.OK)
            return true;
        return false;
    }

    public bool AddFilterData(FilterDataContainer filterData)
    {
        BoxFilterDataTemplate data = new BoxFilterDataTemplate(filterData);
        long count = _autoBox.Count($"from {FILTERS_TABLE} where CharacterID == ? & DialogueID == ? limit 0,1", data.CharacterID, data.DialogueID);
        _cube = _autoBox.Cube();
        if (count == 0)
            _cube[FILTERS_TABLE].Insert(data);
        else
            _cube[FILTERS_TABLE].Update(data);
        CommitResult result = _cube.Commit();
        if (result == CommitResult.OK)
            return true;
        return false;
    }

    public bool AddWorldData(WorldDataContainer worldData)
    {
        BoxWorldDataTemplate data = new BoxWorldDataTemplate(worldData);
        long count = _autoBox.Count($"from {WORLD_TABLE} where ID == ? limit 0,1", data.ID);
        _cube = _autoBox.Cube();
        if (count == 0)
            _cube[WORLD_TABLE].Insert(data);
        else
            _cube[WORLD_TABLE].Update(data);
        CommitResult result = _cube.Commit();
        if (result == CommitResult.OK)
            return true;
        return false;
    }

    public bool AddCharacterData(CharacterDataContainer characterData)
    {
        BoxCharacterDataTemplate data = new BoxCharacterDataTemplate(characterData);
        long count = _autoBox.Count($"from {CHARACTERS_TABLE} where CharacterID == ? & ID == ? limit 0,1", data.CharacterID, data.ID);
        _cube = _autoBox.Cube();
        if (count == 0)
            _cube[CHARACTERS_TABLE].Insert(data);
        else
            _cube[CHARACTERS_TABLE].Update(data);
        CommitResult result = _cube.Commit();
        if (result == CommitResult.OK)
            return true;
        return false;
    }

    #endregion AddData

    #region RemoveData

    public bool RemoveDialogueData(ulong id)
    {
        return _autoBox.Delete(DIALOGUES_TABLE, id);
    }

    public bool RemoveSentenceData(ulong id)
    {
        return _autoBox.Delete(SENTENCES_TABLE, id);
    }

    public bool RemoveFilterData(ulong characterId, ulong dialogueId)
    {
        return _autoBox.Delete(FILTERS_TABLE, characterId, dialogueId);
    }

    public bool RemoveWorldData(ulong id)
    {
        return _autoBox.Delete(WORLD_TABLE, id);
    }

    public bool RemoveCharacterData(ulong characterId, ulong id)
    {
        return _autoBox.Delete(CHARACTERS_TABLE, characterId, id);
    }

    #endregion RemoveData

    #region GetData

    public List<DialogueDataContainer> GetDialogueData()
    {
        List<BoxDialogueDataTemplate> data = _autoBox.Select<BoxDialogueDataTemplate>($"from {DIALOGUES_TABLE}");
        if (data == null || data.Count == 0)
            return new List<DialogueDataContainer>();
        List<DialogueDataContainer> dialogueData = new List<DialogueDataContainer>();
        data.ForEach(d => dialogueData.Add(d.ToDataContainer()));
        return dialogueData;
    }

    public List<SentenceDataContainer> GetSentenceData()
    {
        List<BoxSentenceDataTemplate> data = _autoBox.Select<BoxSentenceDataTemplate>($"from {SENTENCES_TABLE}");
        if (data == null || data.Count == 0)
            return new List<SentenceDataContainer>();
        List<SentenceDataContainer> dialogueData = new List<SentenceDataContainer>();
        data.ForEach(d => dialogueData.Add(d.ToDataContainer()));
        return dialogueData;
    }

    public SentenceDataContainer GetSentenceData(ulong id)
    {
        BoxSentenceDataTemplate data = _autoBox.Select<BoxSentenceDataTemplate>($"from {SENTENCES_TABLE} where ID == ? limit 0, 1", id).FirstOrDefault();
        if (data == null)
            return null;

        return data.ToDataContainer();
    }

    public List<FilterDataContainer> GetFilterData()
    {
        List<BoxFilterDataTemplate> data = _autoBox.Select<BoxFilterDataTemplate>($"from {FILTERS_TABLE}");
        if (data == null || data.Count == 0)
            return new List<FilterDataContainer>();
        List<FilterDataContainer> dialogueData = new List<FilterDataContainer>();
        data.ForEach(d => dialogueData.Add(d.ToDataContainer()));
        return dialogueData;
    }

    public FilterDataContainer GetFilterData(ulong characterId, ulong id)
    {
        BoxFilterDataTemplate data = _autoBox.Select<BoxFilterDataTemplate>($"from {FILTERS_TABLE} where CharacterID == ? & DialogueID == ? limit 0,1", characterId, id).FirstOrDefault();

        if (data == null)
            return null;

        return data.ToDataContainer();
    }

    public List<WorldDataContainer> GetWorldData()
    {
        List<WorldDataContainer> worldData = new List<WorldDataContainer>();
        try
        {
            List<BoxWorldDataTemplate> data = _autoBox.Select<BoxWorldDataTemplate>($"from {WORLD_TABLE}");
            if (data == null || data.Count == 0)
                return new List<WorldDataContainer>();
            data.ForEach(d => worldData.Add(d.ToDataContainer()));
        }
        catch { }
        return worldData;
    }

    public int GetWorldDataValue(ulong id)
    {
        int value = 0;
        BoxWorldDataTemplate data = _autoBox.Select<BoxWorldDataTemplate>($"from {WORLD_TABLE} where ID == ? limit 0,1", id).FirstOrDefault();
        if (data == null)
            value = -1;
        else
            value = data.Value;
        return value;
    }

    public List<CharacterDataContainer> GetCharacterData()
    {
        List<CharacterDataContainer> worldData = new List<CharacterDataContainer>();
        try
        {
            List<BoxCharacterDataTemplate> data = _autoBox.Select<BoxCharacterDataTemplate>($"from {CHARACTERS_TABLE}");
            if (data == null || data.Count == 0)
                return new List<CharacterDataContainer>();
            data.ForEach(d => worldData.Add(d.ToDataContainer()));
        }
        catch { }
        return worldData;
    }

    public int GetCharacterDataValue(ulong characterId, ulong id)
    {
        int value = 0;
        BoxCharacterDataTemplate data = _autoBox.Select<BoxCharacterDataTemplate>($"from {CHARACTERS_TABLE} where CharacterID == ? & ID == ? limit 0,1", characterId, id).FirstOrDefault();
        if (data == null)
            value = -1;
        else
            value = data.Value;
        return value;
    }

    #endregion GetData

    #region Exist

    public bool DoSentenceExist(ulong id)
    {
        long count = _autoBox.Count($"from {SENTENCES_TABLE} where ID == ? limit 0,1", id);
        return count > 0;
    }

    public bool DoDialogueExist(ulong id)
    {
        long count = _autoBox.Count($"from {DIALOGUES_TABLE} where ID == ? limit 0,1", id);
        return count > 0;
    }

    public bool DoFilterExist(ulong characterId, ulong dialogueId)
    {
        long count = _autoBox.Count($"from {FILTERS_TABLE} where CharacterID == ? & DialogueID == ? limit 0,1", characterId, dialogueId);
        return count > 0;
    }

    public bool DoWorldQueryExist(ulong id)
    {
        long count = _autoBox.Count($"from {WORLD_TABLE} where ID == ? limit 0,1", id);
        return count > 0;
    }

    public bool DoCharacterQueryExist(ulong characterId, ulong id)
    {
        long count = _autoBox.Count($"from {CHARACTERS_TABLE} where CharacterID == ? & ID == ? limit 0,1", characterId, id);
        return count > 0;
    }

    #endregion Exist

    public void CloseDatabase()
    {
        _database.Dispose();
    }
    public bool IsWorldQueryValid(WorldDataContainer worldData)
    {
        BoxWorldDataTemplate data = new BoxWorldDataTemplate(worldData);
        BoxWorldDataTemplate query = _autoBox.Select<BoxWorldDataTemplate>($"from {WORLD_TABLE} where ID == ? limit 0,1", data.ID).FirstOrDefault();

        if (query == null || query.Value != worldData.Value)
            return false;
        return true;
    }

    public bool IsCharacterQueryValid(CharacterDataContainer characterData)
    {
        BoxCharacterDataTemplate data = new BoxCharacterDataTemplate(characterData);
        BoxCharacterDataTemplate query = _autoBox.Select<BoxCharacterDataTemplate>($"from {CHARACTERS_TABLE} where CharacterID == ? & ID == ? limit 0,1", data.CharacterID, data.ID).FirstOrDefault();

        if (query == null || query.Value != characterData.Value)
            return false;
        return true;
    }
}
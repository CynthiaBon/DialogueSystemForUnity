using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public static IDatabase Database { get; private set; } = null;

    private void Awake()
    {
        if (Database == null)
            CreateDatabase();
    }

    public static void CreateDatabase()
    {
        if (Database == null)
            Database = new BoxDatabase();
    }

    private void OnDisable()
    {
        CloseDatabase();
    }

    public static void CloseDatabase()
    {
        if (Database != null)
        {
            Database.CloseDatabase();
            Database = null;
        }
    }

    public static bool IsQueryVariableValid(string argument)
    {
        if (Database == null)
            CreateDatabase();

        string[] splitArguments = argument.Split(':');

        if (splitArguments[0] == "Characters")
        {
            string[] ids = splitArguments[1].Split('.');
            return Database.IsCharacterQueryValid(new CharacterDataContainer(ids[0], ids[1], int.Parse(splitArguments[2])));
        }
        else if (splitArguments[0] == "World")
        {
            return Database.IsWorldQueryValid(new WorldDataContainer(splitArguments[1], int.Parse(splitArguments[2])));
        }
        return false;
    }

    public static int GetValue(string argument)
    {
        int value = 0;
        if (Database == null)
            CreateDatabase();

        string[] splitArguments = argument.Split(':');

        if (splitArguments[0] == "Characters")
        {
            string[] ids = splitArguments[1].Split('.');
            value = Database.GetCharacterDataValue(IDEncoder.Encode(ids[0]), IDEncoder.Encode(ids[1]));
        }
        else if (splitArguments[0] == "World")
        {
            value = Database.GetWorldDataValue(IDEncoder.Encode(splitArguments[1]));
        }
        return value;
    }

    public static void SetQueryVariable(string argument)
    {
        if (Database == null)
            CreateDatabase();

        string[] splitArguments = argument.Split(':');

        if (splitArguments[0] == "Characters")
        {
            string[] ids = splitArguments[1].Split('.');
            Database.AddCharacterData(new CharacterDataContainer(ids[0], ids[1], int.Parse(splitArguments[2])));
        }
        else if (splitArguments[0] == "World")
        {
            Database.AddWorldData(new WorldDataContainer(splitArguments[1], int.Parse(splitArguments[2])));
        }
    }

    public static void RemoveQueryVariable(string argument)
    {
        if (Database == null)
            CreateDatabase();

        string[] splitArguments = argument.Split(':');

        if (splitArguments[0] == "Characters")
        {
            string[] ids = splitArguments[1].Split('.');
            Database.RemoveCharacterData(IDEncoder.Encode(ids[0]), IDEncoder.Encode(ids[1]));
        }
        else if (splitArguments[0] == "World")
        {
            Database.RemoveWorldData(IDEncoder.Encode(splitArguments[1]));
        }
    }

    public static bool DoQueryVariableExist(string argument)
    {
        if (Database == null)
            CreateDatabase();

        string[] splitArguments = argument.Split(':');

        if (splitArguments[0] == "Characters")
        {
            string[] ids = splitArguments[1].Split('.');
            return Database.DoCharacterQueryExist(IDEncoder.Encode(ids[0]), IDEncoder.Encode(ids[1]));
        }
        else if (splitArguments[0] == "World")
        {
            return Database.DoWorldQueryExist(IDEncoder.Encode(splitArguments[1]));
        }
        return false;
    }

    public static bool DoSentenceExist(string id)
    {
        if (Database == null)
            CreateDatabase();

        return Database.DoSentenceExist(IDEncoder.Encode(id));
    }

    public static bool DoDialogueExist(string id)
    {
        if (Database == null)
            CreateDatabase();

        return Database.DoDialogueExist(IDEncoder.Encode(id));
    }

    public static bool DoFilterExist(string characterId, string dialogueId)
    {
        if (Database == null)
            CreateDatabase();

        return Database.DoFilterExist(IDEncoder.Encode(characterId), IDEncoder.Encode(dialogueId));
    }

    public static List<WorldDataContainer> GetWorldData()
    {
        if (Database == null)
            CreateDatabase();
        return Database.GetWorldData();
    }

    public static List<CharacterDataContainer> GetCharacterData()
    {
        if (Database == null)
            CreateDatabase();
        return Database.GetCharacterData();
    }
}

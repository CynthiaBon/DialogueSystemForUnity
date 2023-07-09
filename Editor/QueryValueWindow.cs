using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class QueryValueWindow : EditorWindow
{
    private static QueryValueContainer selectedQueryValue = null;
    private List<QueryValueContainer> _queryValues = null;
    private int _value = 0;

    public static QueryValueContainer Open()
    {
        QueryValueWindow queryValueWindow = new QueryValueWindow();
        queryValueWindow.ShowModal();
        return selectedQueryValue;
    }

    private void OnEnable()
    {
        _queryValues = new List<QueryValueContainer>();
        List<WorldDataContainer> worldData = DatabaseManager.GetWorldData();
        if (worldData != null)
            worldData.ForEach(data => _queryValues.Add(new QueryValueContainer("World", data.ID, data.Value)));
        List<CharacterDataContainer> characterData = DatabaseManager.GetCharacterData();
        if (characterData != null)
            characterData.ForEach(data => _queryValues.Add(new QueryValueContainer(data.CharacterID, data.ID, data.Value)));
    }

    private void OnGUI()
    {
        _value = EditorGUILayout.IntField("Value", _value);
        for (int i = 0; i < _queryValues.Count; i++)
        {
            if (GUILayout.Button($"Add {_queryValues[i]} : {_value}"))
            {
                selectedQueryValue = new QueryValueContainer(_queryValues[i].Type, _queryValues[i].Name, _value);
                Close();
            }
        }
    }
}

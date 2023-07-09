using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

public class DialogueTableEditor : EditorWindow
{
    //Dialogue creation
    private string _newDialogue = "";
    private string _characterName = "";

    //Dialogue search
    private string _searchedDialogueName = "";
    private string _searchedDialogueCharacter = "";

    //Dialogue display
    private Vector2 _scrollPosition = Vector2.zero;
    List<(string, string)> _filterNames = null;
    List<(string, string)> _searchFilterNames = null;

    [MenuItem("Tools/DialogueGraph")]
    public static void Open()
    {
        EditorWindow window = GetWindow<DialogueTableEditor>();
        window.titleContent = new GUIContent("Dialogue table");
    }

    private void RefreshDialogueList()
    {
        DatabaseManager.CreateDatabase();
        List<FilterDataContainer> data = DatabaseManager.Database.GetFilterData();
        if (data != null)
            _filterNames = data.Select(d => (d.DialogueID, d.CharacterID)).ToList();
        else
            _filterNames = new List<(string, string)>();
        _searchFilterNames = _filterNames;
    }

    private void OnFocus()
    {
        RefreshDialogueList();
    }

    private void OnGUI()
    {
        DialogueCreationDisplay();
        DrawLine();
        DialogueSearchDisplay();
        DrawLine();
        DisplayDialogues();
    }

    private void DialogueCreationDisplay()
    {
        GUILayout.TextField("Create", EditorStyles.boldLabel);

        GUI.SetNextControlName("text");
        _newDialogue = EditorGUILayout.TextField("Dialogue to create", _newDialogue);
        _characterName = EditorGUILayout.TextField("Linked character", _characterName);
        if (IsTextValid(_newDialogue) && IsTextValid(_characterName) && !DatabaseManager.DoDialogueExist(_newDialogue) && !DatabaseManager.DoFilterExist(_characterName, _newDialogue) && GUILayout.Button("Create dialogue"))
        {
            DatabaseManager.CreateDatabase();
            DatabaseManager.Database.AddDialogueData(new DialogueDataContainer(_newDialogue, ""));
            DatabaseManager.Database.AddFilterData(new FilterDataContainer(_characterName, _newDialogue, new List<QueryValueContainer>()));
            DialogueGraph.OpenGraph(_newDialogue, _characterName);
            _newDialogue = "";
            _characterName = "";
        }
    }

    private void DialogueSearchDisplay()
    {
        GUILayout.TextField("Search", EditorStyles.boldLabel);
        _searchedDialogueName = EditorGUILayout.TextField("Dialogue name", _searchedDialogueName);
        _searchedDialogueCharacter = EditorGUILayout.TextField("Linked character name", _searchedDialogueCharacter);

        if (GUILayout.Button("Search"))
            _searchFilterNames = _filterNames.Where(filter => (_searchedDialogueName == null || _searchedDialogueName == "" || _searchedDialogueName == filter.Item1) && (_searchedDialogueCharacter == null || _searchedDialogueCharacter == "" || _searchedDialogueCharacter == filter.Item2)).ToList();
    }

    private bool IsTextValid(string text)
    {
        return 0 < text.Length && text.Length <= 12 && Regex.IsMatch(text, "^[a-z_]*$");
    }

    private void DisplayDialogues()
    {
        GUILayout.TextField("Dialogues", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        for (int i = 0; i < _searchFilterNames.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{_searchFilterNames[i].Item2}: {_searchFilterNames[i].Item1}");

            if (GUILayout.Button("Edit dialogue"))
                DialogueGraph.OpenGraph(_searchFilterNames[i].Item1, _searchFilterNames[i].Item2);

            if (GUILayout.Button("Delete dialogue"))
            {
                DeleteDialogueSentences(_searchFilterNames[i].Item1);
                DatabaseManager.Database.RemoveDialogueData(IDEncoder.Encode(_searchFilterNames[i].Item1));
                DatabaseManager.Database.RemoveFilterData(IDEncoder.Encode(_searchFilterNames[i].Item2), IDEncoder.Encode(_searchFilterNames[i].Item1));
                GraphSaveUtility.DeleteSave(_searchFilterNames[i].Item1);
                RefreshDialogueList();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DeleteDialogueSentences(string dialogueName)
    {
        DialogueContainer dialogueContainer = GraphSaveUtility.GetSave(dialogueName);
        DatabaseManager.CreateDatabase();
        dialogueContainer.DialogueNodesData.ForEach(node => DatabaseManager.Database.RemoveSentenceData(node.ID));
    }

    private void DrawLine()
    {
        GUILayout.Label("");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("");
    }
}

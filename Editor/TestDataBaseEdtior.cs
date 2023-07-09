using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TestDataBaseEdtior : EditorWindow
{
    private Vector2 _scrollPosition;

    private string _characterName = "";
    private ulong _selectedDialogueId = 0;

    private DialogueDataContainer _dialogueData = null;
    private SentenceDataContainer _sentenceData = null;
    private FilterDataContainer _filterData = null;
    private WorldDataContainer _worldData = null;
    private CharacterDataContainer _characterData = null;

    private List<DialogueDataContainer> _dialogueDataList = null;
    private List<SentenceDataContainer> _sentenceDataList = null;
    private List<FilterDataContainer> _filterDataList = null;
    private List<WorldDataContainer> _worldDataList = null;
    private List<CharacterDataContainer> _characterDataList = null;

    private string _dialogueId = "";
    private string _sentenceId = "";
    private string _filterIdCharacter = "";
    private string _filterIdDialogue = "";
    private string _worldId = "";
    private string _characterIdCharacter = "";
    private string _characterId = "";

    [MenuItem("Tools/DialogueDatabase")]
    private static void CreateWindow()
    {
        EditorWindow.GetWindow(typeof(TestDataBaseEdtior));
    }

    private void OnEnable()
    {
        DatabaseManager.CreateDatabase();
        FillData();
        _dialogueData = new DialogueDataContainer();
        _sentenceData = new SentenceDataContainer();
        _filterData = new FilterDataContainer();
        _worldData = new WorldDataContainer();
        _characterData = new CharacterDataContainer();
    }

    private void OnDisable()
    {
        DatabaseManager.CloseDatabase();
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.ExpandHeight(true));

        DialogueData();
        DrawLine();

        SentenceData();
        DrawLine();

        FilterData();
        DrawLine();

        WorldData();
        DrawLine();

        CharacterData();
        DrawLine();

        DisplayData();
        DrawLine();

        RemoveData();
        DrawLine();

        DialogueParsing();

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    #region AddData

    public void DialogueData()
    {
        EditorGUILayout.LabelField("Dialogue Data", EditorStyles.boldLabel);
        _dialogueData.ID = EditorGUILayout.TextField("Dialogue title", _dialogueData.ID);
        _dialogueData.FirstSentenceID = EditorGUILayout.TextField("First sentence title", _dialogueData.FirstSentenceID);
        if (GUILayout.Button("Add or refresh dialogue data"))
            _dialogueData.AddToDatabase();
    }

    public void SentenceData()
    {
        EditorGUILayout.LabelField("Setence Data", EditorStyles.boldLabel);
        _sentenceData.ID = EditorGUILayout.TextField("Sentence title", _sentenceData.ID);
        _sentenceData.Speaker = EditorGUILayout.TextField("Speaker", _sentenceData.Speaker);
        _sentenceData.Text = EditorGUILayout.TextField("Text", _sentenceData.Text);
        _sentenceData.HasChoice = GUILayout.Toggle(_sentenceData.HasChoice, "Has choice");

        EditorGUILayout.LabelField("Choices");
        for (int i = 0; i < _sentenceData.ChoiceIDs.Count; i++)
        {
            _sentenceData.ChoiceIDs[i] = EditorGUILayout.TextField(_sentenceData.ChoiceIDs[i]);
        }
        if (GUILayout.Button("Add choice"))
            _sentenceData.ChoiceIDs.Add("");
        if (GUILayout.Button("Remove last choice"))
            _sentenceData.ChoiceIDs.RemoveAt(_sentenceData.ChoiceIDs.Count - 1);

        EditorGUILayout.LabelField("Dialogue events");
        _sentenceData.OnDialogueEvents.ForEach(dialogueEvent =>
        {
            dialogueEvent.MethodName = EditorGUILayout.TextField("Method name", dialogueEvent.MethodName);
            dialogueEvent.Argument = EditorGUILayout.TextField("Argument", dialogueEvent.Argument);
        });
        if (GUILayout.Button("Add dialogue event"))
            _sentenceData.OnDialogueEvents.Add(new EventMethodContainer());
        if (GUILayout.Button("Remove last event"))
            _sentenceData.OnDialogueEvents.RemoveAt(_sentenceData.OnDialogueEvents.Count - 1);

        EditorGUILayout.LabelField("End events");
        _sentenceData.OnEndEvents.ForEach(endEvent =>
        {
            endEvent.MethodName = EditorGUILayout.TextField("Method name", endEvent.MethodName);
            endEvent.Argument = EditorGUILayout.TextField("Argument", endEvent.Argument);
        });
        if (GUILayout.Button("Add end event"))
            _sentenceData.OnEndEvents.Add(new EventMethodContainer());
        if (GUILayout.Button("Remove last event"))
            _sentenceData.OnEndEvents.RemoveAt(_sentenceData.OnEndEvents.Count - 1);

        _sentenceData.NextSentenceID = EditorGUILayout.TextField("Next sentence title", _sentenceData.NextSentenceID);

        EditorGUILayout.LabelField("Query");
        _sentenceData.Query.ForEach(query =>
        {
            query.Name = EditorGUILayout.TextField("Data title", query.Name);
            query.Value = EditorGUILayout.IntField("Data value", query.Value);
        });
        if (GUILayout.Button("Add query"))
            _sentenceData.Query.Add(new QueryValueContainer());
        if (GUILayout.Button("Remove last query"))
            _sentenceData.Query.RemoveAt(_sentenceData.Query.Count - 1);

        if (GUILayout.Button("Add or refresh sentence data"))
            _sentenceData.AddToDatabase();
    }

    public void FilterData()
    {
        EditorGUILayout.LabelField("Filter Data", EditorStyles.boldLabel);
        _filterData.CharacterID = EditorGUILayout.TextField("Character name", _filterData.CharacterID);
        _filterData.DialogueID = EditorGUILayout.TextField("Dialogue title", _filterData.DialogueID);

        EditorGUILayout.LabelField("Query");
        _filterData.Query.ForEach(query =>
        {
            query.Name = EditorGUILayout.TextField("Data title", query.Name);
            query.Value = EditorGUILayout.IntField("Data value", query.Value);
        });
        if (GUILayout.Button("Add query"))
            _filterData.Query.Add(new QueryValueContainer());
        if (GUILayout.Button("Remove last query"))
            _filterData.Query.RemoveAt(_filterData.Query.Count - 1);

        if (GUILayout.Button("Add or refresh filter data"))
            _filterData.AddToDatabase();
    }

    public void WorldData()
    {
        EditorGUILayout.LabelField("World Data", EditorStyles.boldLabel);
        _worldData.ID = EditorGUILayout.TextField("Data title", _worldData.ID);
        _worldData.Value = EditorGUILayout.IntField("Data value", _worldData.Value);
        if (GUILayout.Button("Add or refresh world data"))
            _worldData.AddToDatabase();
    }

    public void CharacterData()
    {
        EditorGUILayout.LabelField("Character Data", EditorStyles.boldLabel);
        _characterData.CharacterID = EditorGUILayout.TextField("Character name", _characterData.CharacterID);
        _characterData.ID = EditorGUILayout.TextField("Data title", _characterData.ID);
        _characterData.Value = EditorGUILayout.IntField("Data value", _characterData.Value);
        if (GUILayout.Button("Add or refresh character data"))
            _characterData.AddToDatabase();
    }

    #endregion AddData

    #region RemoveData

    private void RemoveData()
    {
        EditorGUILayout.LabelField("Remove data", EditorStyles.boldLabel);

        _dialogueId = EditorGUILayout.TextField("Dialogue title", _dialogueId);
        if (GUILayout.Button("Remove dialogue data"))
            DatabaseManager.Database.RemoveDialogueData(IDEncoder.Encode(_dialogueId));

        _sentenceId = EditorGUILayout.TextField("Sentence title", _sentenceId);
        if (GUILayout.Button("Remove sentence data"))
            DatabaseManager.Database.RemoveSentenceData(IDEncoder.Encode(_sentenceId));

        _filterIdCharacter = EditorGUILayout.TextField("Character name", _filterIdCharacter);
        _filterIdDialogue = EditorGUILayout.TextField("Dialogue title", _filterIdDialogue);
        if (GUILayout.Button("Remove filter data"))
            DatabaseManager.Database.RemoveFilterData(IDEncoder.Encode(_filterIdCharacter), IDEncoder.Encode(_filterIdDialogue));

        _worldId = EditorGUILayout.TextField("World query title", _worldId);
        if (GUILayout.Button("Remove world data"))
            DatabaseManager.Database.RemoveWorldData(IDEncoder.Encode(_worldId));

        _characterIdCharacter = EditorGUILayout.TextField("Character name", _characterIdCharacter);
        _characterId = EditorGUILayout.TextField("Character query title", _characterId);
        if (GUILayout.Button("Remove character data"))
            DatabaseManager.Database.RemoveCharacterData(IDEncoder.Encode(_characterIdCharacter), IDEncoder.Encode(_characterId));
    }

    #endregion RemoveData

    #region DisplayData

    private void FillData()
    {
        _dialogueDataList = DatabaseManager.Database.GetDialogueData();
        _sentenceDataList = DatabaseManager.Database.GetSentenceData();
        _filterDataList = DatabaseManager.Database.GetFilterData();
        _worldDataList = DatabaseManager.Database.GetWorldData();
        _characterDataList = DatabaseManager.Database.GetCharacterData();
    }

    private void DisplayData()
    {
        EditorGUILayout.LabelField("Database", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh data"))
            FillData();

        EditorGUILayout.LabelField("Dialogue Data", EditorStyles.boldLabel);
        if (_dialogueDataList != null && _dialogueDataList.Count != 0)
        {
            _dialogueDataList.ForEach(data =>
            {
                EditorGUILayout.LabelField("Title: " + data.ID);
                EditorGUILayout.LabelField("First sentence ID: " + data.FirstSentenceID);
            });
        }
        DrawLine();

        EditorGUILayout.LabelField("Sentence Data", EditorStyles.boldLabel);
        if (_sentenceDataList != null && _sentenceDataList.Count != 0)
        {
            _sentenceDataList.ForEach(data =>
            {
                EditorGUILayout.LabelField("Title: " + data.ID);
                EditorGUILayout.LabelField("Speaker: " + data.Speaker);
                EditorGUILayout.LabelField("Text: " + data.Text);
                EditorGUILayout.LabelField("Has choice: " + data.HasChoice.ToString());

                EditorGUILayout.LabelField("Choices");
                if (data.ChoiceIDs != null)
                    data.ChoiceIDs.ForEach(choice => EditorGUILayout.LabelField(choice));

                EditorGUILayout.LabelField("Dialogue events");
                if (data.OnDialogueEvents != null)
                    data.OnDialogueEvents.ForEach(dialogueEvent => EditorGUILayout.LabelField(dialogueEvent.MethodName + "(" + dialogueEvent.Argument + ")"));

                EditorGUILayout.LabelField("End events");
                if (data.OnEndEvents != null)
                    data.OnEndEvents.ForEach(endEvent => EditorGUILayout.LabelField(endEvent.MethodName + "(" + endEvent.Argument + ")"));

                EditorGUILayout.LabelField("Next sentence title: " + data.NextSentenceID);

                EditorGUILayout.LabelField("Query");
                if (data.Query != null)
                    data.Query.ForEach(query => EditorGUILayout.LabelField(query.Type + "." + query.Name + ": " + query.Value.ToString()));

                EditorGUILayout.LabelField("");
                EditorGUILayout.LabelField("----------------------------------");
                EditorGUILayout.LabelField("");
            });
        }
        DrawLine();

        EditorGUILayout.LabelField("Filter Data", EditorStyles.boldLabel);
        if (_filterDataList != null && _filterDataList.Count != 0)
        {
            _filterDataList.ForEach(data =>
            {
                EditorGUILayout.LabelField("Character title: " + data.CharacterID);
                EditorGUILayout.LabelField("Dialogue title: " + data.DialogueID);
                EditorGUILayout.LabelField("Query");
                data.Query.ForEach(query => EditorGUILayout.LabelField(query.Type + ":" + query.Name + ": " + query.Value.ToString()));
            });
        }
        DrawLine();

        EditorGUILayout.LabelField("World Data", EditorStyles.boldLabel);
        if (_worldDataList != null && _worldDataList.Count != 0)
        {
            _worldDataList.ForEach(data =>
            {
                EditorGUILayout.LabelField("Data title: " + data.ID);
                EditorGUILayout.LabelField("Data value: " + data.Value);
            });
        }
        DrawLine();

        EditorGUILayout.LabelField("Characters Data", EditorStyles.boldLabel);
        if (_characterDataList != null && _characterDataList.Count != 0)
        {
            _characterDataList.ForEach(data =>
            {
                EditorGUILayout.LabelField("Character name: " + data.CharacterID);
                EditorGUILayout.LabelField("Data title: " + data.ID);
                EditorGUILayout.LabelField("Data value: " + data.Value);
            });
        }
    }

    #endregion DisplayData

    #region DialogueParsing

    private void DialogueParsing()
    {
        EditorGUILayout.LabelField("Dialogue parsing", EditorStyles.boldLabel);

        _characterName = EditorGUILayout.TextField("Character name", _characterName);
        if (GUILayout.Button("Search dialogue ID"))
            _selectedDialogueId = DatabaseManager.Database.GetFirstSentenceID(_characterName);
        EditorGUILayout.LabelField($"DialogueID: {IDEncoder.Decode(_selectedDialogueId)}");
    }

    #endregion DialogueParsing

    private void DrawLine()
    {
        GUILayout.Label("");
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("");
    }
}

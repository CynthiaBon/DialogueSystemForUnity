using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraph : EditorWindow
{
    public static string DialogueName = null;
    public static string CharacterName = null;
    public static string FirstSentenceName = null;
    public static bool IsLoading { get; private set; } = false;

    private DialogueGraphView _graphView = null;
    private MiniMap _miniMap = null;

    public static void OpenGraph(string dialogueName, string characterName)
    {
        DialogueName = dialogueName;
        CharacterName = characterName;
        EditorWindow window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue graph");
    }

    private void OnEnable()
    {
        IsLoading = true;
        ConstructGraphView();
        ConstructToolBar();
        ConstrcutMinimap();
        ConstructBlackboard();
        DatabaseManager.CreateDatabase();
        RequestData(false);
        IsLoading = false;
    }

    private void ConstructBlackboard()
    {
        Blackboard blackboard = new Blackboard(_graphView);
        BlackboardSection worldSection = new BlackboardSection();
        worldSection.title = "World variables";
        blackboard.Add(worldSection);

        BlackboardSection characterSection = new BlackboardSection();
        characterSection.title = "Characters variables";
        blackboard.Add(characterSection);

        BlackboardSection filterSection = new BlackboardSection();
        filterSection.title = "Filter variables";
        blackboard.Add(filterSection);

        blackboard.addItemRequested += _blackboard =>
        {
            int index = DropdownWindow.Open();
            if (index == -1)
                return;

            if (index == 0 || index == 1)
            {
                _graphView.AddQueryToGraphView(index, worldSection, characterSection);
            }
            else
            {
                QueryValueContainer queryValue = QueryValueWindow.Open();
                if (queryValue == null)
                    return;
                _graphView.AddFilterToGraphView(queryValue, filterSection);
            }

        };

        blackboard.editTextRequested = (_blackboard, element, newName) =>
        {
            string oldName = ((BlackboardField)element).text;
            if (element.name == "World")
            {
                if (DatabaseManager.DoQueryVariableExist($"World:{newName}"))
                {
                    EditorUtility.DisplayDialog("Invalid query name", "Already exist", "OK");
                }
                else if (newName.Length > 12 || !Regex.IsMatch(newName, "^[a-z_]*$"))
                {
                    EditorUtility.DisplayDialog("Invalid query name", "Must be \"a\" to \"z\" or \"_\", not empty and less or equal than 12 letters", "OK");
                }
                else
                {
                    ((BlackboardField)element).text = newName;
                    int queryValue = DatabaseManager.GetValue($"World:{oldName}");
                    DatabaseManager.RemoveQueryVariable($"World:{oldName}");
                    DatabaseManager.SetQueryVariable($"World:{newName}:{queryValue}");
                }
            }
            else if (element.name == "Characters")
            {
                BlackboardRow row = QInParents<BlackboardRow>(element, null);
                row.expanded = true;
                TextField characterIdField = QInParents<TextField>(element, "CharacterID");
                if (DatabaseManager.DoQueryVariableExist($"Characters:{characterIdField.text}.{newName}"))
                {
                    EditorUtility.DisplayDialog("Invalid query name", "Already exist", "OK");
                }
                else if (newName.Length > 12 || !Regex.IsMatch(newName, "^[a-z_]*$"))
                {
                    EditorUtility.DisplayDialog("Invalid query name", "Must be \"a\" to \"z\" or \"_\", not empty and less or equal than 12 letters", "OK");
                }
                else
                {
                    ((BlackboardField)element).text = newName;
                    int queryValue = DatabaseManager.GetValue($"Characters:{characterIdField.text}.{oldName}");
                    DatabaseManager.RemoveQueryVariable($"Characters:{characterIdField.text}.{oldName}");
                    DatabaseManager.SetQueryVariable($"Characters:{characterIdField.text}.{newName}:{queryValue}");
                }
            }
        };

        List<WorldDataContainer> worldData = DatabaseManager.GetWorldData();
        if (worldData != null && worldData.Count != 0)
            worldData.ForEach(data => _graphView.AddWorldQuery(data, worldSection, false));

        List<CharacterDataContainer> characterData = DatabaseManager.GetCharacterData();
        if (characterData != null && characterData.Count != 0)
            characterData.ForEach(data => _graphView.AddCharacterQuery(data, characterSection, false));

        FilterDataContainer data = DatabaseManager.Database.GetFilterData(IDEncoder.Encode(CharacterName), IDEncoder.Encode(DialogueName));
        data.Query.ForEach(q =>
        {
            if (q.Type == "World")
                _graphView.AddFilterQuery(new WorldDataContainer(q.Name, q.Value), filterSection, false);
            else
                _graphView.AddFilterQuery(new CharacterDataContainer(q.Type, q.Name, q.Value), filterSection, false);
        });

        blackboard.SetPosition(new Rect(10f, 30f, 300f, 300f));
        _graphView.Add(blackboard);
    }

    public T QInChildren<T>(VisualElement element, string name) where T : VisualElement
    {
        T result = null;
        result = element.Q<T>(name);
        if (result != null)
            return result;

        List<VisualElement> children = element.Children().ToList();
        for (int i = 0; i < children.Count; i++)
        {
            result = QInChildren<T>(children[i], name);
            if (result != null)
                return result;
        }
        return null;
    }

    public T QInParents<T>(VisualElement element, string name) where T : VisualElement
    {
        T result = null;
        VisualElement parent = element;
        while (parent != null && result == null)
        {
            result = QInChildren<T>(parent, name);
            parent = parent.parent;
        }
        return result;
    }

    private void ConstrcutMinimap()
    {
        _miniMap = new MiniMap();
        //miniMap.anchored = true;
        _miniMap.SetPosition(new Rect(position.size.x - 10f, 30f, 200f, 140f));
        _graphView.Add(_miniMap);
        _graphView.RegisterCallback<GeometryChangedEvent>(evt => _miniMap.SetPosition(new Rect(position.size.x - 210f, 30f, 200f, 140f)));
    }

    private void ConstructGraphView()
    {
        _graphView = new DialogueGraphView(position.size / 2 - Vector2.right * 100f);
        _graphView.name = "Dialogue Graph";
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void ConstructToolBar()
    {
        Toolbar toolBar = new Toolbar();

        TextField dialogueNameTextField = new TextField("Dialogue name");
        dialogueNameTextField.SetValueWithoutNotify(DialogueName);
        dialogueNameTextField.MarkDirtyRepaint();
        dialogueNameTextField.RegisterValueChangedCallback(evt =>
        {
            if (DatabaseManager.DoDialogueExist($"{evt.newValue}") || evt.newValue.Length > 12 || !Regex.IsMatch(evt.newValue, "^[a-z_]*$"))
            {
                ((TextField)evt.currentTarget).SetValueWithoutNotify(evt.previousValue);
                return;
            }

            GraphSaveUtility.DeleteSave(DialogueName);
            DialogueName = evt.newValue;
            DatabaseManager.CreateDatabase();
            DatabaseManager.Database.RemoveDialogueData(IDEncoder.Encode(evt.previousValue));
            DatabaseManager.Database.RemoveFilterData(IDEncoder.Encode(CharacterName), IDEncoder.Encode(evt.previousValue));
            DatabaseManager.Database.AddDialogueData(new DialogueDataContainer(evt.newValue, FirstSentenceName));
            DatabaseManager.Database.AddFilterData(new FilterDataContainer(CharacterName, evt.newValue, new List<QueryValueContainer>()));
            ((TextField)evt.currentTarget).SetValueWithoutNotify(evt.newValue);
        });
        toolBar.Add(dialogueNameTextField);

        TextField characterNameTextField = new TextField("Character name");
        characterNameTextField.SetValueWithoutNotify(CharacterName);
        characterNameTextField.MarkDirtyRepaint();
        characterNameTextField.RegisterValueChangedCallback(evt =>
        {
            if (DatabaseManager.DoFilterExist(evt.newValue, DialogueName) || evt.newValue.Length > 12 || !Regex.IsMatch(evt.newValue, "^[a-z_]*$") || evt.newValue.Length == 0)
            {
                ((TextField)evt.currentTarget).SetValueWithoutNotify(evt.previousValue);
                return;
            }

            DatabaseManager.CreateDatabase();
            FilterDataContainer data = DatabaseManager.Database.GetFilterData(IDEncoder.Encode(evt.previousValue), IDEncoder.Encode(DialogueName));
            data.CharacterID = evt.newValue;
            DatabaseManager.Database.RemoveFilterData(IDEncoder.Encode(evt.previousValue), IDEncoder.Encode(DialogueName));
            DatabaseManager.Database.AddFilterData(data);
            CharacterName = evt.newValue;
        });
        toolBar.Add(characterNameTextField);

        Button nodeCreationButton = new Button(() =>
        {
            _graphView.AddNodeToGraphView("node_name", position.size / 2, false);
        });
        nodeCreationButton.text = "Create dialogue node";
        toolBar.Add(nodeCreationButton);

        Button choiceCreationButton = new Button(() =>
        {
            _graphView.AddNodeToGraphView("choice_name", position.size / 2, true);
        });
        choiceCreationButton.text = "Create choice node";
        toolBar.Add(choiceCreationButton);

        rootVisualElement.Add(toolBar);
    }

    private void RequestData(bool save)
    {
        if (string.IsNullOrEmpty(DialogueName))
        {
            EditorUtility.DisplayDialog("Invalid dialogue name", "Enter a valid dialogue name", "OK");
            return;
        }

        GraphSaveUtility graphSaveUtility = new GraphSaveUtility(_graphView);
        if (save)
            graphSaveUtility.SaveGraph(DialogueName);
        else
            graphSaveUtility.LoadGraph(DialogueName);
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
        DatabaseManager.CloseDatabase();
        DialogueTableEditor.Open();
        RequestData(true);
    }
}

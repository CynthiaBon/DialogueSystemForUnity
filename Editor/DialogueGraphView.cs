using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(200f, 150f);

    public static DialogueGraphView Instance = null;

    public DialogueGraphView(Vector2 entryPointPosition)
    {
        Instance = this;

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        AddElement(GenerateEntryPointNode(entryPointPosition));

        graphViewChanged = OnGraphChange;
    }

    private GraphViewChange OnGraphChange(GraphViewChange change)
    {
        if (change.elementsToRemove != null)
        {
            change.elementsToRemove.ForEach(element =>
            {
                if (element.GetType() == typeof(Edge))
                {
                    Edge destroyedEdge = element as Edge;
                    DialogueNode firstNode = destroyedEdge.output.node as DialogueNode;
                    DatabaseManager.CreateDatabase();
                    SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(firstNode.ID);
                    if (data == null)
                        return;

                    if (data.ChoiceIDs.Count == 0)
                    {
                        data.NextSentenceID = "";
                    }
                    else if (firstNode.OutputPortsCount == data.ChoiceIDs.Count)
                    {
                        DialogueNode secondNode = destroyedEdge.input.node as DialogueNode;
                        data.ChoiceIDs.Remove(IDEncoder.Decode(secondNode.ID));
                        data.ChoiceIDs.Add("");
                    }
                    DatabaseManager.Database.AddSentenceData(data);
                }
                else if (element.GetType() == typeof(DialogueNode))
                {
                    DialogueNode node = element as DialogueNode;
                    if (node.IsFirstSentence())
                    {
                        DatabaseManager.CreateDatabase();
                        DatabaseManager.Database.AddDialogueData(new DialogueDataContainer(DialogueGraph.DialogueName, ""));
                    }
                    node.RemoveAllPorts();
                    DatabaseManager.CreateDatabase();
                    DatabaseManager.Database.RemoveSentenceData(node.ID);
                }
            });
        }

        return change;
    }

    private DialogueNode GenerateEntryPointNode(Vector2 entryPointPosition)
    {
        DialogueNode node = new DialogueNode("start", "ENTRYPOINT", false, false, false, true);
        node.SetPosition(new Rect(entryPointPosition, new Vector2(100f, 150f)));
        node.GeneratePort("Next", Direction.Output);

        return node;
    }

    public void AddNodeToGraphView(string nodeName, Vector2 position, bool isChoice)
    {
        AddElement(CreateDialogueNode(nodeName, position, true, isChoice));
    }

    public DialogueNode CreateDialogueNode(string nodeName, Vector2 position, bool checkValidity, bool isChoice)
    {
        DialogueNode node = new DialogueNode(nodeName, "", true, checkValidity, isChoice, false);
        node.GeneratePort("Input", Direction.Input, Port.Capacity.Multi);
        node.SetPosition(new Rect(position, DefaultNodeSize));
        return node;
    }

    public void AddQueryToGraphView(int index, BlackboardSection worldSection, BlackboardSection characterSection)
    {
        if (index == 0)
            AddWorldQuery(new WorldDataContainer("v_world", 0), worldSection, true);
        else if (index == 1)
            AddCharacterQuery(new CharacterDataContainer("n_char", "v_char", 0), characterSection, true);
    }

    public void AddFilterToGraphView(QueryValueContainer queryValue, BlackboardSection filterSection)
    {
        if (queryValue.Type == "World")
            AddFilterQuery(new WorldDataContainer(queryValue.Name, queryValue.Value), filterSection, true);
        else
            AddFilterQuery(new CharacterDataContainer(queryValue.Type, queryValue.Name, queryValue.Value), filterSection, true);
    }

    public void AddFilterQuery(WorldDataContainer worldDataContainer, BlackboardSection filterSection, bool checkValidity)
    {
        VisualElement container = new VisualElement();
        container.name = "FilterSection";
        BlackboardField blackboardField = new BlackboardField();
        string validId = worldDataContainer.ID;
        blackboardField.text = validId;
        blackboardField.name = "FilterWorld";
        blackboardField.typeText = "World filter variable";

        container.Add(blackboardField);

        TextField worldVariableValue = new TextField("Value:");
        worldVariableValue.name = "WorldFilterValue";
        worldVariableValue.value = worldDataContainer.Value.ToString();
        worldVariableValue.isReadOnly = true;

        TextField deleteButton = new TextField();
        deleteButton.SetValueWithoutNotify("Delete");
        deleteButton.isReadOnly = true;
        deleteButton.RegisterCallback<FocusEvent>(evt =>
        {
            bool validation = ValidationWindow.Open("Delete ?");
            if (!validation)
                return;

            BlackboardField parentField = QInParents<BlackboardField>((VisualElement)evt.target, null);

            if (parentField != null)
            {
                DatabaseManager.CreateDatabase();
                FilterDataContainer data = DatabaseManager.Database.GetFilterData(IDEncoder.Encode(DialogueGraph.CharacterName), IDEncoder.Encode(DialogueGraph.DialogueName));
                QueryValueContainer queryValue = data.Query.Find(d => d.Type == "World" && d.Name == parentField.text);
                data.Query.Remove(queryValue);
                DatabaseManager.Database.AddFilterData(data);
                filterSection.Remove(QInParents<VisualElement>(parentField, "FilterSection"));
            }
        });

        VisualElement values = new VisualElement();
        values.Add(worldVariableValue);
        values.Add(deleteButton);
        BlackboardRow blackboardRow = new BlackboardRow(blackboardField, values);

        container.Add(blackboardRow);
        filterSection.Add(container);

        if (checkValidity)
        {
            DatabaseManager.CreateDatabase();
            FilterDataContainer data = DatabaseManager.Database.GetFilterData(IDEncoder.Encode(DialogueGraph.CharacterName), IDEncoder.Encode(DialogueGraph.DialogueName));
            data.Query.Add(new QueryValueContainer("World", validId, worldDataContainer.Value));
            DatabaseManager.Database.AddFilterData(data);
        }
    }

    public void AddFilterQuery(CharacterDataContainer characterDataContainer, BlackboardSection filterSection, bool checkValidity)
    {
        VisualElement container = new VisualElement();
        container.name = "FilterSection";
        BlackboardField blackboardField = new BlackboardField();
        blackboardField.text = characterDataContainer.ID;
        blackboardField.name = "FilterCharacters";
        blackboardField.typeText = "Character filter variable";
        container.Add(blackboardField);

        TextField characterVariableValue = new TextField("Value:");
        characterVariableValue.name = "CharacterFilterValue";
        characterVariableValue.value = characterDataContainer.Value.ToString();
        characterVariableValue.isReadOnly = true;

        TextField characterName = new TextField("Character name:");
        characterName.value = characterDataContainer.CharacterID;
        characterName.name = "CharacterID";
        characterName.isReadOnly = true;

        TextField deleteButton = new TextField();
        deleteButton.SetValueWithoutNotify("Delete");
        deleteButton.isReadOnly = true;
        deleteButton.RegisterCallback<FocusEvent>(evt =>
        {
            bool validation = ValidationWindow.Open("Delete ?");
            if (!validation)
                return;

            BlackboardField parentField = QInParents<BlackboardField>((VisualElement)evt.target, null);
            TextField characterNameField = QInChildren<TextField>(((VisualElement)evt.target).parent, "CharacterID");

            if (parentField != null && characterNameField != null)
            {
                DatabaseManager.CreateDatabase();
                FilterDataContainer data = DatabaseManager.Database.GetFilterData(IDEncoder.Encode(DialogueGraph.CharacterName), IDEncoder.Encode(DialogueGraph.DialogueName));
                QueryValueContainer queryValue = data.Query.Find(q => q.Type == characterNameField.text && q.Name == parentField.text);
                data.Query.Remove(queryValue);
                DatabaseManager.Database.AddFilterData(data);
                filterSection.Remove(QInParents<VisualElement>(parentField, "FilterSection"));
            }
        });

        VisualElement values = new VisualElement();
        values.Add(characterName);
        values.Add(characterVariableValue);
        values.Add(deleteButton);
        BlackboardRow blackboardRow = new BlackboardRow(blackboardField, values);

        container.Add(blackboardRow);
        filterSection.Add(container);

        if (checkValidity)
        {
            DatabaseManager.CreateDatabase();
            FilterDataContainer data = DatabaseManager.Database.GetFilterData(IDEncoder.Encode(DialogueGraph.CharacterName), IDEncoder.Encode(DialogueGraph.DialogueName));
            data.Query.Add(new QueryValueContainer(characterDataContainer.CharacterID, characterDataContainer.ID, characterDataContainer.Value));
            DatabaseManager.Database.AddFilterData(data);
        }
    }

    public void AddWorldQuery(WorldDataContainer worldDataContainer, BlackboardSection blackboardSection, bool checkValidity)
    {
        VisualElement container = new VisualElement();
        container.name = "WorldSection";
        BlackboardField blackboardField = new BlackboardField();
        string validId = checkValidity ? GetValidID("World:", worldDataContainer.ID) : worldDataContainer.ID;
        blackboardField.text = validId;
        blackboardField.name = "World";
        blackboardField.typeText = "World variable";

        container.Add(blackboardField);

        TextField worldVariableValue = new TextField("Value:");
        worldVariableValue.name = "WorldValue";
        worldVariableValue.value = worldDataContainer.Value.ToString();
        worldVariableValue.RegisterValueChangedCallback(evt =>
        {
            int result = 0;
            if (!int.TryParse(evt.newValue, out result))
            {
                ((TextField)evt.currentTarget).SetValueWithoutNotify(evt.previousValue);
            }
            else
            {
                BlackboardField parentField = null;
                VisualElement parent = ((VisualElement)evt.target).parent;
                while (parent != null && parentField == null)
                {
                    parentField = parent.Q<BlackboardField>("World");
                    parent = parent.parent;
                }

                if (parentField != null)
                    DatabaseManager.SetQueryVariable($"World:{parentField.text}:{result}");
            }
        });

        TextField deleteButton = new TextField();
        deleteButton.SetValueWithoutNotify("Delete");
        deleteButton.isReadOnly = true;
        deleteButton.RegisterCallback<FocusEvent>(evt =>
        {
            bool validation = ValidationWindow.Open("Delete ?");
            if (!validation)
                return;

            BlackboardField parentField = QInParents<BlackboardField>((VisualElement)evt.target, null);

            if (parentField != null)
            {
                DatabaseManager.CreateDatabase();
                DatabaseManager.Database.RemoveWorldData(IDEncoder.Encode(parentField.text));
                blackboardSection.Remove(QInParents<VisualElement>(parentField, "WorldSection"));
            }
        });

        VisualElement values = new VisualElement();
        values.Add(worldVariableValue);
        values.Add(deleteButton);
        BlackboardRow blackboardRow = new BlackboardRow(blackboardField, values);

        container.Add(blackboardRow);
        blackboardSection.Add(container);

        if (checkValidity)
            DatabaseManager.SetQueryVariable("World:" + validId + ":0");
    }

    public void AddCharacterQuery(CharacterDataContainer characterDataContainer, BlackboardSection blackboardSection, bool checkValidity)
    {
        VisualElement container = new VisualElement();
        container.name = "CharacterSection";
        BlackboardField blackboardField = new BlackboardField();
        blackboardField.text = checkValidity ? GetValidID("Characters:" + characterDataContainer.CharacterID + ".", characterDataContainer.ID) : characterDataContainer.ID;
        blackboardField.name = "Characters";
        blackboardField.typeText = "Character variable";
        container.Add(blackboardField);

        TextField characterVariableValue = new TextField("Value:");
        characterVariableValue.name = "CharacterValue";
        characterVariableValue.value = characterDataContainer.Value.ToString();
        characterVariableValue.RegisterValueChangedCallback(evt =>
        {
            int result = 0;
            if (!int.TryParse(evt.newValue, out result))
            {
                ((TextField)evt.currentTarget).SetValueWithoutNotify(evt.previousValue);
                return;
            }

            BlackboardField parentField = null;
            VisualElement parent = ((VisualElement)evt.target).parent;
            TextField characterIdField = parent.Q<TextField>("CharacterID");

            while (parent != null && parentField == null)
            {
                parentField = parent.Q<BlackboardField>("Characters");
                parent = parent.parent;
            }

            if (parentField != null && characterIdField != null)
            {
                DatabaseManager.SetQueryVariable($"Characters:{characterIdField.text}.{parentField.text}:{result}");
            }
        });

        TextField characterName = new TextField("Character name:");
        characterName.value = characterDataContainer.CharacterID;
        characterName.name = "CharacterID";
        characterName.RegisterValueChangedCallback(evt =>
        {
            BlackboardField parentField = null;
            VisualElement parent = ((VisualElement)evt.target).parent;

            while (parent != null && parentField == null)
            {
                parentField = parent.Q<BlackboardField>("Characters");
                parent = parent.parent;
            }

            if (parent == null || DatabaseManager.DoQueryVariableExist($"Characters:{evt.newValue}.{parentField.text}") || evt.newValue.Length > 12 || !Regex.IsMatch(evt.newValue, "^[a-z_]*$"))
            {
                ((TextField)evt.currentTarget).SetValueWithoutNotify(evt.previousValue);
                return;
            }

            int queryValue = DatabaseManager.GetValue($"Characters:{evt.previousValue}.{parentField.text}");
            DatabaseManager.RemoveQueryVariable($"Characters:{evt.previousValue}.{parentField.text}");
            DatabaseManager.SetQueryVariable($"Characters:{evt.newValue}.{parentField.text}:{queryValue}");
        });

        TextField deleteButton = new TextField();
        deleteButton.SetValueWithoutNotify("Delete");
        deleteButton.isReadOnly = true;
        deleteButton.RegisterCallback<FocusEvent>(evt =>
        {
            bool validation = ValidationWindow.Open("Delete ?");
            if (!validation)
                return;

            BlackboardField parentField = QInParents<BlackboardField>((VisualElement)evt.target, null);
            TextField characterNameField = QInChildren<TextField>(((VisualElement)evt.target).parent, "CharacterID");

            if (parentField != null && characterNameField != null)
            {
                DatabaseManager.CreateDatabase();
                DatabaseManager.Database.RemoveCharacterData(IDEncoder.Encode(characterNameField.text), IDEncoder.Encode(parentField.text));
                blackboardSection.Remove(QInParents<VisualElement>(parentField, "CharacterSection"));
            }
        });

        VisualElement values = new VisualElement();
        values.Add(characterName);
        values.Add(characterVariableValue);
        values.Add(deleteButton);
        BlackboardRow blackboardRow = new BlackboardRow(blackboardField, values);

        container.Add(blackboardRow);
        blackboardSection.Add(container);

        if (checkValidity)
            DatabaseManager.SetQueryVariable($"Characters:{characterDataContainer.CharacterID}.{characterDataContainer.ID}:{characterDataContainer.Value}");
    }

    private string GetValidID(string type, string baseId)
    {
        if (!DatabaseManager.DoQueryVariableExist(type + baseId))
            return baseId;

        int i = 1;
        string newId = baseId + "a";
        if (newId.Length > 12)
        {
            baseId = baseId.Substring(0, 11);
            newId = baseId + "a";
        }

        while (DatabaseManager.DoQueryVariableExist(type + newId))
        {
            newId = baseId + (char)(i + 97);
            if (newId.Length > 12)
            {
                baseId = baseId.Substring(0, 12 - (int)(i / 10) + 1);
                newId = baseId + (char)(i + 97);
            }
            i += 1;
        }
        return newId;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        List<Port> compatiblePorts = new List<Port>();

        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node)
                compatiblePorts.Add(port);
        });

        return compatiblePorts;
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
}
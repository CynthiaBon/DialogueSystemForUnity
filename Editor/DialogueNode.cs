using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor.UI;
using UnityEngine.UIElements;
using System.Linq;
using System.Text.RegularExpressions;

public class DialogueNode : Node
{
    public ulong ID = 0;
    public string DialogueText = null;
    public bool EntryPoint = false;
    public bool IsChoice = false;
    public int OutputPortsCount { get; private set; } = 0;

    private string _currentText = "";
    private TextField _textField = null;
    private VisualElement _listContainer = null;
    private Port _inputPort = null;
    private List<Port> _outputPorts = null;

    public DialogueNode(string nodeName, string dialogueText, bool choiceButton, bool checkValidity, bool isChoice, bool entryPoint = false)
    {
        IsChoice = isChoice;
        _outputPorts = new List<Port>();
        nodeName = checkValidity ? GetValidID(nodeName) : nodeName;
        if (checkValidity)
        {
            DatabaseManager.CreateDatabase();
            SentenceDataContainer data = new SentenceDataContainer(nodeName, "", "", false, new List<string>(), new List<EventMethodContainer>(), new List<EventMethodContainer>(), "", new List<QueryValueContainer>());
            DatabaseManager.Database.AddSentenceData(data);
        }

        title = "";
        DialogueText = dialogueText;
        EntryPoint = entryPoint;
        ID = IDEncoder.Encode(nodeName);

        TextField nodeNameField = new TextField();
        nodeNameField.RegisterValueChangedCallback(evt =>
        {
            string pattern = "^[a-z_]*$";
            if (evt.newValue.Length <= 12 && (Regex.IsMatch(evt.newValue, pattern) || evt.newValue.Length == 0))
            {
                DatabaseManager.CreateDatabase();
                if (IsFirstSentence())
                {
                    DialogueGraph.FirstSentenceName = evt.newValue;
                    DatabaseManager.Database.AddDialogueData(new DialogueDataContainer(DialogueGraph.DialogueName, evt.newValue));
                }
                else
                {
                    Edge edge = _inputPort.connections.FirstOrDefault();
                    if (edge == null)
                        return;

                    DialogueNode node = edge.output.node as DialogueNode;
                    node.ChangePortConnectionName(evt.previousValue, evt.newValue);
                }
                SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
                data.ID = evt.newValue;
                DatabaseManager.Database.RemoveSentenceData(ID);
                DatabaseManager.Database.AddSentenceData(data);
                ID = IDEncoder.Encode(evt.newValue);
            }
            else
            {
                ((TextField)evt.currentTarget).value = evt.previousValue;
            }
        });
        nodeNameField.SetValueWithoutNotify(nodeName);
        titleContainer.Add(nodeNameField);

        if (choiceButton && !isChoice)
        {
            Button button = new Button(() => AddChoicePort(true));
            button.text = "Add choice";
            titleContainer.Add(button);
        }

        if (!entryPoint)
        {
            if (!isChoice)
            {
                TextField speakerField = new TextField();
                speakerField.label = "Speaker:";
                speakerField.RegisterValueChangedCallback(evt =>
                {
                    DatabaseManager.CreateDatabase();
                    SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
                    data.Speaker = evt.newValue;
                    DatabaseManager.Database.AddSentenceData(data);
                });
                if (checkValidity)
                {
                    speakerField.SetValueWithoutNotify("");
                }
                else
                {
                    DatabaseManager.CreateDatabase();
                    SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
                    if (data != null)
                        speakerField.SetValueWithoutNotify(data.Speaker);
                }
                contentContainer.Add(speakerField);
            }

            _textField = new TextField();
            _textField.isReadOnly = true;
            _textField.RegisterCallback<FocusEvent>(_ => OpenWritingWindow());
            _textField.SetValueWithoutNotify("");
            _textField.multiline = true;
            contentContainer.Add(_textField);

            if (checkValidity)
                AddChoicePort(true);

            RefreshExpandedState();
        }
        else
        {
            nodeNameField.isReadOnly = true;
        }

        if (!entryPoint)
        {
            _listContainer = new VisualElement();
            contentContainer.Add(_listContainer);
        }

        if (!isChoice && !entryPoint)
        {
            Button addEndEventButton = new Button(() => OpenEventWindow());
            addEndEventButton.text = "Add end event";
            contentContainer.Add(addEndEventButton);
        }
        else if (isChoice && !entryPoint)
        {
            Button addQueryButton = new Button(() => OpenQueryWindow());
            addQueryButton.text = "Add query";
            contentContainer.Add(addQueryButton);
        }
    }

    private string GetValidID(string id)
    {
        if (!DatabaseManager.DoSentenceExist(id))
            return id;

        int i = 1;
        string newId = id + "a";
        if (newId.Length > 12)
        {
            id = id.Substring(0, 11);
            newId = id + "a";
        }

        while (DatabaseManager.DoSentenceExist(newId))
        {
            newId = id + (char)(i + 97);
            if (newId.Length > 12)
            {
                id = id.Substring(0, 12 - (int)(i / 10) + 1);
                newId = id + (char)(i + 97);
            }
            i += 1;
        }
        return newId;
    }

    public void SetText(string text)
    {
        DialogueText = text;
        _textField.SetValueWithoutNotify(GetTextSummary(text));
    }

    private void OpenWritingWindow()
    {
        string text = WritingWindow.Open(DialogueText);
        SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
        if (data != null)
            data.Text = text;
        DatabaseManager.Database.AddSentenceData(data);
        _textField.SetValueWithoutNotify(GetTextSummary(text));
        DialogueText = text;
    }

    private string GetTextSummary(string text)
    {
        if (text.Length <= 20)
            return text;
        return text.Substring(0, 17) + "...";
    }

    private void OpenEventWindow()
    {
        EventMethodContainer eventMethod = EventWindow.Open();
        if (eventMethod == null)
            return;

        AddEndEvent(eventMethod, true);
    }

    public void AddEndEvent(EventMethodContainer eventMethod, bool addToDatabase)
    {
        if (addToDatabase)
        {
            DatabaseManager.CreateDatabase();
            SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
            data.OnEndEvents.Add(eventMethod);
            DatabaseManager.Database.AddSentenceData(data);
        }

        TextField eventField = new TextField();
        eventField.name = "EventField";
        eventField.SetValueWithoutNotify($"{eventMethod.MethodName}({eventMethod.Argument})");
        eventField.isReadOnly = true;

        VisualElement container = new VisualElement();
        container.name = "EndEvent";

        Button deleteButton = new Button(() =>
        {
            DatabaseManager.CreateDatabase();
            SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
            EventMethodContainer ev = data.OnEndEvents.Find(e => e.MethodName == eventMethod.MethodName && e.Argument == eventMethod.Argument);
            data.OnEndEvents.Remove(ev);
            DatabaseManager.Database.AddSentenceData(data);
            _listContainer.Remove(container);
        });
        deleteButton.text = "X";

        container.Add(eventField);
        container.Add(deleteButton);

        _listContainer.Add(container);
        RefreshExpandedState();
    }

    private void OpenQueryWindow()
    {
        QueryValueContainer queryValue = QueryValueWindow.Open();
        if (queryValue == null)
            return;

        AddQueryToChoice(queryValue, true);
    }

    public void AddQueryToChoice(QueryValueContainer queryValue, bool addToDatabase)
    {
        if (addToDatabase)
        {
            DatabaseManager.CreateDatabase();
            SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
            data.Query.Add(queryValue);
            DatabaseManager.Database.AddSentenceData(data);
        }

        TextField eventField = new TextField();
        eventField.name = "QueryField";
        eventField.SetValueWithoutNotify($"{queryValue.Type}.{queryValue.Name} : {queryValue.Value}");
        eventField.isReadOnly = true;

        VisualElement container = new VisualElement();
        container.name = "Query";

        Button deleteButton = new Button(() =>
        {
            DatabaseManager.CreateDatabase();
            SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
            QueryValueContainer query = data.Query.Find(q => q.Type == queryValue.Type && q.Name == queryValue.Name);
            data.Query.Remove(query);
            DatabaseManager.Database.AddSentenceData(data);
            _listContainer.Remove(container);
        });
        deleteButton.text = "X";

        container.Add(eventField);
        container.Add(deleteButton);

        _listContainer.Add(container);
        RefreshExpandedState();
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

    public void AddChoicePort(bool updateDatabase, string name = "")
    {
        Port port = GeneratePort(name != "" ? name : $"Choice {OutputPortsCount}", Direction.Output);
        OutputPortsCount += 1;
        if (updateDatabase && OutputPortsCount > 1)
        {
            DatabaseManager.CreateDatabase();
            SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
            if (OutputPortsCount == 2)
            {
                data.ChoiceIDs.Add(data.NextSentenceID);
                data.NextSentenceID = "";
                data.HasChoice = true;
            }
            data.ChoiceIDs.Add("");
            DatabaseManager.Database.AddSentenceData(data);
        }
        _outputPorts.Add(port);
    }

    public Port GeneratePort(string portName, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        Port port = InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
        port.portName = portName;
        port.AddManipulator(new EdgeConnector<Edge>(new MyIEdgeConnectorListener()));

        if (!EntryPoint && portDirection == Direction.Output)
        {
            Label oldLabel = port.contentContainer.Q<Label>("type");
            port.contentContainer.Remove(oldLabel);

            Label newLabel = new Label("  ");
            port.contentContainer.Add(newLabel);

            TextField textField = new TextField();
            textField.value = portName;
            textField.RegisterValueChangedCallback(evt => port.portName = evt.newValue);
            port.contentContainer.Add(textField);

            Button deleteButton = new Button(() => RemovePort(port));
            deleteButton.text = "X";
            port.contentContainer.Add(deleteButton);
        }

        if (portDirection == Direction.Output)
        {
            outputContainer.Add(port);
        }
        else
        {
            inputContainer.Add(port);
            _inputPort = port;
        }

        RefreshExpandedState();
        RefreshPorts();

        return port;
    }

    private void RemovePort(Port port)
    {
        OutputPortsCount -= 1;
        List<Edge> edges = port.connections.ToList();
        Edge edge = null;
        if (edges.Any())
            edge = edges.First();

        DatabaseManager.CreateDatabase();
        SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);

        if (edge != null)
        {
            DialogueNode node = edge.input.node as DialogueNode;
            if (data != null)
                data.ChoiceIDs.Remove(IDEncoder.Decode(node.ID));
        }
        else
        {
            if (data != null)
                data.ChoiceIDs.Remove("");
        }

        if (OutputPortsCount == 1)
        {
            if (data != null)
            {
                data.NextSentenceID = data.ChoiceIDs.FirstOrDefault();
                data.ChoiceIDs = new List<string>();
                data.HasChoice = false;
            }
        }
        if (data != null)
            DatabaseManager.Database.AddSentenceData(data);

        if (edge != null)
        {
            edge.input.Disconnect(edge);
            DialogueGraphView.Instance.RemoveElement(edge);
        }

        _outputPorts.Remove(port);
        outputContainer.Remove(port);
        RefreshExpandedState();
        RefreshPorts();
    }

    public void ChangePortConnectionName(string oldName, string newName)
    {
        DatabaseManager.CreateDatabase();
        SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(ID);
        if (!data.HasChoice)
        {
            data.NextSentenceID = newName;
        }
        else
        {
            data.ChoiceIDs.Remove(oldName);
            data.ChoiceIDs.Add(newName);
        }
        DatabaseManager.Database.AddSentenceData(data);
    }

    public bool IsFirstSentence()
    {
        Edge edge = _inputPort.connections.FirstOrDefault();
        if (edge != null)
        {
            Port outputPort = edge.output;
            return outputPort.portName == "Next";
        }
        return false;
    }

    public void RemoveAllPorts()
    {
        for (int i = 0; i < _outputPorts.Count; i++)
        {
            RemovePort(_outputPorts[i]);
        }
        _outputPorts = new List<Port>();
    }
}

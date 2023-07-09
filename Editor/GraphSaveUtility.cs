using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private DialogueGraphView _dialogueGraphView = null;
    private DialogueContainer _dialogueContainer = null;

    private List<Edge> _edges = null;
    private List<DialogueNode> _dialogueNodes = null;

    public GraphSaveUtility(DialogueGraphView dialogueGraphView)
    {
        _dialogueGraphView = dialogueGraphView;
        _edges = _dialogueGraphView.edges.ToList();
        _dialogueNodes = _dialogueGraphView.nodes.ToList().Cast<DialogueNode>().ToList();
    }

    public void SaveGraph(string dialogueName)
    {
        DialogueContainer dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();
        dialogueContainer.DialogueNodesData = new List<DialogueNodeData>();
        dialogueContainer.NodeLinksData = new List<NodeLinkData>();

        if (_edges.Any())
        {
            List<Edge> connectedPorts = _edges.Where(edge => edge.input.node != null).ToList();
            connectedPorts.ForEach(connectedPort =>
            {
                DialogueNode outputNode = connectedPort.output.node as DialogueNode;
                DialogueNode inputNode = connectedPort.input.node as DialogueNode;
                dialogueContainer.NodeLinksData.Add(new NodeLinkData(outputNode.ID, connectedPort.output.portName, inputNode.ID));
            });
        }

        List<DialogueNode> dialogueNodes = _dialogueNodes.Where(node => !node.EntryPoint).ToList();
        dialogueNodes.ForEach(node =>
        {
            dialogueContainer.DialogueNodesData.Add(new DialogueNodeData(node.ID, IDEncoder.Decode(node.ID), node.DialogueText, node.IsChoice, node.GetPosition().position));
        });

        if (!AssetDatabase.IsValidFolder("Assets/Scripts/DialogueSystem/Resources"))
            AssetDatabase.CreateFolder("Assets/Scripts/DialogueSystem", "Resources");

        AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Scripts/DialogueSystem/Resources/{dialogueName}.asset");
    }

    public static DialogueContainer GetSave(string dialogueName)
    {
        return Resources.Load<DialogueContainer>(dialogueName);
    }

    public static void DeleteSave(string dialogueName)
    {
        DialogueContainer _dialogueContainer = Resources.Load<DialogueContainer>(dialogueName);
        if (_dialogueContainer != null)
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_dialogueContainer));
    }

    public void LoadGraph(string dialogueName)
    {
        _dialogueContainer = Resources.Load<DialogueContainer>(dialogueName);
        if (_dialogueContainer == null)
            return;

        ClearGraph();
        CreateNodes();
        ConnectNodes();
    }

    private void ClearGraph()
    {
        if (_dialogueContainer.NodeLinksData == null || _dialogueContainer.NodeLinksData.Count == 0)
            return;

        _dialogueNodes.Find(node => node.EntryPoint).ID = _dialogueContainer.NodeLinksData.First(link => link.PortName == "Next").BaseNodeID;

        _dialogueNodes.ForEach(node =>
        {
            if (!node.EntryPoint)
            {
                List<Edge> edges = _edges.Where(edge => edge.input.node == node).ToList();
                edges.ForEach(edge => _dialogueGraphView.RemoveElement(edge));
                _dialogueGraphView.RemoveElement(node);
            }
        });
    }

    private void CreateNodes()
    {
        DatabaseManager.CreateDatabase();
        _dialogueContainer.DialogueNodesData.ForEach(node =>
        {
            DialogueNode newNode = _dialogueGraphView.CreateDialogueNode(node.Title, Vector2.zero, false, node.IsChoice);
            newNode.ID = node.ID;
            newNode.SetText(node.Text);
            _dialogueGraphView.AddElement(newNode);

            List<NodeLinkData> ports = _dialogueContainer.NodeLinksData.Where(link => link.BaseNodeID == node.ID).ToList();
            if (ports != null && ports.Count != 0)
                ports.ForEach(port => newNode.AddChoicePort(false, port.PortName));
            else
                newNode.AddChoicePort(false);
            SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(node.ID);
            if (!node.IsChoice && data != null && data.OnEndEvents != null)
            {
                data.OnEndEvents.ForEach(eventMethod => newNode.AddEndEvent(eventMethod, false));
            }
            else if (node.IsChoice && data != null && data.Query != null)
            {
                data.Query.ForEach(query => newNode.AddQueryToChoice(query, false));
            }
            newNode.SetPosition(new Rect(_dialogueContainer.DialogueNodesData.First(n => n.ID == node.ID).Position, _dialogueGraphView.DefaultNodeSize));
        });


        _dialogueNodes = _dialogueGraphView.nodes.ToList().Cast<DialogueNode>().ToList();
    }

    private void ConnectNodes()
    {
        for (int i = 0; i < _dialogueNodes.Count; i++)
        {
            List<NodeLinkData> connections = _dialogueContainer.NodeLinksData.Where(link => link.BaseNodeID == _dialogueNodes[i].ID).ToList();
            for (int y = 0; y < connections.Count; y++)
            {
                ulong targetNodeGuid = connections[y].TargetNodeID;
                DialogueNode targetNode = _dialogueNodes.First(node => node.ID == targetNodeGuid);
                LinkNodes(_dialogueNodes[i].outputContainer[y].Q<Port>(), (Port)targetNode.inputContainer[0]);
            }
        }
    }

    private void LinkNodes(Port output, Port input)
    {
        Edge newEdge = new Edge();
        newEdge.output = output;
        newEdge.input = input;

        newEdge.input.Connect(newEdge);
        newEdge.output.Connect(newEdge);

        _dialogueGraphView.Add(newEdge);
    }
}

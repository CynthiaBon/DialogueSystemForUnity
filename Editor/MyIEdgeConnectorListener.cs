using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class MyIEdgeConnectorListener : IEdgeConnectorListener
{
    public void OnDrop(GraphView graphView, Edge edge)
    {
        DatabaseManager.CreateDatabase();
        if (edge.output.portName == "Next")
        {
            DialogueNode firstNode = edge.input.node as DialogueNode;
            DialogueGraph.FirstSentenceName = IDEncoder.Decode(firstNode.ID);
            DatabaseManager.Database.AddDialogueData(new DialogueDataContainer(DialogueGraph.DialogueName, IDEncoder.Decode(firstNode.ID)));
        }
        else if (!DialogueGraph.IsLoading)
        {
            DialogueNode firstNode = edge.output.node as DialogueNode;
            SentenceDataContainer data = DatabaseManager.Database.GetSentenceData(firstNode.ID);
            if (data == null)
                return;
            DialogueNode secondetNode = edge.input.node as DialogueNode;
            if (firstNode.OutputPortsCount == 1)
            {
                data.NextSentenceID = IDEncoder.Decode(secondetNode.ID);
            }
            else
            {
                data.ChoiceIDs.Remove("");
                data.ChoiceIDs.Add(IDEncoder.Decode(secondetNode.ID));
            }
            DatabaseManager.Database.AddSentenceData(data);
        }
    }

    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
    }
}

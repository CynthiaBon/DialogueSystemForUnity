using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class NodeLinkData
{
    public NodeLinkData(ulong nodeBaseId, string portName, ulong targetNodeId)
    {
        BaseNodeID = nodeBaseId;
        PortName = portName;
        TargetNodeID = targetNodeId;
    }

    public ulong BaseNodeID = 0;
    public string PortName = null;
    public ulong TargetNodeID = 0;
}

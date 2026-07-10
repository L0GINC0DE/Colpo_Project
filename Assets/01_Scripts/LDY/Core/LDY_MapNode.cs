using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LDY_MapNode
{
    public string id;
    public Vector2 position;
    public bool isPlayerBase;
    public List<string> connectedNodeIds = new List<string>();
}

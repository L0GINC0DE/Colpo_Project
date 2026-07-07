using System.Collections.Generic;
using UnityEngine;

public class GraphMapSetup : MonoBehaviour
{
    public static GraphMapSetup Instance { get; private set; }

    [SerializeField] private List<MapNode> nodes = new List<MapNode>();

    public AStarPathfinder Pathfinder { get; private set; }
    public Dictionary<string, MapNode> Nodes { get; private set; }
    
    public Dictionary<string, Renderer> NodeRenderers { get; private set; }

    public Transform TrailContainer { get; private set; }
    
    public List<MapNode> NodeList => nodes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Nodes = new Dictionary<string, MapNode>();
        foreach (MapNode node in nodes)
            Nodes[node.id] = node;

        Pathfinder = new AStarPathfinder(Nodes);

        BuildVisuals();
    }
    
    private void BuildVisuals()
    {
        Transform container = new GameObject("MapVisuals").transform;
        container.SetParent(transform, false);

        var drawnEdges = new HashSet<string>();
        foreach (MapNode node in nodes)
        {
            foreach (string neighborId in node.connectedNodeIds)
            {
                if (!Nodes.TryGetValue(neighborId, out MapNode neighbor))
                    continue;

                string key = string.CompareOrdinal(node.id, neighborId) < 0 ? $"{node.id}|{neighborId}" : $"{neighborId}|{node.id}";
                if (!drawnEdges.Add(key))
                    continue;

                MapVisualFactory.CreateEdgeLine($"Edge_{key}", container, node.position, neighbor.position, 0.05f, new Color(1f, 1f, 1f, 0.6f));
            }
        }

        NodeRenderers = new Dictionary<string, Renderer>();
        foreach (MapNode node in nodes)
        {
            Color color = node.isPlayerBase ? Color.cyan : Color.white;
            GameObject marker = MapVisualFactory.CreateMarker($"Node_{node.id}", container, node.position, 0.5f, color);
            NodeRenderers[node.id] = marker.GetComponent<Renderer>();
        }

        TrailContainer = new GameObject("Trails").transform;
        TrailContainer.SetParent(transform, false);
    }

    private void Start()
    {
        if (MapStateManager.Instance != null)
            MapStateManager.Instance.SetNodes(Nodes);
    }

    private void Reset()
    {
        nodes = BuildSampleMap();
    }

    private static List<MapNode> BuildSampleMap()
    {
        return new List<MapNode>
        {
            new MapNode { id = "base", position = new Vector2(0, 0), isPlayerBase = true, connectedNodeIds = new List<string> { "j1", "j2", "j3", "j4" } },

            new MapNode { id = "j1", position = new Vector2(0, 3), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j2", "j4", "gA" } },
            new MapNode { id = "j2", position = new Vector2(3, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j1", "j3", "gB" } },
            new MapNode { id = "j3", position = new Vector2(0, -3), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j2", "j4", "gC" } },
            new MapNode { id = "j4", position = new Vector2(-3, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j3", "j1", "gD" } },

            new MapNode { id = "gA", position = new Vector2(0, 6), isPlayerBase = false, connectedNodeIds = new List<string> { "j1" } },
            new MapNode { id = "gB", position = new Vector2(6, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "j2" } },
            new MapNode { id = "gC", position = new Vector2(0, -6), isPlayerBase = false, connectedNodeIds = new List<string> { "j3" } },
            new MapNode { id = "gD", position = new Vector2(-6, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "j4" } },
        };
    }
}

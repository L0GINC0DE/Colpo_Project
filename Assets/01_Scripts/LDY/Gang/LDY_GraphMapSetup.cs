using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class LDY_GraphMapSetup : MonoBehaviour
{
    public static LDY_GraphMapSetup Instance { get; private set; }

    [SerializeField] private List<LDY_MapNode> nodes = new List<LDY_MapNode>();

    public LDY_AStarPathfinder Pathfinder { get; private set; }
    public Dictionary<string, LDY_MapNode> Nodes { get; private set; }
    
    public Dictionary<string, Renderer> NodeRenderers { get; private set; }

    public Transform TrailContainer { get; private set; }

    // 벽으로 막힌 간선마다 도로를 가로지르는 바리케이드 선을 하나씩 심어두는 곳.
    public Transform WallContainer { get; private set; }
    public Dictionary<string, GameObject> WallMarkers { get; private set; }
    
    public List<LDY_MapNode> NodeList => nodes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Nodes = new Dictionary<string, LDY_MapNode>();
        foreach (LDY_MapNode node in nodes)
            Nodes[node.id] = node;

        Pathfinder = new LDY_AStarPathfinder(Nodes);

        BuildVisuals();
    }
    
    private void BuildVisuals()
    {
        Transform container = new GameObject("MapVisuals").transform;
        container.SetParent(transform, false);

        var drawnEdges = new HashSet<string>();
        foreach (LDY_MapNode node in nodes)
        {
            foreach (string neighborId in node.connectedNodeIds)
            {
                if (!Nodes.TryGetValue(neighborId, out LDY_MapNode neighbor))
                    continue;

                string key = string.CompareOrdinal(node.id, neighborId) < 0 ? $"{node.id}|{neighborId}" : $"{neighborId}|{node.id}";
                if (!drawnEdges.Add(key))
                    continue;

                LDY_MapVisualFactory.CreateClickableEdgeLine(
                    $"Edge_{key}", container, node.position, neighbor.position, 0.05f, new Color(1f, 1f, 1f, 0.6f), node.id, neighborId);
            }
        }

        NodeRenderers = new Dictionary<string, Renderer>();
        foreach (LDY_MapNode node in nodes)
        {
            Color color = node.isPlayerBase ? Color.cyan : Color.white;
            // keepCollider: true - PathRedirectHandler가 노드를 좌클릭으로 찍을 수 있어야 한다.
            GameObject marker = LDY_MapVisualFactory.CreateMarker($"Node_{node.id}", container, node.position, 0.5f, color, keepCollider: true);
            NodeRenderers[node.id] = marker.GetComponent<Renderer>();
            marker.AddComponent<LDY_NodeMarker>().nodeId = node.id;
        }

        TrailContainer = new GameObject("Trails").transform;
        TrailContainer.SetParent(transform, false);

        WallContainer = new GameObject("Walls").transform;
        WallContainer.SetParent(transform, false);
        WallMarkers = new Dictionary<string, GameObject>();
    }

    // 벽 아이템/스킬로 간선이 막히면(blocked=true) 그 도로 한가운데를 가로지르는 짧은
    // 바리케이드 선을 그려서 "진짜로 막혀있다"는 느낌을 준다. 풀리면(blocked=false) 치운다.
    public void SetWallVisual(string a, string b, bool blocked)
    {
        string key = string.CompareOrdinal(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";

        if (!blocked)
        {
            if (WallMarkers.TryGetValue(key, out GameObject existing))
            {
                Destroy(existing);
                WallMarkers.Remove(key);
            }
            return;
        }

        if (WallMarkers.ContainsKey(key) || !Nodes.TryGetValue(a, out LDY_MapNode nodeA) || !Nodes.TryGetValue(b, out LDY_MapNode nodeB))
            return;

        Vector2 mid = (nodeA.position + nodeB.position) * 0.5f;
        Vector2 dir = (nodeB.position - nodeA.position).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * 0.4f;

        Vector3 wallA = new Vector3(mid.x + perp.x, mid.y + perp.y, -0.06f);
        Vector3 wallB = new Vector3(mid.x - perp.x, mid.y - perp.y, -0.06f);

        LineRenderer wallLine = LDY_MapVisualFactory.CreateEdgeLine($"Wall_{key}", WallContainer, wallA, wallB, 0.15f, Color.black);
        WallMarkers[key] = wallLine.gameObject;
    }

    private void Start()
    {
        if (LDY_MapStateManager.Instance != null)
            LDY_MapStateManager.Instance.SetNodes(Nodes);
    }

    // 게임 리스타트: 갱단이 지나가며 남긴 트레일을 지우고, 노드 색을 원래(기지=청록, 나머지=흰색)로 되돌린다.
    public void ResetVisuals()
    {
        for (int i = TrailContainer.childCount - 1; i >= 0; i--)
            Destroy(TrailContainer.GetChild(i).gameObject);

        for (int i = WallContainer.childCount - 1; i >= 0; i--)
            Destroy(WallContainer.GetChild(i).gameObject);
        WallMarkers.Clear();

        foreach (LDY_MapNode node in nodes)
        {
            if (!NodeRenderers.TryGetValue(node.id, out Renderer renderer))
                continue;

            renderer.material.DOKill();
            renderer.material.color = node.isPlayerBase ? Color.cyan : Color.white;
        }
    }

    private void Reset()
    {
        nodes = BuildSampleMap();
    }

    private static List<LDY_MapNode> BuildSampleMap()
    {
        return new List<LDY_MapNode>
        {
            new LDY_MapNode { id = "base", position = new Vector2(0, 0), isPlayerBase = true, connectedNodeIds = new List<string> { "j1", "j2", "j3", "j4" } },

            new LDY_MapNode { id = "j1", position = new Vector2(0, 3), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j2", "j4", "gA", "gE" } },
            new LDY_MapNode { id = "j2", position = new Vector2(3, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j1", "j3", "gB" } },
            new LDY_MapNode { id = "j3", position = new Vector2(0, -3), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j2", "j4", "gC", "gF" } },
            new LDY_MapNode { id = "j4", position = new Vector2(-3, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j3", "j1", "gD", "gG" } },

            new LDY_MapNode { id = "gA", position = new Vector2(0, 6), isPlayerBase = false, connectedNodeIds = new List<string> { "j1" } },
            new LDY_MapNode { id = "gB", position = new Vector2(6, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "j2" } },
            new LDY_MapNode { id = "gC", position = new Vector2(0, -6), isPlayerBase = false, connectedNodeIds = new List<string> { "j3" } },
            new LDY_MapNode { id = "gD", position = new Vector2(-6, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "j4" } },
            new LDY_MapNode { id = "gE", position = new Vector2(3, 5), isPlayerBase = false, connectedNodeIds = new List<string> { "j1" } },
            new LDY_MapNode { id = "gF", position = new Vector2(-3, -5), isPlayerBase = false, connectedNodeIds = new List<string> { "j3" } },
            new LDY_MapNode { id = "gG", position = new Vector2(-6, -4), isPlayerBase = false, connectedNodeIds = new List<string> { "j4" } },
        };
    }
}

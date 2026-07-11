using System.Collections.Generic;
using UnityEngine;
// [내부 구현] 인스펙터에서 편집 가능한 노드 그래프를 실제 Dictionary/LDY_AStarPathfinder로 초기화한다.
public class GraphMapSetup : MonoBehaviour
{
    public static GraphMapSetup Instance { get; private set; }

    [SerializeField] private List<LDY_MapNode> nodes = new List<LDY_MapNode>();

    public LDY_AStarPathfinder Pathfinder { get; private set; }
    public Dictionary<string, LDY_MapNode> Nodes { get; private set; }

    // 갱단이 지나갈 때 노드 마커를 그 갱단 색으로 물들이기 위한 룩업.
    public Dictionary<string, Renderer> NodeRenderers { get; private set; }

    // 갱단이 지나간 길(트레일)을 심어두는 컨테이너. GangController가 아니라 여기에 부모를 둬야
    // 나중에 그 갱단이 와해되어 비활성화되더라도 지나간 흔적은 지도에 그대로 남는다.
    public Transform TrailContainer { get; private set; }

    // GraphMapSetupEditor(Scene 뷰 맵 편집 툴)가 원본 리스트를 직접 편집할 수 있도록 노출.
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

    // 플레이 모드에서 노드/간선이 실제로 보이도록 런타임 마커를 생성한다 (기지=청록, 나머지=흰색).
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

                LDY_MapVisualFactory.CreateEdgeLine($"Edge_{key}", container, node.position, neighbor.position, 0.05f, new Color(1f, 1f, 1f, 0.6f));
            }
        }

        NodeRenderers = new Dictionary<string, Renderer>();
        foreach (LDY_MapNode node in nodes)
        {
            Color color = node.isPlayerBase ? Color.cyan : Color.white;
            GameObject marker = LDY_MapVisualFactory.CreateMarker($"Node_{node.id}", container, node.position, 0.5f, color);
            NodeRenderers[node.id] = marker.GetComponent<Renderer>();
        }

        TrailContainer = new GameObject("Trails").transform;
        TrailContainer.SetParent(transform, false);
    }

    private void Start()
    {
        // LDY_MapStateManager는 자신의 Awake에서 Instance를 세팅하므로,
        // 모든 오브젝트의 Awake가 끝난 뒤인 Start 시점에 안전하게 노드 그래프를 주입한다.
        if (LDY_MapStateManager.Instance != null)
            LDY_MapStateManager.Instance.SetNodes(Nodes);
    }

    // 컴포넌트를 처음 추가하거나 인스펙터에서 "Reset"을 실행하면 기본 샘플 맵을 채워준다.
    // 이후에는 인스펙터에서 자유롭게 노드를 추가/수정할 수 있다.
    private void Reset()
    {
        nodes = BuildSampleMap();
    }

    // 기지(base, 중앙) + 정점 4개(j1~j4, 사방) + 갱단 노드 4개(gA~gD, 각 정점 바깥쪽).
    // 정점끼리는 사각형(j1-j2-j3-j4-j1)으로 상호 연결되고, 각 갱단 노드는 자기 쪽 정점에만 연결된다.
    private static List<LDY_MapNode> BuildSampleMap()
    {
        return new List<LDY_MapNode>
        {
            new LDY_MapNode { id = "base", position = new Vector2(0, 0), isPlayerBase = true, connectedNodeIds = new List<string> { "j1", "j2", "j3", "j4" } },

            new LDY_MapNode { id = "j1", position = new Vector2(0, 3), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j2", "j4", "gA" } },
            new LDY_MapNode { id = "j2", position = new Vector2(3, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j1", "j3", "gB" } },
            new LDY_MapNode { id = "j3", position = new Vector2(0, -3), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j2", "j4", "gC" } },
            new LDY_MapNode { id = "j4", position = new Vector2(-3, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "base", "j3", "j1", "gD" } },

            new LDY_MapNode { id = "gA", position = new Vector2(0, 6), isPlayerBase = false, connectedNodeIds = new List<string> { "j1" } },
            new LDY_MapNode { id = "gB", position = new Vector2(6, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "j2" } },
            new LDY_MapNode { id = "gC", position = new Vector2(0, -6), isPlayerBase = false, connectedNodeIds = new List<string> { "j3" } },
            new LDY_MapNode { id = "gD", position = new Vector2(-6, 0), isPlayerBase = false, connectedNodeIds = new List<string> { "j4" } },
        };
    }
}

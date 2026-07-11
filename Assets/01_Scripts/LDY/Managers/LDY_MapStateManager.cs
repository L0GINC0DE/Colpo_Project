using System.Collections.Generic;
using UnityEngine;

public class LDY_MapStateManager : MonoBehaviour, LDY_ISaveable
{
    public static LDY_MapStateManager Instance { get; private set; }

    // 벽 정보. a-b 사이 간선을 expireTurn까지 막는다.
    [System.Serializable]
    private class BlockedEdge
    {
        public string nodeA;
        public string nodeB;
        public int expireTurn;
    }

    // 임시 길 정보. 구조는 BlockedEdge와 같지만, 만료 시 connectedNodeIds에서도
    // 제거해야 하므로(실제로 간선을 새로 이었기 때문에) 별도 타입으로 분리한다.
    [System.Serializable]
    private class TemporaryEdge
    {
        public string nodeA;
        public string nodeB;
        public int expireTurn;
    }

    private readonly List<BlockedEdge> blockedEdges = new List<BlockedEdge>();
    private readonly List<TemporaryEdge> temporaryEdges = new List<TemporaryEdge>();

    private Dictionary<string, LDY_MapNode> nodes;

    public string SaveKey => "mapstate";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        LDY_GameEvents.OnSkillUsed += HandleSkillUsed;
    }

    private void OnDisable()
    {
        LDY_GameEvents.OnSkillUsed -= HandleSkillUsed;
    }

    public void SetNodes(Dictionary<string, LDY_MapNode> nodeMap)
    {
        nodes = nodeMap;
    }

    public void BlockEdge(string a, string b, int currentTurn, int durationTurns)
    {
        blockedEdges.Add(new BlockedEdge { nodeA = a, nodeB = b, expireTurn = currentTurn + durationTurns });
        if (LDY_GraphMapSetup.Instance != null)
            LDY_GraphMapSetup.Instance.SetWallVisual(a, b, true);
        Debug.Log($"[MapState] 간선 차단: {a} - {b} (턴 {currentTurn + durationTurns}까지)");
    }

    public void CreateEdge(string a, string b, int currentTurn, int durationTurns)
    {
        temporaryEdges.Add(new TemporaryEdge { nodeA = a, nodeB = b, expireTurn = currentTurn + durationTurns });

        if (nodes != null)
        {
            if (nodes.TryGetValue(a, out LDY_MapNode nodeA) && !nodeA.connectedNodeIds.Contains(b))
                nodeA.connectedNodeIds.Add(b);
            if (nodes.TryGetValue(b, out LDY_MapNode nodeB) && !nodeB.connectedNodeIds.Contains(a))
                nodeB.connectedNodeIds.Add(a);
        }

        Debug.Log($"[MapState] 임시 길 생성: {a} - {b} (턴 {currentTurn + durationTurns}까지)");
    }

    // 게임 리스타트: 걸려있던 벽/임시 길을 전부 걷어내고, 임시 길로 추가됐던 connectedNodeIds도 되돌린다.
    public void ResetState()
    {
        foreach (BlockedEdge edge in blockedEdges)
        {
            if (LDY_GraphMapSetup.Instance != null)
                LDY_GraphMapSetup.Instance.SetWallVisual(edge.nodeA, edge.nodeB, false);
        }
        blockedEdges.Clear();

        if (nodes != null)
        {
            foreach (TemporaryEdge edge in temporaryEdges)
            {
                if (nodes.TryGetValue(edge.nodeA, out LDY_MapNode nodeA))
                    nodeA.connectedNodeIds.Remove(edge.nodeB);
                if (nodes.TryGetValue(edge.nodeB, out LDY_MapNode nodeB))
                    nodeB.connectedNodeIds.Remove(edge.nodeA);
            }
        }
        temporaryEdges.Clear();
    }

    public bool IsEdgeBlocked(string a, string b)
    {
        foreach (BlockedEdge edge in blockedEdges)
        {
            if ((edge.nodeA == a && edge.nodeB == b) || (edge.nodeA == b && edge.nodeB == a))
                return true;
        }
        return false;
    }

    public void RemoveExpired(int currentTurn)
    {
        blockedEdges.RemoveAll(edge =>
        {
            bool expired = edge.expireTurn <= currentTurn;
            if (expired)
            {
                if (LDY_GraphMapSetup.Instance != null)
                    LDY_GraphMapSetup.Instance.SetWallVisual(edge.nodeA, edge.nodeB, false);
                Debug.Log($"[MapState] 벽 해제: {edge.nodeA} - {edge.nodeB}");
            }
            return expired;
        });

        temporaryEdges.RemoveAll(edge =>
        {
            bool expired = edge.expireTurn <= currentTurn;
            if (expired)
            {
                if (nodes != null)
                {
                    if (nodes.TryGetValue(edge.nodeA, out LDY_MapNode nodeA))
                        nodeA.connectedNodeIds.Remove(edge.nodeB);
                    if (nodes.TryGetValue(edge.nodeB, out LDY_MapNode nodeB))
                        nodeB.connectedNodeIds.Remove(edge.nodeA);
                }
                Debug.Log($"[MapState] 임시 길 소멸: {edge.nodeA} - {edge.nodeB}");
            }
            return expired;
        });
    }

    // 스킬명 -> 효과 매핑. 새 스킬이 추가되면 여기에 case만 늘리면 된다.
    private void HandleSkillUsed(string skillName, string targetNodeId, int durationTurns)
    {
        string[] parts = targetNodeId.Split(':');
        if (parts.Length != 2)
        {
            Debug.LogWarning($"[MapState] 스킬 대상 형식이 잘못됨: '{targetNodeId}' (\"nodeA:nodeB\" 형식이어야 함)");
            return;
        }

        int currentTurn = LDY_TurnManager.Instance != null ? LDY_TurnManager.Instance.currentTurn : 0;

        switch (skillName)
        {
            case "길목틀어막기":
                BlockEdge(parts[0], parts[1], currentTurn, durationTurns);
                break;

            case "길만들기":
                CreateEdge(parts[0], parts[1], currentTurn, durationTurns);
                break;
        }
    }

    // JsonUtility가 최상위 List를 직렬화 못 해서 감싸는 내부 Wrapper.
    [System.Serializable]
    private class MapStateSaveData
    {
        public List<BlockedEdge> blockedEdges = new List<BlockedEdge>();
        public List<TemporaryEdge> temporaryEdges = new List<TemporaryEdge>();
    }

    public string CaptureState()
    {
        var data = new MapStateSaveData
        {
            blockedEdges = blockedEdges,
            temporaryEdges = temporaryEdges
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        MapStateSaveData data = JsonUtility.FromJson<MapStateSaveData>(json);
        if (data == null)
            return;

        // 먼저 지금 걸려있는 벽/임시 길을 깨끗이 걷어낸다(비주얼 + connectedNodeIds 포함).
        // 안 그러면 복원한 값이랑 중복되거나, 세이브 시점 이후에 새로 생긴 게 안 지워진 채 남는다.
        ResetState();

        foreach (BlockedEdge edge in data.blockedEdges)
        {
            blockedEdges.Add(edge);
            if (LDY_GraphMapSetup.Instance != null)
                LDY_GraphMapSetup.Instance.SetWallVisual(edge.nodeA, edge.nodeB, true);
        }

        foreach (TemporaryEdge edge in data.temporaryEdges)
        {
            temporaryEdges.Add(edge);

            // CreateEdge를 그대로 호출하지 않는 이유: CreateEdge는 currentTurn 기준으로
            // expireTurn을 새로 계산해버린다. 여기선 세이브에 적힌 expireTurn을 그대로
            // 써야 해서, connectedNodeIds 갱신만 CreateEdge와 똑같이 직접 해준다.
            if (nodes != null)
            {
                if (nodes.TryGetValue(edge.nodeA, out LDY_MapNode nodeA) && !nodeA.connectedNodeIds.Contains(edge.nodeB))
                    nodeA.connectedNodeIds.Add(edge.nodeB);
                if (nodes.TryGetValue(edge.nodeB, out LDY_MapNode nodeB) && !nodeB.connectedNodeIds.Contains(edge.nodeA))
                    nodeB.connectedNodeIds.Add(edge.nodeA);
            }
        }
    }
}

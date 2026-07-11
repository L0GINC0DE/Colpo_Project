using System.Collections.Generic;
using UnityEngine;

public class LDY_MapStateManager : MonoBehaviour
{
    public static LDY_MapStateManager Instance { get; private set; }

    // 벽 정보. a-b 사이 간선을 expireTurn까지 막는다.
    private class BlockedEdge
    {
        public string nodeA;
        public string nodeB;
        public int expireTurn;
    }

    // 임시 길 정보. 구조는 BlockedEdge와 같지만, 만료 시 connectedNodeIds에서도
    // 제거해야 하므로(실제로 간선을 새로 이었기 때문에) 별도 타입으로 분리한다.
    private class TemporaryEdge
    {
        public string nodeA;
        public string nodeB;
        public int expireTurn;
    }

    private readonly List<BlockedEdge> blockedEdges = new List<BlockedEdge>();
    private readonly List<TemporaryEdge> temporaryEdges = new List<TemporaryEdge>();

    private Dictionary<string, LDY_MapNode> nodes;

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
}

using System.Collections.Generic;
using UnityEngine;

// [내부 구현] 벽(막힌 길)과 임시 길(새로 만든 길)을 관리하는 싱글톤.
// 팀원 코드에서 직접 호출해도 되는 public API: BlockEdge / CreateEdge / IsEdgeBlocked.
public class MapStateManager : MonoBehaviour
{
    public static MapStateManager Instance { get; private set; }

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

    // CreateEdge/RemoveExpired에서 노드의 connectedNodeIds를 직접 수정해야 하므로
    // GraphMapSetup이 만든 노드 그래프를 주입받는다 (GraphMapSetup.Start 참고).
    private Dictionary<string, MapNode> nodes;

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
        GameEvents.OnSkillUsed += HandleSkillUsed;
    }

    private void OnDisable()
    {
        GameEvents.OnSkillUsed -= HandleSkillUsed;
    }

    public void SetNodes(Dictionary<string, MapNode> nodeMap)
    {
        nodes = nodeMap;
    }

    public void BlockEdge(string a, string b, int currentTurn, int durationTurns)
    {
        blockedEdges.Add(new BlockedEdge { nodeA = a, nodeB = b, expireTurn = currentTurn + durationTurns });
        Debug.Log($"[MapState] 간선 차단: {a} - {b} (턴 {currentTurn + durationTurns}까지)");
    }

    public void CreateEdge(string a, string b, int currentTurn, int durationTurns)
    {
        temporaryEdges.Add(new TemporaryEdge { nodeA = a, nodeB = b, expireTurn = currentTurn + durationTurns });

        if (nodes != null)
        {
            if (nodes.TryGetValue(a, out MapNode nodeA) && !nodeA.connectedNodeIds.Contains(b))
                nodeA.connectedNodeIds.Add(b);
            if (nodes.TryGetValue(b, out MapNode nodeB) && !nodeB.connectedNodeIds.Contains(a))
                nodeB.connectedNodeIds.Add(a);
        }

        Debug.Log($"[MapState] 임시 길 생성: {a} - {b} (턴 {currentTurn + durationTurns}까지)");
    }

    // 양방향 체크: a-b든 b-a든 같은 간선으로 취급한다.
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
                Debug.Log($"[MapState] 벽 해제: {edge.nodeA} - {edge.nodeB}");
            return expired;
        });

        temporaryEdges.RemoveAll(edge =>
        {
            bool expired = edge.expireTurn <= currentTurn;
            if (expired)
            {
                if (nodes != null)
                {
                    if (nodes.TryGetValue(edge.nodeA, out MapNode nodeA))
                        nodeA.connectedNodeIds.Remove(edge.nodeB);
                    if (nodes.TryGetValue(edge.nodeB, out MapNode nodeB))
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

        int currentTurn = TurnManager.Instance != null ? TurnManager.Instance.currentTurn : 0;

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

using System;
using System.Collections.Generic;
using UnityEngine;

// [내부 구현] 갱단 하나의 이동 AI. GangData(팀 공유 데이터)를 참조해서
// 성향(GangType)에 따라 다른 규칙으로 노드 그래프 위를 이동한다.
public class GangController : MonoBehaviour
{
    public GangData gangData;
    public bool pursuing;
    public string playerBaseId = "base";

    // 직진파(Direct) 전용 캐시.
    // 추적을 시작하면 A*를 딱 한 번만 계산해서 저장해두고, 이후에는 매 턴 재탐색하지 않고
    // 이 경로를 그대로 따라간다. (다른 성향과 달리 "한 번 정한 길로 우직하게 직진"하는 것이
    // 직진파의 개성이기 때문 - 재탐색을 안 하므로 벽에 막히면 뚫릴 때까지 그냥 기다린다.)
    private List<string> cachedPath;
    private int cachedPathIndex;

    // 플레이 모드에서 실제로 보이는 갱단 마커. 성향별로 색을 다르게 줘서 구분하기 쉽게 한다.
    private void Start()
    {
        if (GraphMapSetup.Instance != null && GraphMapSetup.Instance.Nodes.TryGetValue(gangData.currentNodeId, out MapNode node))
            transform.position = node.position;

        MapVisualFactory.CreateMarker($"GangVisual_{gangData.gangName}", transform, transform.position, 0.7f, GetGangColor(gangData.type), keepCollider: true);
    }

    private static Color GetGangColor(GangType type)
    {
        switch (type)
        {
            case GangType.Direct: return Color.red;
            case GangType.Intelligent: return new Color(0.6f, 0f, 1f);
            case GangType.Greedy: return new Color(1f, 0.5f, 0f);
            case GangType.Scale: return Color.magenta;
            case GangType.Chaos: return Color.yellow;
            default: return Color.gray;
        }
    }

    public void UpdateResistance(int turnCount)
    {
        switch (gangData.type)
        {
            case GangType.Scale:
                // 턴이 지날수록 단단해지는 왕귀파.
                // 상한을 0.8로 둔 이유: 1.0이 되면 절대 못 터는(영원히 무적인) 갱단이 생겨
                // 밸런스가 무너지기 때문.
                gangData.damageResistance = Mathf.Min(0.8f, turnCount * 0.05f);
                break;

            case GangType.Tanker:
                // 탱커파는 이번 단계에서는 둔감도 고정값만 세팅하고, 이동 로직은 미구현.
                gangData.damageResistance = 0.3f;
                break;
        }
    }

    public void ProcessTurn(int turnCount, AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        switch (gangData.type)
        {
            case GangType.Direct:
                ProcessDirect(pathfinder, isEdgeBlocked);
                break;

            case GangType.Intelligent:
                ProcessIntelligent(pathfinder, isEdgeBlocked);
                break;

            case GangType.Greedy:
                {
                    // 훔긴 금액 누적치 = maxFunds - currentFunds
                    int stolenSoFar = gangData.maxFunds - gangData.currentFunds;
                    int speed = 1 + stolenSoFar / 250;
                    ProcessSpeedMove(pathfinder, isEdgeBlocked, speed);
                    break;
                }

            case GangType.Scale:
                {
                    int speed = 1 + turnCount / 3;
                    ProcessSpeedMove(pathfinder, isEdgeBlocked, speed);
                    break;
                }

            case GangType.Chaos:
                ProcessChaos(pathfinder, isEdgeBlocked);
                break;
        }
    }

    // 직진파: 캐싱된 경로를 한 칸(속도 1)씩 따라간다.
    // 다음 칸으로 가는 간선이 막혀 있으면 재탐색 없이 그 자리에서 대기하고,
    // 벽이 풀리면(TurnManager가 매 턴 RemoveExpired를 호출해주므로) 다음 턴에 자동으로 다시 전진한다.
    private void ProcessDirect(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        if (cachedPath == null)
            cachedPath = pathfinder.FindPath(gangData.currentNodeId, playerBaseId);

        if (cachedPath == null || cachedPathIndex + 1 >= cachedPath.Count)
            return;

        string nextNode = cachedPath[cachedPathIndex + 1];
        if (isEdgeBlocked != null && isEdgeBlocked(gangData.currentNodeId, nextNode))
        {
            Debug.Log($"[{gangData.gangName}] 직진파 - 길이 막혀 대기");
            return;
        }

        cachedPathIndex++;
        MoveTo(nextNode);
    }

    // 지능파: 매 턴 막힌 길을 반영해서 새로 탐색한다 -> 막힌 길은 A*가 알아서 우회한다.
    private void ProcessIntelligent(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        List<string> path = pathfinder.FindPath(gangData.currentNodeId, playerBaseId, isEdgeBlocked);
        if (path == null || path.Count < 2)
            return;

        MoveTo(path[1]);
    }

    // 탐욕파/왕귀파 공용: 매 턴 재탐색한 경로를 속도만큼 전진하되,
    // 중간에 막힌 간선을 만나면 그 자리에서 멈춘다.
    private void ProcessSpeedMove(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked, int speed)
    {
        List<string> path = pathfinder.FindPath(gangData.currentNodeId, playerBaseId, isEdgeBlocked);
        if (path == null || path.Count < 2)
            return;

        int index = 0;
        int steps = 0;
        while (steps < speed && index + 1 < path.Count)
        {
            if (isEdgeBlocked != null && isEdgeBlocked(path[index], path[index + 1]))
                break;

            index++;
            steps++;
        }

        if (index > 0)
            MoveTo(path[index]);
    }

    // 불예측파: 40% 확률로 인접 노드 중 무작위 이동, 그 외에는 평범하게 A* 경로로 한 칸 전진.
    private void ProcessChaos(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        if (UnityEngine.Random.value < 0.4f)
        {
            MapNode currentNode = pathfinder.GetNode(gangData.currentNodeId);
            if (currentNode == null || currentNode.connectedNodeIds.Count == 0)
                return;

            var reachable = new List<string>();
            foreach (string neighbor in currentNode.connectedNodeIds)
            {
                if (isEdgeBlocked == null || !isEdgeBlocked(gangData.currentNodeId, neighbor))
                    reachable.Add(neighbor);
            }

            if (reachable.Count == 0)
                return;

            string randomNode = reachable[UnityEngine.Random.Range(0, reachable.Count)];
            Debug.Log($"[{gangData.gangName}] 불예측파 - 무작위 이동");
            MoveTo(randomNode);
        }
        else
        {
            ProcessSpeedMove(pathfinder, isEdgeBlocked, 1);
        }
    }

    private void MoveTo(string nodeId)
    {
        Debug.Log($"[{gangData.gangName}] {gangData.currentNodeId} -> {nodeId} 이동");
        gangData.currentNodeId = nodeId;

        if (GraphMapSetup.Instance != null && GraphMapSetup.Instance.Nodes.TryGetValue(nodeId, out MapNode node))
            transform.position = node.position; // 애니메이션 없이 즉시 스냅 이동 (연출은 이번 단계에서 생략)

        if (nodeId == playerBaseId)
        {
            pursuing = false;
            GameEvents.GangReachedBase(gangData.gangName);
        }
    }
}

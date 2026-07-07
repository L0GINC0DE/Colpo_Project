using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GangController : MonoBehaviour
{
    public GangData gangData;
    public bool pursuing;
    public string playerBaseId = "base";

    [SerializeField] private float moveDuration = 0.4f;

    // 직진파(Direct) 전용 캐시.
    // 추적을 시작하면 A*를 딱 한 번만 계산해서 저장해두고, 이후에는 매 턴 재탐색하지 않고
    // 이 경로를 그대로 따라간다.
    private List<string> cachedPath;
    private int cachedPathIndex;

    private void Start()
    {
        if (GraphMapSetup.Instance != null && GraphMapSetup.Instance.Nodes.TryGetValue(gangData.currentNodeId, out MapNode node))
            transform.position = node.position;

        MapVisualFactory.CreateMarker($"GangVisual_{gangData.gangName}", transform, transform.position, 0.7f, GetGangColor(gangData.type), keepCollider: true);
    }

    private void OnEnable()
    {
        GameEvents.OnGangDefeated += HandleGangDefeated;
    }

    private void OnDisable()
    {
        GameEvents.OnGangDefeated -= HandleGangDefeated;
    }

    private void HandleGangDefeated(string gangId)
    {
        if (gangData != null && gangData.gangName == gangId)
            gameObject.SetActive(false);
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
                gangData.damageResistance = Mathf.Min(0.8f, turnCount * 0.05f);
                break;

            case GangType.Tanker:
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

        MapNode fromNode = null;
        MapNode toNode = null;
        if (GraphMapSetup.Instance != null)
        {
            GraphMapSetup.Instance.Nodes.TryGetValue(gangData.currentNodeId, out fromNode);
            GraphMapSetup.Instance.Nodes.TryGetValue(nodeId, out toNode);
        }

        gangData.currentNodeId = nodeId;

        if (fromNode != null && toNode != null)
        {
            transform.DOKill();
            transform.DOMove(toNode.position, moveDuration).SetEase(Ease.InOutSine);
            PlayMoveTrail(fromNode, toNode);
        }

        if (nodeId == playerBaseId)
        {
            pursuing = false;
            GameEvents.GangReachedBase(gangData.gangName);
        }
    }

    private void PlayMoveTrail(MapNode fromNode, MapNode toNode)
    {
        Color color = GetGangColor(gangData.type);
        Transform parent = GraphMapSetup.Instance != null ? GraphMapSetup.Instance.TrailContainer : transform;

        Vector3 from3 = new Vector3(fromNode.position.x, fromNode.position.y, -0.05f);
        Vector3 to3 = new Vector3(toNode.position.x, toNode.position.y, -0.05f);

        LineRenderer trail = MapVisualFactory.CreateEdgeLine(
            $"Trail_{gangData.gangName}_{fromNode.id}-{toNode.id}", parent, from3, from3, 0.08f, color);

        FlashNode(fromNode.id, color);

        float progress = 0f;
        DOTween.To(() => progress, x => progress = x, 1f, moveDuration)
            .OnUpdate(() => trail.SetPosition(1, Vector3.Lerp(from3, to3, progress)))
            .OnComplete(() => FlashNode(toNode.id, color));
    }

    private static void FlashNode(string nodeId, Color color)
    {
        if (GraphMapSetup.Instance == null || !GraphMapSetup.Instance.NodeRenderers.TryGetValue(nodeId, out Renderer renderer))
            return;

        renderer.material.DOKill();
        renderer.material.DOColor(color, 0.2f);
    }
}

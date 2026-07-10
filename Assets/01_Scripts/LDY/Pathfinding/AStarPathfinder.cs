using System;
using System.Collections.Generic;
using UnityEngine;

// 각 노드마다 f = g + h 값을 계산해서, f가 가장 작은 노드부터 우선 방문한다.
//   g = 시작 노드에서 현재 노드까지 실제로 이동한 거리 (지금까지 확정된 비용)
//   h = 현재 노드에서 목표까지 남았을 것으로 추정되는 거리 (휴리스틱, 여기서는 좌표 간 유클리드 거리)
//   f = g + h  → 이미 온 거리 + 앞으로 남을 것 같은 거리. 이 값이 작을수록 목표까지 더 빨리 닿을
//               가능성이 높은 노드이므로, 힙에서 그 노드를 먼저 꺼내 확장한다.
public class AStarPathfinder
{
    private readonly Dictionary<string, MapNode> nodes;

    public AStarPathfinder(Dictionary<string, MapNode> nodes)
    {
        this.nodes = nodes;
    }

    // 노드 id로 MapNode를 직접 조회한다 (예: 불예측파가 인접 노드 목록을 뒤질 때 사용).
    public MapNode GetNode(string id)
    {
        nodes.TryGetValue(id, out MapNode node);
        return node;
    }

    // start -> goal 최단 경로를 찾아 노드 id 리스트로 반환한다 (start와 goal 포함).
    public List<string> FindPath(string start, string goal, Func<string, string, bool> isEdgeBlocked = null)
    {
        if (!nodes.ContainsKey(start) || !nodes.ContainsKey(goal))
            return null;

        var openHeap = new MinHeap<string>();
        var gScore = new Dictionary<string, float> { [start] = 0f };
        var cameFrom = new Dictionary<string, string>();
        var visited = new HashSet<string>(); // 최단 거리가 확정된(방문 완료) 노드 집합

        openHeap.Push(start, Heuristic(start, goal));

        while (!openHeap.IsEmpty)
        {
            string current = openHeap.Pop();

            if (visited.Contains(current))
                continue;
            visited.Add(current);

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            MapNode currentNode = nodes[current];

            foreach (string neighborId in currentNode.connectedNodeIds)
            {
                if (visited.Contains(neighborId) || !nodes.ContainsKey(neighborId))
                    continue;

                if (isEdgeBlocked != null && isEdgeBlocked(current, neighborId))
                    continue; // 막힌 간선은 이웃에서 제외 -> 자동 우회

                float tentativeG = gScore[current] + Vector2.Distance(nodes[current].position, nodes[neighborId].position);

                if (!gScore.TryGetValue(neighborId, out float knownG) || tentativeG < knownG)
                {
                    gScore[neighborId] = tentativeG;
                    cameFrom[neighborId] = current;
                    float f = tentativeG + Heuristic(neighborId, goal);
                    openHeap.Push(neighborId, f); // 기존에 힙에 남아있던 값은 지연 삭제로 나중에 무시됨
                }
            }
        }

        return null; // 경로 없음
    }

    private float Heuristic(string a, string b)
    {
        return Vector2.Distance(nodes[a].position, nodes[b].position);
    }

    private List<string> ReconstructPath(Dictionary<string, string> cameFrom, string current)
    {
        var path = new List<string> { current };
        while (cameFrom.TryGetValue(current, out string prev))
        {
            current = prev;
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}

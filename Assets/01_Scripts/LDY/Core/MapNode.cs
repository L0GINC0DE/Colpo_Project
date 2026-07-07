using System.Collections.Generic;
using UnityEngine;

// [팀 공유 계층] 맵 그래프의 노드 하나를 나타내는 순수 데이터 클래스.
// MonoBehaviour가 아니므로 GraphMapSetup의 인스펙터 리스트뿐 아니라
// 어디서든 new로 생성해서 쓸 수 있다.
[System.Serializable]
public class MapNode
{
    public string id;
    public Vector2 position;
    public bool isPlayerBase;
    public List<string> connectedNodeIds = new List<string>();
}

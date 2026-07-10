using UnityEngine;

// 클릭 가능한 간선(길) 콜라이더에 붙어서, 이 콜라이더가 어떤 두 노드 사이의
// 길인지 알려준다. WallItemHandler가 레이캐스트로 이 컴포넌트를 찾아 BlockEdge에 넘길 노드 id를 얻는다.
public class LDY_EdgeMarker : MonoBehaviour
{
    public string nodeAId;
    public string nodeBId;
}

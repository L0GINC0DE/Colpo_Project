using UnityEngine;

// 클릭 가능한 노드 콜라이더에 붙어서 이 콜라이더가 어떤 노드인지 알려준다.
// PathRedirectHandler가 레이캐스트로 이 컴포넌트를 찾아 강제 경로를 이어붙이는 데 쓴다.
public class NodeMarker : MonoBehaviour
{
    public string nodeId;
}

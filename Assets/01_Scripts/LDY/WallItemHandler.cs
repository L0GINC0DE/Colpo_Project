using UnityEngine;
using UnityEngine.InputSystem;

// [테스트/플레이어 조작] 벽 아이템. 좌클릭으로 노드 사이 간선(EdgeMarker 콜라이더)을 직접 찍어서
// 그 길을 wallDurationTurns 턴 동안 막는다. 개수 제한 아이템이라 wallCharges를 다 쓰면
// 더 이상 설치할 수 없다 (AtmClickHandler와 같은 좌클릭을 쓰지만, 갱단 마커와 간선 콜라이더는
// 서로 다른 오브젝트라 한 번의 클릭이 둘 중 하나에만 맞는다).
[RequireComponent(typeof(Camera))]
public class WallItemHandler : MonoBehaviour
{
    public static WallItemHandler Instance { get; private set; }

    [SerializeField] private int wallCharges = 3;
    [SerializeField] private int wallDurationTurns = 3;

    private int initialWallCharges;
    private Camera cam;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        initialWallCharges = wallCharges;
    }

    // 게임 리스타트: 벽 개수를 시작할 때 값으로 되돌린다.
    public void ResetCharges()
    {
        wallCharges = initialWallCharges;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        if (wallCharges <= 0)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        EdgeMarker edge = hit.collider.GetComponent<EdgeMarker>();
        if (edge == null)
            return;

        int currentTurn = TurnManager.Instance != null ? TurnManager.Instance.currentTurn : 0;
        MapStateManager.Instance.BlockEdge(edge.nodeAId, edge.nodeBId, currentTurn, wallDurationTurns);

        wallCharges--;
        Debug.Log($"[WallItemHandler] {edge.nodeAId}-{edge.nodeBId} 벽 설치 ({wallDurationTurns}턴, 남은 개수: {wallCharges})");
    }
}

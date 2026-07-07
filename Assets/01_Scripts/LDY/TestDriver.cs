using UnityEngine;
using UnityEngine.InputSystem;

// [테스트 전용] 팀원 스킬/UI 코드가 아직 없는 상태에서 추적 시스템을 키 입력만으로
// 검증하기 위한 임시 드라이버. 실제 게임 로직에는 관여하지 않는다.
//
//   S     : testGangId 에게서 StealFrom 호출 (추적 시작 확인)
//   Space : AdvanceTurn 호출 (갱단들이 성향별로 이동하는 로그 확인)
//   B     : blockNodeA - blockNodeB 간선을 blockDuration턴 동안 BlockEdge
//   N     : newEdgeNodeA - newEdgeNodeB 사이에 newEdgeDuration턴짜리 CreateEdge
public class TestDriver : MonoBehaviour
{
    [Header("S: StealFrom 테스트")]
    [SerializeField] private string testGangId = "gA";
    [SerializeField] private int stealAmount = 300;

    [Header("B: BlockEdge 테스트")]
    [SerializeField] private string blockNodeA = "j1";
    [SerializeField] private string blockNodeB = "base";
    [SerializeField] private int blockDuration = 3;

    [Header("N: CreateEdge 테스트")]
    [SerializeField] private string newEdgeNodeA = "gA";
    [SerializeField] private string newEdgeNodeB = "base";
    [SerializeField] private int newEdgeDuration = 3;

    private void OnEnable()
    {
        GameEvents.OnGangDefeated += HandleGangDefeated;
        GameEvents.OnMoneyChanged += HandleMoneyChanged;
        GameEvents.OnGangReachedBase += HandleGangReachedBase;
        GameEvents.OnAllGangsDefeated += HandleAllGangsDefeated;
    }

    private void OnDisable()
    {
        GameEvents.OnGangDefeated -= HandleGangDefeated;
        GameEvents.OnMoneyChanged -= HandleMoneyChanged;
        GameEvents.OnGangReachedBase -= HandleGangReachedBase;
        GameEvents.OnAllGangsDefeated -= HandleAllGangsDefeated;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.sKey.wasPressedThisFrame)
        {
            Debug.Log($"[TestDriver] S - StealFrom({testGangId}, {stealAmount})");
            GangManager.Instance.StealFrom(testGangId, stealAmount);
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("[TestDriver] Space - AdvanceTurn");
            TurnManager.Instance.AdvanceTurn();
        }

        if (keyboard.bKey.wasPressedThisFrame)
        {
            Debug.Log($"[TestDriver] B - BlockEdge({blockNodeA}, {blockNodeB}, {blockDuration}턴)");
            MapStateManager.Instance.BlockEdge(blockNodeA, blockNodeB, TurnManager.Instance.currentTurn, blockDuration);
        }

        if (keyboard.nKey.wasPressedThisFrame)
        {
            Debug.Log($"[TestDriver] N - CreateEdge({newEdgeNodeA}, {newEdgeNodeB}, {newEdgeDuration}턴)");
            MapStateManager.Instance.CreateEdge(newEdgeNodeA, newEdgeNodeB, TurnManager.Instance.currentTurn, newEdgeDuration);
        }
    }

    private void HandleGangDefeated(string gangId) => Debug.Log($"[TestDriver] 이벤트 - 갱단 와해: {gangId}");
    private void HandleMoneyChanged(int amount) => Debug.Log($"[TestDriver] 이벤트 - 플레이어 자금 변화: +{amount}");
    private void HandleGangReachedBase(string gangId) => Debug.Log($"[TestDriver] 이벤트 - 기지 도달(배드엔딩): {gangId}");
    private void HandleAllGangsDefeated() => Debug.Log("[TestDriver] 이벤트 - 전원 와해(해피엔딩)");
}

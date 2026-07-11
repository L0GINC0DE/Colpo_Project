using UnityEngine;
using UnityEngine.InputSystem;

public class LDY_TestDriver : MonoBehaviour
{
    [Header("S: StealFrom 테스트")]
    // gA 노드에 스폰되는 갱단의 gangId는 "gA"가 아니라 LDY_GangConfig.gangId("Direct")다 -
    // StealFrom은 이제 gangName이 아니라 gangId로 매칭한다.
    [SerializeField] private string testGangId = "Direct";
    [SerializeField] private int stealAmount = 300;

    [Header("N: CreateEdge 테스트")]
    [SerializeField] private string newEdgeNodeA = "gA";
    [SerializeField] private string newEdgeNodeB = "base";
    [SerializeField] private int newEdgeDuration = 3;

    private void OnEnable()
    {
        LDY_GameEvents.OnGangDefeated += HandleGangDefeated;
        LDY_GameEvents.OnMoneyChanged += HandleMoneyChanged;
        LDY_GameEvents.OnGangReachedBase += HandleGangReachedBase;
        LDY_GameEvents.OnAllGangsDefeated += HandleAllGangsDefeated;
    }

    private void OnDisable()
    {
        LDY_GameEvents.OnGangDefeated -= HandleGangDefeated;
        LDY_GameEvents.OnMoneyChanged -= HandleMoneyChanged;
        LDY_GameEvents.OnGangReachedBase -= HandleGangReachedBase;
        LDY_GameEvents.OnAllGangsDefeated -= HandleAllGangsDefeated;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.sKey.wasPressedThisFrame)
        {
            Debug.Log($"[LDY_TestDriver] S - StealFrom({testGangId}, {stealAmount})");
            LDY_GangManager.Instance.StealFrom(testGangId, stealAmount);
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("[LDY_TestDriver] Space - AdvanceTurn");
            LDY_TurnManager.Instance.AdvanceTurn();
        }

        if (keyboard.nKey.wasPressedThisFrame)
        {
            Debug.Log($"[LDY_TestDriver] N - CreateEdge({newEdgeNodeA}, {newEdgeNodeB}, {newEdgeDuration}턴)");
            LDY_MapStateManager.Instance.CreateEdge(newEdgeNodeA, newEdgeNodeB, LDY_TurnManager.Instance.currentTurn, newEdgeDuration);
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            Debug.Log("[LDY_TestDriver] R - ResetGame (재시도)");
            LDY_TurnManager.Instance.ResetGame();
        }

        if (keyboard.lKey.wasPressedThisFrame)
        {
            Debug.Log("[LDY_TestDriver] L - SaveSystem.Load (세이브 파일 불러오기 확인용)");
            LDY_SaveSystem.Instance.Load();
        }
    }

    private void HandleGangDefeated(string gangId) => Debug.Log($"[LDY_TestDriver] 이벤트 - 갱단 와해: {gangId}");
    private void HandleMoneyChanged(int amount) => Debug.Log($"[LDY_TestDriver] 이벤트 - 플레이어 자금 변화: +{amount}");
    private void HandleGangReachedBase(string gangId) => Debug.Log($"[LDY_TestDriver] 이벤트 - 기지 도달(배드엔딩): {gangId}");
    private void HandleAllGangsDefeated() => Debug.Log("[LDY_TestDriver] 이벤트 - 전원 와해(해피엔딩)");
}

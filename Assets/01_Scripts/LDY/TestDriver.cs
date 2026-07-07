using UnityEngine;
using UnityEngine.InputSystem;

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

        if (keyboard.rKey.wasPressedThisFrame)
        {
            Debug.Log("[TestDriver] R - ResetGame (재시도)");
            TurnManager.Instance.ResetGame();
        }
    }

    private void HandleGangDefeated(string gangId) => Debug.Log($"[TestDriver] 이벤트 - 갱단 와해: {gangId}");
    private void HandleMoneyChanged(int amount) => Debug.Log($"[TestDriver] 이벤트 - 플레이어 자금 변화: +{amount}");
    private void HandleGangReachedBase(string gangId) => Debug.Log($"[TestDriver] 이벤트 - 기지 도달(배드엔딩): {gangId}");
    private void HandleAllGangsDefeated() => Debug.Log("[TestDriver] 이벤트 - 전원 와해(해피엔딩)");
}

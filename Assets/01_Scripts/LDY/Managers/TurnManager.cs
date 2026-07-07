using System.Collections.Generic;
using UnityEngine;

// [내부 구현] 턴 진행을 총괄하는 싱글톤.
// 팀원 공용 API: AdvanceTurn(). 현재 턴은 currentTurn 필드로 읽을 수 있다.
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public int currentTurn = 0;
    public int maxTurn = 30;

    private bool isGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnGangReachedBase += HandleGangReachedBase;
        GameEvents.OnAllGangsDefeated += HandleAllGangsDefeated;
    }

    private void OnDisable()
    {
        GameEvents.OnGangReachedBase -= HandleGangReachedBase;
        GameEvents.OnAllGangsDefeated -= HandleAllGangsDefeated;
    }

    public void AdvanceTurn()
    {
        if (isGameOver)
        {
            Debug.LogWarning("[TurnManager] 게임이 이미 종료되어 턴을 진행하지 않습니다.");
            return;
        }

        currentTurn++;

        // 1) 이동 처리 전에 만료된 벽/임시 길부터 정리해야 갱단이 그 턴에 바로 반영된 경로를 탄다.
        MapStateManager.Instance.RemoveExpired(currentTurn);

        List<GangController> controllers = GangManager.Instance.GetAllGangControllers();

        // 2) 추적 여부와 무관하게 모든 갱단의 피해 둔감도 갱신 (왕귀파는 턴이 지날수록 단단해짐).
        foreach (GangController controller in controllers)
            controller.UpdateResistance(currentTurn);

        // 3) 추적 중(pursuing)인 갱단만 이동 처리.
        AStarPathfinder pathfinder = GraphMapSetup.Instance.Pathfinder;
        foreach (GangController controller in controllers)
        {
            if (controller.pursuing)
                controller.ProcessTurn(currentTurn, pathfinder, MapStateManager.Instance.IsEdgeBlocked);
        }

        GameEvents.TurnAdvanced(currentTurn);
        Debug.Log($"[TurnManager] {currentTurn}번째 턴 진행");

        // 4) 종료 조건 체크 (갱단의 기지 도달은 HandleGangReachedBase에서 별도 처리됨).
        if (currentTurn >= maxTurn && !isGameOver)
        {
            isGameOver = true;
            Debug.LogError($"[배드엔딩] {maxTurn}턴 제한에 도달했습니다 - 게임 종료");
        }
    }

    private void HandleGangReachedBase(string gangId)
    {
        isGameOver = true;
        Debug.LogError($"[배드엔딩] 갱단 '{gangId}'가 플레이어 기지에 도달했습니다! (턴 {currentTurn})");
    }

    private void HandleAllGangsDefeated()
    {
        isGameOver = true;
        Debug.Log("[해피엔딩] 모든 갱단이 자금을 소진했습니다!");
    }
}

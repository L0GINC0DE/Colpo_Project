using System;
using System.Collections.Generic;
using UnityEngine;

public class LDY_TurnManager : MonoBehaviour, LDY_ISaveable
{
    public static LDY_TurnManager Instance { get; private set; }

    public int currentTurn = 0;
    public int maxTurn = 30;

    // OnMoneyChanged 이벤트는 변화량만 방송해서, 실제 누적 총액은 여기서 따로 들고 있는다.
    public int playerMoney = 0;

    private bool isGameOver;

    public string SaveKey => "turn";

    // 갱단창 안에서 하는 유료 행동(해킹/털기, 돈세탁, 공격형 스킬-기밀정보유출)의 공통 진입점.
    // action을 실행한 뒤 반드시 턴을 소모시킨다. 방어형 스킬(길목틀어막기/새길만들기/
    // 추적경로재탐색/노이즈/프리즈)은 이 메서드를 거치지 않고 각 핸들러가 직접 호출해서
    // 턴을 소모하지 않는다 - 새 유료 행동을 추가할 때도 반드시 이 메서드를 통해서만 실행할 것.
    public void PerformGangWindowAction(Action action)
    {
        action.Invoke();
        AdvanceTurn();
    }

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
        LDY_GameEvents.OnGangReachedBase += HandleGangReachedBase;
        LDY_GameEvents.OnAllGangsDefeated += HandleAllGangsDefeated;
        LDY_GameEvents.OnMoneyChanged += HandleMoneyChanged;
    }

    private void OnDisable()
    {
        LDY_GameEvents.OnGangReachedBase -= HandleGangReachedBase;
        LDY_GameEvents.OnAllGangsDefeated -= HandleAllGangsDefeated;
        LDY_GameEvents.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleMoneyChanged(int amount)
    {
        playerMoney += amount;
    }

    public void AdvanceTurn()
    {
        if (isGameOver)
        {
            Debug.LogWarning("[LDY_TurnManager] 게임이 이미 종료되어 턴을 진행하지 않습니다.");
            return;
        }

        currentTurn++;

        // 1) 이동 처리 전에 만료된 벽/임시 길부터 정리해야 갱단이 그 턴에 바로 반영된 경로를 탄다.
        LDY_MapStateManager.Instance.RemoveExpired(currentTurn);

        List<LDY_GangController> controllers = LDY_GangManager.Instance.GetAllGangControllers();

        // 2) 추적 여부와 무관하게 모든 갱단의 피해 둔감도 갱신(왕귀파는 턴이 지날수록 단단해짐)과
        //    노이즈 지속 턴 감소를 처리한다.
        foreach (LDY_GangController controller in controllers)
        {
            controller.UpdateResistance(currentTurn);
            controller.TickNoise();
        }

        // 3) 추적 중(pursuing)인 갱단만 이동 처리.
        LDY_AStarPathfinder pathfinder = LDY_GraphMapSetup.Instance.Pathfinder;
        foreach (LDY_GangController controller in controllers)
        {
            if (controller.pursuing)
                controller.ProcessTurn(currentTurn, pathfinder, LDY_MapStateManager.Instance.IsEdgeBlocked);
        }

        // 4) 얼음은 이동 처리가 끝난 "뒤에" 줄여야 한다. 먼저 줄이면 1턴 얼려도 카운트가 바로 0이
        //    되면서 그 턴에 곧장 움직여버린다(방금 겪은 버그) - 이동 여부와 무관하게 전부 처리해서
        //    추적 중이 아닌 갱단도 얼음이 영원히 안 풀리는 일이 없게 한다.
        foreach (LDY_GangController controller in controllers)
            controller.TickFreeze();

        LDY_GameEvents.TurnAdvanced(currentTurn);
        Debug.Log($"[LDY_TurnManager] {currentTurn}번째 턴 진행");

        // 5) 종료 조건 체크 (갱단의 기지 도달은 HandleGangReachedBase에서 별도 처리됨).
        if (currentTurn >= maxTurn && !isGameOver)
        {
            isGameOver = true;
            Debug.LogWarning($"[배드엔딩] {maxTurn}턴 제한에 도달했습니다 - 게임 종료");
        }

        // 턴이 넘어갈 때마다 자동 저장.
        if (LDY_SaveSystem.Instance != null)
            LDY_SaveSystem.Instance.AutoSave("턴 종료");
    }

    private void HandleGangReachedBase(string gangId)
    {
        isGameOver = true;
        Debug.LogWarning($"[배드엔딩] 갱단 '{gangId}'가 플레이어 기지에 도달했습니다! (턴 {currentTurn}) - 재시도하려면 리스타트하세요.");
    }

    private void HandleAllGangsDefeated()
    {
        isGameOver = true;
        Debug.Log("[해피엔딩] 모든 갱단이 자금을 소진했습니다!");
    }

    public void ResetGame()
    {
        currentTurn = 0;
        playerMoney = 0;
        isGameOver = false;

        foreach (LDY_GangController controller in LDY_GangManager.Instance.GetAllGangControllers())
            controller.ResetToStart();

        LDY_MapStateManager.Instance.ResetState();
        LDY_GraphMapSetup.Instance.ResetVisuals();

        if (LDY_WallItemHandler.Instance != null)
            LDY_WallItemHandler.Instance.ResetCharges();

        if (LDY_FreezeItemHandler.Instance != null)
            LDY_FreezeItemHandler.Instance.ResetCharges();

        if (LDY_PathRedirectHandler.Instance != null)
            LDY_PathRedirectHandler.Instance.ResetCharges();

        if (LDY_NoiseItemHandler.Instance != null)
            LDY_NoiseItemHandler.Instance.ResetCharges();

        Debug.Log("[LDY_TurnManager] 게임 리스타트 - 모든 상태를 초기화했습니다.");
    }

    [System.Serializable]
    private class TurnSaveData
    {
        public int currentTurn;
        public int playerMoney;
        public bool isGameOver;
    }

    public string CaptureState()
    {
        var data = new TurnSaveData
        {
            currentTurn = currentTurn,
            playerMoney = playerMoney,
            isGameOver = isGameOver
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string json)
    {
        TurnSaveData data = JsonUtility.FromJson<TurnSaveData>(json);
        if (data == null)
            return;

        currentTurn = data.currentTurn;
        playerMoney = data.playerMoney;
        isGameOver = data.isGameOver;
    }
}

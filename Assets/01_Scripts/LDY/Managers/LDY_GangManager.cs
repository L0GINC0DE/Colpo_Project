using System.Collections.Generic;
using UnityEngine;

public class LDY_GangManager : MonoBehaviour
{
    public static LDY_GangManager Instance { get; private set; }

    private readonly List<LDY_GangController> gangControllers = new List<LDY_GangController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        gangControllers.AddRange(FindObjectsByType<LDY_GangController>(FindObjectsSortMode.None));
    }

    private void OnEnable()
    {
        LDY_GameEvents.OnColpoResult += HandleColpoResult;
        LDY_GameEvents.OnSkillUsed += HandleSkillUsed;
    }

    private void OnDisable()
    {
        LDY_GameEvents.OnColpoResult -= HandleColpoResult;
        LDY_GameEvents.OnSkillUsed -= HandleSkillUsed;
    }

    public LDY_GangData GetGangInfo(string gangId)
    {
        LDY_GangController controller = FindController(gangId);
        return controller != null ? controller.gangData : null;
    }

    public List<LDY_GangData> GetAllGangs()
    {
        var result = new List<LDY_GangData>();
        foreach (LDY_GangController controller in gangControllers)
            result.Add(controller.gangData);
        return result;
    }

    public void StealFrom(string gangId, int amount)
    {
        LDY_GangController controller = FindController(gangId);
        if (controller == null)
        {
            Debug.LogWarning($"[LDY_GangManager] 존재하지 않는 갱단id: {gangId}");
            return;
        }

        LDY_GangData data = controller.gangData;

        int mitigated = Mathf.RoundToInt(amount * (1f - data.damageResistance));
        int actualStolen = Mathf.Min(mitigated, data.currentFunds);

        data.currentFunds -= actualStolen;
        LDY_GameEvents.MoneyChanged(actualStolen);
        controller.pursuing = true;

        Debug.Log($"[LDY_GangManager] {gangId} 에게서 {actualStolen} 훔침 (남은 자금/체력: {data.currentFunds}) - 추적 시작");

        if (data.currentFunds <= 0)
        {
            data.currentFunds = 0;
            controller.pursuing = false;
            LDY_GameEvents.GangDefeated(gangId);
            Debug.Log($"[LDY_GangManager] {gangId} 와해됨");
            CheckAllGangsDefeated();
        }
    }

    public void Freeze(string gangId, int turns)
    {
        LDY_GangController controller = FindController(gangId);
        if (controller == null)
        {
            Debug.LogWarning($"[LDY_GangManager] 존재하지 않는 갱단id: {gangId}");
            return;
        }

        controller.Freeze(turns);
    }

    public void Noise(string gangId, int turns)
    {
        LDY_GangController controller = FindController(gangId);
        if (controller == null)
        {
            Debug.LogWarning($"[LDY_GangManager] 존재하지 않는 갱단id: {gangId}");
            return;
        }

        controller.ApplyNoise(turns);
    }

    // 스킬명 -> 갱단 대상 효과 매핑. 간선 대상 스킬(길목틀어막기/길만들기)은 MapStateManager가 처리한다.
    private void HandleSkillUsed(string skillName, string targetId, int durationTurns)
    {
        if (skillName == "얼리기")
            Freeze(targetId, durationTurns);
        else if (skillName == "노이즈")
            Noise(targetId, durationTurns);
        else if (skillName == "기밀정보유출")
            Debug.Log($"[LDY_GangManager] {targetId} 대상 기밀정보유출 사용 - 실제 효과는 아직 미구현(플레이스홀더).");
    }

    private void HandleColpoResult(string gangId, int amount, string resultType)
    {
        switch (resultType)
        {
            case "safe":
            case "colpo":
                StealFrom(gangId, amount);
                break;

            case "fail":
                {
                    // 금고 한도 초과("망"): 돈은 못 벌지만 바로 추적 시작 + 최대 추적 거리 1.5배 강제 전진.
                    LDY_GangController controller = FindController(gangId);
                    if (controller == null)
                    {
                        Debug.LogWarning($"[LDY_GangManager] 존재하지 않는 갱단id: {gangId}");
                        break;
                    }

                    controller.pursuing = true;
                    int steps = Mathf.CeilToInt(controller.gangData.maxChaseDistanceByRisk * 1.5f);
                    ForceAdvance(controller, steps);
                    break;
                }

            case "hackFail":
                {
                    // 미니게임(사전 해킹) 실패: 이 갱단만 2턴 해킹 락, 돈/추적 변화 없음.
                    LDY_GangController controller = FindController(gangId);
                    if (controller == null)
                    {
                        Debug.LogWarning($"[LDY_GangManager] 존재하지 않는 갱단id: {gangId}");
                        break;
                    }

                    int currentTurn = LDY_TurnManager.Instance != null ? LDY_TurnManager.Instance.currentTurn : 0;
                    controller.gangData.hackLockedUntilTurn = currentTurn + 2;
                    Debug.Log($"[LDY_GangManager] {gangId} 해킹 실패 - 턴 {controller.gangData.hackLockedUntilTurn}까지 해킹 락");
                    break;
                }

            default:
                Debug.LogWarning($"[LDY_GangManager] 알 수 없는 콜포 결과: {resultType}");
                break;
        }
    }

    // LDY_GangController.ForceAdvance로 그대로 위임.
    public void ForceAdvance(LDY_GangController gang, int steps)
    {
        if (gang == null || LDY_GraphMapSetup.Instance == null || LDY_MapStateManager.Instance == null)
            return;

        gang.ForceAdvance(steps, LDY_GraphMapSetup.Instance.Pathfinder, LDY_MapStateManager.Instance.IsEdgeBlocked);
    }

    private void CheckAllGangsDefeated()
    {
        foreach (LDY_GangController controller in gangControllers)
        {
            if (controller.gangData.currentFunds > 0)
                return;
        }

        LDY_GameEvents.AllGangsDefeated();
        Debug.Log("[LDY_GangManager] 모든 갱단 자금 소진 - 해피엔딩");
    }

    private LDY_GangController FindController(string gangId)
    {
        foreach (LDY_GangController controller in gangControllers)
        {
            if (controller.gangData != null && controller.gangData.gangName == gangId)
                return controller;
        }
        return null;
    }
    
    public List<LDY_GangController> GetAllGangControllers()
    {
        return gangControllers;
    }
}

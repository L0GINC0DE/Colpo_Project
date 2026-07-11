using System.Collections.Generic;
using UnityEngine;

public class LDY_GangManager : MonoBehaviour, LDY_ISaveable
{
    public static LDY_GangManager Instance { get; private set; }

    private readonly List<LDY_GangController> gangControllers = new List<LDY_GangController>();

    public string SaveKey => "gangs";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        gangControllers.AddRange(FindObjectsByType<LDY_GangController>(FindObjectsSortMode.None));

        // 매 게임 시작마다 GangConfig(불변 설정) 기준으로 새 GangRuntimeState를 만들어서
        // 각 컨트롤러에 심어준다 - ScriptableObject에 값이 남아 다음 게임에 이어지는 문제를
        // 원천적으로 막는 지점. Awake는 모든 오브젝트의 Start()보다 먼저 실행되니까,
        // LDY_GangController.Start()가 실행될 땐 이미 runtimeState가 채워져 있다.
        foreach (LDY_GangController controller in gangControllers)
            controller.AssignRuntimeState(CreateInitialRuntimeState(controller));
    }

    private static LDY_GangRuntimeState CreateInitialRuntimeState(LDY_GangController controller)
    {
        LDY_GangConfig config = controller.config;
        return new LDY_GangRuntimeState
        {
            gangId = config.gangId,
            currentFunds = config.maxFunds,
            currentNodeId = controller.SpawnNodeId,
            hackLockedUntilTurn = 0,
            damageResistance = config.initialDamageResistance,
            pursuing = false
        };
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

    public LDY_GangConfig GetGangInfo(string gangId)
    {
        LDY_GangController controller = FindController(gangId);
        return controller != null ? controller.config : null;
    }

    public List<LDY_GangConfig> GetAllGangs()
    {
        var result = new List<LDY_GangConfig>();
        foreach (LDY_GangController controller in gangControllers)
            result.Add(controller.config);
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

        LDY_GangRuntimeState state = controller.runtimeState;

        int mitigated = Mathf.RoundToInt(amount * (1f - state.damageResistance));
        int actualStolen = Mathf.Min(mitigated, state.currentFunds);

        state.currentFunds -= actualStolen;
        LDY_GameEvents.MoneyChanged(actualStolen);
        controller.pursuing = true;

        Debug.Log($"[LDY_GangManager] {gangId} 에게서 {actualStolen} 훔침 (남은 자금/체력: {state.currentFunds}) - 추적 시작");

        if (state.currentFunds <= 0)
        {
            state.currentFunds = 0;
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
        // TODO(미구현): 기획서 기준 갱단 체력에 직접 데미지를 줘야 함.
        //  지금은 로그만 찍는 플레이스홀더 - 데미지 수치/공식 확정되면 구현
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
                    int steps = Mathf.CeilToInt(controller.config.maxChaseDistanceByRisk * 1.5f);
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
                    controller.runtimeState.hackLockedUntilTurn = currentTurn + 2;
                    Debug.Log($"[LDY_GangManager] {gangId} 해킹 실패 - 턴 {controller.runtimeState.hackLockedUntilTurn}까지 해킹 락");
                    break;
                }

            default:
                Debug.LogWarning($"[LDY_GangManager] 알 수 없는 콜포 결과: {resultType}");
                break;
        }

        // 콜포 결과가 확정될 때마다(자금/추적/해킹락이 바뀌는 시점) 자동 저장.
        if (LDY_SaveSystem.Instance != null)
            LDY_SaveSystem.Instance.AutoSave("Colpo 결과 확정");
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
            if (controller.runtimeState.currentFunds > 0)
                return;
        }

        LDY_GameEvents.AllGangsDefeated();
        Debug.Log("[LDY_GangManager] 모든 갱단 자금 소진 - 해피엔딩");
    }

    // gangId(LDY_GangConfig.gangId, 세이브/이벤트 전반에서 쓰는 고유 식별자) 기준으로 찾는다.
    // gangName은 표시용이라 더 이상 매칭에 쓰지 않는다.
    private LDY_GangController FindController(string gangId)
    {
        foreach (LDY_GangController controller in gangControllers)
        {
            if (controller.config != null && controller.config.gangId == gangId)
                return controller;
        }
        return null;
    }

    public List<LDY_GangController> GetAllGangControllers()
    {
        return gangControllers;
    }

    // JsonUtility가 최상위 List를 직렬화 못 해서 감싸는 내부 Wrapper.
    [System.Serializable]
    private class RuntimeStateListWrapper
    {
        public List<LDY_GangRuntimeState> states = new List<LDY_GangRuntimeState>();
    }

    public string CaptureState()
    {
        var wrapper = new RuntimeStateListWrapper();
        foreach (LDY_GangController controller in gangControllers)
            wrapper.states.Add(controller.runtimeState);

        return JsonUtility.ToJson(wrapper);
    }

    public void RestoreState(string json)
    {
        RuntimeStateListWrapper wrapper = JsonUtility.FromJson<RuntimeStateListWrapper>(json);
        if (wrapper?.states == null)
            return;

        foreach (LDY_GangRuntimeState savedState in wrapper.states)
        {
            LDY_GangController controller = FindController(savedState.gangId);
            if (controller == null)
            {
                Debug.LogWarning($"[LDY_GangManager] 복원 대상 갱단을 씬에서 못 찾음: {savedState.gangId}");
                continue;
            }

            controller.ApplyRuntimeState(savedState);
        }
    }
}

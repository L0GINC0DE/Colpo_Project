using System.Collections.Generic;
using UnityEngine;

// [내부 구현] 씬의 모든 GangController를 관리하는 싱글톤.
// 팀원 코드에서 직접 호출해도 되는 public API: GetGangInfo / GetAllGangs / StealFrom.
public class GangManager : MonoBehaviour
{
    public static GangManager Instance { get; private set; }

    private readonly List<GangController> gangControllers = new List<GangController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        gangControllers.AddRange(FindObjectsByType<GangController>(FindObjectsSortMode.None));
    }

    private void OnEnable()
    {
        GameEvents.OnColpoResult += HandleColpoResult;
    }

    private void OnDisable()
    {
        GameEvents.OnColpoResult -= HandleColpoResult;
    }

    // ----- 팀원 공용 API -----

    public GangData GetGangInfo(string gangId)
    {
        GangController controller = FindController(gangId);
        return controller != null ? controller.gangData : null;
    }

    public List<GangData> GetAllGangs()
    {
        var result = new List<GangData>();
        foreach (GangController controller in gangControllers)
            result.Add(controller.gangData);
        return result;
    }

    public void StealFrom(string gangId, int amount)
    {
        GangController controller = FindController(gangId);
        if (controller == null)
        {
            Debug.LogWarning($"[GangManager] 존재하지 않는 갱단id: {gangId}");
            return;
        }

        GangData data = controller.gangData;

        // 피해 둔감도(damageResistance)만큼 훔칠 수 있는 금액이 줄어든다.
        int mitigated = Mathf.RoundToInt(amount * (1f - data.damageResistance));
        int actualStolen = Mathf.Min(mitigated, data.currentFunds);

        data.currentFunds -= actualStolen;
        GameEvents.MoneyChanged(actualStolen);
        controller.pursuing = true;

        Debug.Log($"[GangManager] {gangId} 에게서 {actualStolen} 훔침 (남은 자금/체력: {data.currentFunds}) - 추적 시작");

        if (data.currentFunds <= 0)
        {
            data.currentFunds = 0;
            controller.pursuing = false;
            GameEvents.GangDefeated(gangId);
            Debug.Log($"[GangManager] {gangId} 와해됨");
            CheckAllGangsDefeated();
        }
    }

    // ----- 내부 구현 -----

    private void HandleColpoResult(string gangId, int amount, string resultType)
    {
        switch (resultType)
        {
            case "safe":
            case "colpo":
                StealFrom(gangId, amount);
                break;

            case "fail":
                GangController controller = FindController(gangId);
                if (controller != null)
                    controller.pursuing = true;
                break;

            default:
                Debug.LogWarning($"[GangManager] 알 수 없는 콜포 결과: {resultType}");
                break;
        }
    }

    private void CheckAllGangsDefeated()
    {
        foreach (GangController controller in gangControllers)
        {
            if (controller.gangData.currentFunds > 0)
                return;
        }

        GameEvents.AllGangsDefeated();
        Debug.Log("[GangManager] 모든 갱단 자금 소진 - 해피엔딩");
    }

    private GangController FindController(string gangId)
    {
        foreach (GangController controller in gangControllers)
        {
            if (controller.gangData != null && controller.gangData.gangName == gangId)
                return controller;
        }
        return null;
    }

    // TurnManager가 매 턴 모든 갱단을 순회하기 위한 내부용 접근자.
    // 팀원 코드는 GangController를 직접 다루지 말고 GetAllGangs()/GetGangInfo()를 사용할 것.
    public List<GangController> GetAllGangControllers()
    {
        return gangControllers;
    }
}

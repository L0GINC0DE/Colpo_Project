using System.Collections.Generic;
using UnityEngine;

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
    
    public List<GangController> GetAllGangControllers()
    {
        return gangControllers;
    }
}

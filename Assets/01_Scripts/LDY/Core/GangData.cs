using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewGangData", menuName = "Colpo/Gang Data")]
public class GangData : ScriptableObject
{
    [Header("기본 정보")]
    public string gangName;
    public GangType type;

    // 평상시 위험도. 실제 조회는 GetEffectiveRiskLevel()을 거칠 것 - 직접 읽으면
    // 시너지로 인한 위험도 상승이 반영 안 된다.
    [FormerlySerializedAs("riskLevel")]
    public RiskLevel baseRiskLevel;

    // 위험도가 시너지랑 무관하게 고정인 갱단이면 체크.
    public bool isRiskFixed;

    [Header("자금 = 체력")]
    public int maxFunds;
    public int currentFunds;

    [Header("위치")]
    public string currentNodeId;

    [Header("전투 특성")]
    [Range(0f, 1f)]
    public float damageResistance;

    [Header("Colpo 결과 상태")]
    // 해킹 실패 시 이 턴까지 해킹 막힘 (ProcessTurn에서 체크).
    public int hackLockedUntilTurn;

    // 위험도별 최대 추적 거리(칸 수). low=3/mid=5/high=8 정도 임시값, 나중에 조정 예정.
    // 금고 한도 초과("망") 시 이 값의 1.5배만큼 강제 전진시키는 데 쓰인다.
    public int maxChaseDistanceByRisk = 5;

    // 고정이면 baseRiskLevel 그대로, 아니면 시너지 활성화 중엔 High로 취급.
    public RiskLevel GetEffectiveRiskLevel(bool synergyActive)
    {
        if (isRiskFixed)
            return baseRiskLevel;
        return synergyActive ? RiskLevel.High : baseRiskLevel;
    }
}

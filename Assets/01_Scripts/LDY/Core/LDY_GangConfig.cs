using UnityEngine;
using UnityEngine.Serialization;

// 갱단의 "절대 안 바뀌는" 설정값만 담는 ScriptableObject. 예전엔 currentFunds/currentNodeId
// 같은 매 게임마다 바뀌는 값도 여기 같이 있었는데, 그러면 Play 모드에서 값이 바뀔 때
// 에셋 자체에 남아버려서 다음 게임을 시작할 때 이전 게임 상태로 시작하는 문제가 있었다.
// 그런 값들은 전부 LDY_GangRuntimeState(순수 C# 클래스)로 옮겼음.
[CreateAssetMenu(fileName = "NewGangData", menuName = "Colpo/Gang Data")]
public class LDY_GangConfig : ScriptableObject
{
    [Header("기본 정보")]
    // 런타임 상태(LDY_GangRuntimeState)랑 세이브 데이터를 매칭할 때 쓰는 고유 식별자.
    // gangName은 표시용이라 바뀔 수 있으니, 식별/매칭은 항상 gangId로 한다.
    public string gangId;
    public string gangName;
    public LDY_GangType type;

    // 평상시 위험도. 실제 조회는 GetEffectiveRiskLevel()을 거칠 것 - 직접 읽으면
    // 시너지로 인한 위험도 상승이 반영 안 된다.
    [FormerlySerializedAs("riskLevel")]
    public LDY_RiskLevel baseRiskLevel;

    // 위험도가 시너지랑 무관하게 고정인 갱단이면 체크.
    public bool isRiskFixed;

    [Header("자금 = 체력")]
    public int maxFunds;

    [Header("전투 특성")]
    // 게임 시작 시점의 피해 저항값. 왕귀파/탱커파처럼 턴이 지나며 저항이 바뀌는 갱단도
    // 시작값은 여기서 가져오고, 그 이후로는 LDY_GangRuntimeState.damageResistance가
    // 따로 관리한다(LDY_GangController.UpdateResistance).
    [FormerlySerializedAs("damageResistance")]
    [Range(0f, 1f)]
    public float initialDamageResistance;

    // 왕귀파(Scale) 전용: 10턴마다 이만큼씩 피해 저항이 증가한다(LDY_GangController.UpdateResistance에서 사용).
    public float resistanceGainPerTenTurns = 0.5f;

    // 위험도(RiskLevel)에 따라 자동으로 정해지는 값이 아님 - 이 갱단 개별 설정값.
    // 새 GangConfig 에셋을 만들 때 baseRiskLevel과 맞춰서 직접 입력할 것
    // (권장: Low→3, Mid→5, High→8, 필요시 팀 협의로 조정).
    // 금고 한도 초과("망") 시 이 값의 1.5배만큼 강제 전진시키는 데 쓰인다.
    public int maxChaseDistanceByRisk = 5;

    // 고정이면 baseRiskLevel 그대로, 아니면 시너지 활성화 중엔 High로 취급.
    public LDY_RiskLevel GetEffectiveRiskLevel(bool synergyActive)
    {
        if (isRiskFixed)
            return baseRiskLevel;
        return synergyActive ? LDY_RiskLevel.High : baseRiskLevel;
    }
}

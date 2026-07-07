using UnityEngine;

// [팀 공유 계층] 갱단 하나의 상태를 담는 ScriptableObject.
// 팀원들은 이 데이터를 읽어서 UI/연출 등에 자유롭게 쓰되,
// 값을 "바꾸는" 행위는 반드시 GangManager의 public API(StealFrom 등)나
// GameEvents를 거쳐서 해야 한다 (직접 currentFunds를 수정하지 말 것).
[CreateAssetMenu(fileName = "NewGangData", menuName = "Colpo/Gang Data")]
public class GangData : ScriptableObject
{
    [Header("기본 정보")]
    public string gangName; // 갱단 고유 id로도 사용된다 (GetGangInfo/StealFrom 등의 gangId와 동일한 문자열)
    public GangType type;
    public RiskLevel riskLevel; // 인스펙터 드롭다운에서 Low/Mid/High 중 하나 선택

    [Header("자금 = 체력")]
    // ★ 이 게임에서 돈과 체력은 같은 값이다.
    // 훔친 만큼 그대로 currentFunds에서 차감되고, 0이 되면 갱단이 와해된다.
    // 별도의 health 필드는 두지 않는다.
    public int maxFunds;
    public int currentFunds;

    [Header("위치")]
    public string currentNodeId;

    [Header("전투 특성")]
    [Range(0f, 1f)]
    public float damageResistance; // 훔칠 수 있는 금액에 (1 - damageResistance) 배율로 적용됨
}

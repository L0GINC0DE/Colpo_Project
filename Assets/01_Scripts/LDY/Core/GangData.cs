using UnityEngine;

[CreateAssetMenu(fileName = "NewGangData", menuName = "Colpo/Gang Data")]
public class GangData : ScriptableObject
{
    [Header("기본 정보")]
    public string gangName;
    public GangType type;
    public RiskLevel riskLevel; 

    [Header("자금 = 체력")]
    public int maxFunds;
    public int currentFunds;

    [Header("위치")]
    public string currentNodeId;

    [Header("전투 특성")]
    [Range(0f, 1f)]
    public float damageResistance;
}

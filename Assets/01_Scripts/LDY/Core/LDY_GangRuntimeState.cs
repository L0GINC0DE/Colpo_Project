using System;

// 갱단의 "매 게임마다 바뀌는" 상태만 모아둔 순수 C# 클래스. LDY_GangConfig(ScriptableObject)엔
// 절대 안 바뀌는 설정값만 남기고, 실제 플레이 중 변하는 값은 전부 여기서 들고 있는다.
// ScriptableObject 필드로 두면 Play 모드에서 값이 바뀔 때 에셋 자체에 남아서, 다음 게임을
// 시작할 때 이전 게임 상태로 시작해버리는 문제가 있었다 - 그걸 막으려고 분리함.
[Serializable]
public class LDY_GangRuntimeState
{
    public string gangId;
    public int currentFunds;
    public string currentNodeId;
    public int hackLockedUntilTurn;
    public float damageResistance;
    public bool pursuing;
}

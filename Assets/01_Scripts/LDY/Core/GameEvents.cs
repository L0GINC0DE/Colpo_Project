using System;

// [팀 공유 계층] 이벤트 버스.
// 팀원들은 서로의 내부 구현(Managers/, Gang/ 등)을 직접 참조하지 않고,
// 이 클래스의 이벤트를 구독하거나 아래 static 메서드로 이벤트를 발행해서 통신한다.
public static class GameEvents
{
    public static event Action<string> OnGangDefeated;                  // gangId
    public static event Action<int> OnMoneyChanged;                     // 변화량

    // 스킬명, 대상노드id, 지속턴.
    // 규약: 대상이 간선(edge)인 스킬(길목틀어막기/길만들기)은 대상노드id 파라미터에
    // "nodeA:nodeB" 형식의 문자열을 담아서 전달한다 (이벤트 시그니처가 노드 id 하나뿐이므로).
    public static event Action<string, string, int> OnSkillUsed;

    public static event Action<int> OnTurnAdvanced;                     // 현재 턴
    public static event Action<string> OnGangReachedBase;               // gangId
    public static event Action OnAllGangsDefeated;
    public static event Action<string, int, string> OnColpoResult;      // gangId, 금액, "safe"/"fail"/"colpo"

    public static void GangDefeated(string gangId) => OnGangDefeated?.Invoke(gangId);
    public static void MoneyChanged(int amount) => OnMoneyChanged?.Invoke(amount);
    public static void SkillUsed(string skillName, string targetNodeId, int durationTurns) => OnSkillUsed?.Invoke(skillName, targetNodeId, durationTurns);
    public static void TurnAdvanced(int currentTurn) => OnTurnAdvanced?.Invoke(currentTurn);
    public static void GangReachedBase(string gangId) => OnGangReachedBase?.Invoke(gangId);
    public static void AllGangsDefeated() => OnAllGangsDefeated?.Invoke();
    public static void ColpoResult(string gangId, int amount, string resultType) => OnColpoResult?.Invoke(gangId, amount, resultType);
}

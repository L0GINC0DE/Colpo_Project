// [팀 공유 계층] 갱단 성향(이동 AI 패턴).
// 이번 구현 대상은 Direct/Greedy/Chaos/Scale/Intelligent 5개이며,
// Support/Debuff/Tanker는 추후 구현을 위해 enum 값만 미리 정의해둔다.
// (Tanker는 damageResistance 고정값만 GangController.UpdateResistance에 반영되어 있고, 이동 로직은 없음)
public enum GangType
{
    Direct,       // 직진파: 추적 시작 시 A* 한 번만 계산해 캐싱, 막히면 재탐색 없이 대기
    Greedy,       // 탐욕파: 훔친 금액이 많을수록 빨라짐
    Chaos,        // 불예측파: 40% 확률로 인접 노드 중 무작위 이동
    Scale,        // 왕귀파: 턴이 지날수록 빨라지고 둔감해짐
    Intelligent,  // 지능파: 매 턴 재탐색해서 막힌 길을 자동 우회
    Support,      // 보급파 (미구현)
    Debuff,       // 디버프파 (미구현)
    Tanker        // 탱커파 (미구현, 둔감도 고정값만 반영)
}

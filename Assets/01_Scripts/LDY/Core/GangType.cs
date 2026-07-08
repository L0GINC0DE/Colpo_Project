public enum GangType
{
    Direct,       // 직진파: 추적 시작 시 A* 한 번만 계산해 캐싱, 막히면 재탐색 없이 대기
    Greedy,       // 탐욕파: 훔친 금액이 많을수록 빨라짐
    Chaos,        // 불예측파: 40% 확률로 인접 노드 중 무작위 이동
    Scale,        // 왕귀파: 턴이 지날수록 빨라지고 둔감해짐
    Intelligent,  // 지능파: 매 턴 재탐색해서 막힌 길을 자동 우회
    Support,      // 보급파 
    Debuff,       // 디버프파 
    Tanker        // 탱커파 
}

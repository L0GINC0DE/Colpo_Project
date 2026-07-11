// 세이브 시스템이 자동으로 찾아내는 저장 대상 인터페이스. Register/Unregister 같은 별도
// 관리 없이, 저장/불러오기 시점마다 LDY_SaveSystem이 씬을 훑어서(FindObjectsByType)
// 이 인터페이스를 구현한 컴포넌트를 그때그때 찾아낸다.
public interface LDY_ISaveable
{
    // 세이브 파일 안에서 이 컴포넌트를 구분하는 키. "gangs", "mapstate", "turn"처럼
    // 겹치지 않게만 정해주면 됨.
    string SaveKey { get; }

    // 자기 상태를 JsonUtility.ToJson으로 직접 문자열로 만들어서 반환.
    string CaptureState();

    // CaptureState가 만들었던 문자열을 JsonUtility.FromJson<T>으로 직접 역직렬화해서 복원.
    void RestoreState(string json);
}

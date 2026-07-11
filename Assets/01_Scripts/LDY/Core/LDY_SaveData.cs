using System;
using System.Collections.Generic;

// 세이브 파일 하나의 항목. LDY_ISaveable 구현체 하나당 (SaveKey, CaptureState 결과) 한 쌍씩 담긴다.
[Serializable]
public class LDY_SaveEntry
{
    public string key;
    public string json;
}

// 세이브 파일 전체 구조. JsonUtility는 Dictionary를 직렬화하지 못하고 최상위 List도
// 직렬화 못 하기 때문에, 반드시 이 Wrapper로 한 번 감싸서 List<LDY_SaveEntry>를 담는다.
[Serializable]
public class LDY_SaveFileWrapper
{
    public int saveVersion;
    public List<LDY_SaveEntry> entries = new List<LDY_SaveEntry>();
}

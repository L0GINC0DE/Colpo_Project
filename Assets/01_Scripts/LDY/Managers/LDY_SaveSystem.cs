using System;
using System.IO;
using System.Linq;
using UnityEngine;

// ISaveable 자동 탐색 기반 세이브 시스템. 저장 대상은 Register/Unregister로 따로 관리하지
// 않고, 저장/불러오기 시점마다 씬에 떠 있는 LDY_ISaveable 구현체를 그때그때 훑어서 찾는다.
[DefaultExecutionOrder(-100)]
public class LDY_SaveSystem : MonoBehaviour
{
    public static LDY_SaveSystem Instance { get; private set; }

    private const int CURRENT_VERSION = 1;

    private string GetSavePath() => Path.Combine(Application.persistentDataPath, "colpo_save.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePersistentInstance()
    {
        var go = new GameObject("[LDY_SaveSystem]");
        DontDestroyOnLoad(go);
        go.AddComponent<LDY_SaveSystem>();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // reason은 그냥 로그용(어디서 호출했는지 콘솔에서 바로 보이게).
    public void AutoSave(string reason)
    {
        var wrapper = new LDY_SaveFileWrapper { saveVersion = CURRENT_VERSION };

        foreach (LDY_ISaveable saveable in FindAllSaveables())
        {
            wrapper.entries.Add(new LDY_SaveEntry
            {
                key = saveable.SaveKey,
                json = saveable.CaptureState()
            });
        }

        SafeWrite(GetSavePath(), JsonUtility.ToJson(wrapper, prettyPrint: true));
        Debug.Log($"[LDY_SaveSystem] 저장 완료 - 사유: {reason} (항목 {wrapper.entries.Count}개)");
    }

    public void Load()
    {
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            Debug.Log("[LDY_SaveSystem] 세이브 파일이 없어 불러오기를 건너뜁니다.");
            return;
        }

        LDY_SaveFileWrapper wrapper;
        try
        {
            string json = File.ReadAllText(path);
            wrapper = JsonUtility.FromJson<LDY_SaveFileWrapper>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LDY_SaveSystem] 불러오기 실패: {e.Message}");
            return;
        }

        if (wrapper == null)
        {
            Debug.LogWarning("[LDY_SaveSystem] 세이브 파일을 읽었지만 내용이 비어있습니다.");
            return;
        }

        wrapper = Migrate(wrapper);

        var saveables = FindAllSaveables().ToList();
        foreach (LDY_SaveEntry entry in wrapper.entries)
        {
            LDY_ISaveable target = saveables.FirstOrDefault(s => s.SaveKey == entry.key);
            if (target == null)
            {
                Debug.LogWarning($"[LDY_SaveSystem] '{entry.key}' 를 복원할 대상을 씬에서 못 찾았습니다.");
                continue;
            }

            target.RestoreState(entry.json);
        }

        Debug.Log($"[LDY_SaveSystem] 불러오기 완료 (항목 {wrapper.entries.Count}개)");
    }

    // 비활성화된 오브젝트(와해된 갱단 등)도 저장/복원 대상이라 Include로 찾는다.
    private static System.Collections.Generic.IEnumerable<LDY_ISaveable> FindAllSaveables()
    {
        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<LDY_ISaveable>();
    }

    // saveVersion이 CURRENT_VERSION보다 낮은 옛날 세이브 파일을 지금 구조에 맞게 보정하는 자리.
    // 지금은 버전 1이 처음이라 실제 마이그레이션 로직은 없음 - 나중에 필드가 추가되면
    // "if (wrapper.saveVersion < 2) { ... 옛 필드 기본값 채우기 ... }" 식으로 여기에 늘려나가면 된다.
    private LDY_SaveFileWrapper Migrate(LDY_SaveFileWrapper wrapper)
    {
        if (wrapper.saveVersion < CURRENT_VERSION)
            Debug.Log($"[LDY_SaveSystem] 구버전 세이브(v{wrapper.saveVersion}) 감지 - 아직 마이그레이션 규칙 없음, 그대로 사용");

        wrapper.saveVersion = CURRENT_VERSION;
        return wrapper;
    }

    // 저장 도중 강제종료돼도 파일이 깨지지 않도록, .tmp에 먼저 쓰고 나서 원래 경로로 복사한다.
    private void SafeWrite(string path, string json)
    {
        string tempPath = path + ".tmp";
        try
        {
            File.WriteAllText(tempPath, json);
            File.Copy(tempPath, path, overwrite: true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LDY_SaveSystem] 저장 실패: {e.Message}");
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}

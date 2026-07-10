using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 갱단 하나의 저장 위치. JsonUtility가 Dictionary를 직렬화하지 못해서 리스트로 대신 담는다.
[Serializable]
public class LDY_GangSaveEntry
{
    public string gangName;
    public string nodeId;
}

[Serializable]
public class LDY_SaveData
{
    public int IceItem       = 0;
    public int WallItem           = 0;
    public int NoiseItem      = 0;
    public int MoveItem  = 0;
    public int AttackItem = 0;

    public string mapProgress;

    public List<LDY_GangSaveEntry> gangPositions = new List<LDY_GangSaveEntry>();

    public int ScaleHigh = 0;
    
}


[DefaultExecutionOrder(-100)] 
public class LDY_SaveManager : MonoBehaviour
{
    public static LDY_SaveManager Instance { get; private set; }

    const string SAVE_FILE = "save.json";

    string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE);

    public LDY_SaveData Data { get; private set; } = new LDY_SaveData();

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreatePersistentInstance()
    {
        var go = new GameObject("[LDY_SaveManager]");
        DontDestroyOnLoad(go);
        go.AddComponent<LDY_SaveManager>(); 
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this); 
            return;
        }
        Instance = this;
        Load();
    }

    void OnApplicationQuit() => Save();


    public void Save()
    {
        SaveGangPositions();

        try
        {
            string json = JsonUtility.ToJson(Data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LDY_SaveManager] 저장 실패: {e.Message}");
        }
    }

    // 씬에 GangManager가 떠 있으면(=맵 진입한 상태) 그 시점 currentNodeId를 전부 긁어서
    // Data.gangPositions에 채워넣는다. 맵 밖(로비 등)에서 저장되면 GangManager가 없으니
    // 마지막으로 저장돼 있던 위치를 그대로 둔다.
    private void SaveGangPositions()
    {
        if (LDY_GangManager.Instance == null)
            return;

        Data.gangPositions.Clear();
        foreach (LDY_GangController controller in LDY_GangManager.Instance.GetAllGangControllers())
        {
            if (controller.gangData == null)
                continue;

            Data.gangPositions.Add(new LDY_GangSaveEntry
            {
                gangName = controller.gangData.gangName,
                nodeId = controller.gangData.currentNodeId
            });
        }
    }

    // 갱단이 스폰될 때(LDY_GangController.Start) 저장된 위치가 있으면 그걸 돌려준다.
    public bool TryGetSavedGangNodeId(string gangName, out string nodeId)
    {
        foreach (LDY_GangSaveEntry entry in Data.gangPositions)
        {
            if (entry.gangName == gangName)
            {
                nodeId = entry.nodeId;
                return true;
            }
        }

        nodeId = null;
        return false;
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Data = new LDY_SaveData();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            Data = JsonUtility.FromJson<LDY_SaveData>(json) ?? new LDY_SaveData();
            // 예전 세이브 파일엔 gangPositions가 없을 수 있어서(구버전 호환) null이면 채워준다.
            if (Data.gangPositions == null)
                Data.gangPositions = new List<LDY_GangSaveEntry>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LDY_SaveManager] 불러오기 실패 – 초기화 진행: {e.Message}");
            Data = new LDY_SaveData();
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        Data = new LDY_SaveData();
        Debug.Log("[LDY_SaveManager] 저장 데이터 초기화 완료.");
    }

    public void SetMapProgress(string progressKey)
    {
        Data.mapProgress = progressKey;
    }   
}

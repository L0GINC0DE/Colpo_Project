using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class currentNodeIdDic
{
    public Dictionary<string, GangType> cniDictionary;
}

[Serializable]
public class SaveData
{
    public int IceItem       = 0;
    public int WallItem           = 0;
    public int NoiseItem      = 0;
    public int MoveItem  = 0;
    public int AttackItem = 0;
    
    public Dictionary<string, GangType> cniDictionary = new Dictionary<string, GangType>();

    public string mapProgress = "";

    public int  pityCount      = 0;
    public bool[] chapterCleared = new bool[4];
    public bool mailClaimed          = false;
    public bool beginnerGachaUsed   = false;


    public List<string> equippedDeck = new();
    
    public int normalModeClears = 0;

    public string lastTarotDate = "";
}


[DefaultExecutionOrder(-100)] // CurrencyManager / GachaManager 보다 먼저 초기화
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    const string SAVE_FILE = "save.json";

    string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE);

    public SaveData Data { get; private set; } = new SaveData();

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreatePersistentInstance()
    {
        var go = new GameObject("[SaveManager]");
        DontDestroyOnLoad(go);
        go.AddComponent<SaveManager>(); 
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
        try
        {
            string json = JsonUtility.ToJson(Data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
        }
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Data = new SaveData();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            Data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();

            if (Data.chapterCleared == null || Data.chapterCleared.Length < 4)
                Data.chapterCleared = new bool[4];
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 불러오기 실패 – 초기화 진행: {e.Message}");
            Data = new SaveData();
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        Data = new SaveData();
        Debug.Log("[SaveManager] 저장 데이터 초기화 완료.");
    }

    public void SetMapProgress(string progressKey)
    {
        Data.mapProgress = progressKey;
    }   
}

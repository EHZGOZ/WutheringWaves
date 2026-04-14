using System;
using System.IO;
using UnityEngine;
namespace WutheringWaves
{
    //public class SaveManager : MonoBehaviour
    //{
    //    // 单例实例（全局唯一）
    //    public static SaveManager Instance;

    //    //引用SO数据容器（在Inspector面板拖入创建好的PlayerData）
    //    [SerializeField] private PlayerDataSO playerDataSO;

    //    // 存档路径（不同平台自动适配，存在沙盒目录，卸载游戏才会删）
    //    private string savePath;

    //    private void Awake()
    //    {
    //        // 单例实现（确保全局唯一）
    //        if (Instance == null)
    //        {
    //            Instance = this;
    //            DontDestroyOnLoad(gameObject); // 切换场景不销毁
    //                                           // 初始化存档路径（比如Windows：C:\Users\XXX\AppData\Local\游戏名\save.json）
    //            savePath = Path.Combine(Application.persistentDataPath, "save.json");
    //        }
    //        else
    //        {
    //            Destroy(gameObject);
    //        }
    //    }

    //    // 游戏启动时自动加载存档
    //    private void Start()
    //    {
    //        LoadGameData();
    //    }

    //    /// <summary>
    //    /// 保存数据：把SO里的内存数据 → 本地Json文件
    //    /// </summary>
    //    public void SaveGameData()
    //    {
    //        try
    //        {
    //            // 1. 把SO的数据序列化为Json字符串
    //            string jsonData = JsonUtility.ToJson(playerDataSO, true); // true=格式化，方便调试
    //                                                                      // 2. 写入文件（覆盖旧存档）
    //            File.WriteAllText(savePath, jsonData);
    //            Debug.Log("存档成功！路径：" + savePath);
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.LogError("存档失败：" + e.Message);
    //        }
    //    }

    //    /// <summary>
    //    /// 加载数据：把本地Json文件 → SO的内存数据
    //    /// </summary>
    //    public void LoadGameData()
    //    {
    //        // 如果存档文件不存在，初始化默认数据
    //        if (!File.Exists(savePath))
    //        {
    //            Debug.Log("无存档，初始化默认数据");
    //            playerDataSO.ResetData();
    //            SaveGameData(); // 生成初始存档文件
    //            return;
    //        }

    //        try
    //        {
    //            // 1. 读取文件内容
    //            string jsonData = File.ReadAllText(savePath);
    //            // 2. 反序列化到SO（覆盖内存数据）
    //            JsonUtility.FromJsonOverwrite(jsonData, playerDataSO);
    //            Debug.Log("读档成功！");
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.LogError("读档失败，使用默认数据：" + e.Message);
    //            playerDataSO.ResetData();
    //        }
    //    }

    //    /// <summary>
    //    /// 删除存档（重置游戏用）
    //    /// </summary>
    //    public void DeleteSaveData()
    //    {
    //        if (File.Exists(savePath))
    //        {
    //            File.Delete(savePath);
    //            playerDataSO.ResetData();
    //            Debug.Log("存档已删除");
    //        }
    //    }
    //}
}



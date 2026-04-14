using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    // 存档数据：定义当前需要持久化的核心字段
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1; // 存档版本号
        public string sceneName = string.Empty; // 当前场景名

        public bool hasPlayerTransform = false; // 是否记录了玩家位姿
        public Vector3 playerPosition = Vector3.zero; // 玩家位置
        public Vector3 playerEulerAngles = Vector3.zero; // 玩家旋转（欧拉角）

        public static SaveData CreateDefault()
        {
            // 默认存档使用当前激活场景，避免初始数据缺少场景上下文。
            return new SaveData
            {
                saveVersion = 1,
                sceneName = SceneManager.GetActiveScene().name,
                hasPlayerTransform = false
            };
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    // 存档数据：定义当前需要持久化的核心字段
    [Serializable]
    public class SaveData
    {
        public string sceneName = string.Empty; // 当前场景名

        public List<CharacterName> teamCharacterIds = new();//队伍角色标识列表
        public List<CharacterRuntimeData> teamCharacterRuntimeData = new();//队伍角色数据
        public int currentCharacterIndex = 0; // 当前受控角色槽位
        
        public Vector3 playerPosition = Vector3.zero; // 玩家位置
        public Vector3 playerEulerAngles = Vector3.zero; // 玩家旋转（欧拉角）

        public static SaveData CreateDefault()
        {
            // 默认存档使用当前激活场景，避免初始数据缺少场景上下文。
            return new SaveData
            {
                sceneName = SceneManager.GetActiveScene().name,
                playerPosition = new Vector3(0f, 0f, -5f),
                playerEulerAngles = new Vector3(0f, 0f, 0f)
            };
        }
    }
}

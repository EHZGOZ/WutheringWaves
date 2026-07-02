using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    [System.Serializable]
    //队伍中角色信息
    public class TeamCharacterSlotData
    {
        [Header("角色名称")]
        public CharacterName characterName;

        [Header("角色运行时数据")]
        public CharacterRuntimeData runtimeData;
        // 克隆一份独立槽位数据，避免运行时数据和存档数据共享引用
        public TeamCharacterSlotData Clone()
        {
            return new TeamCharacterSlotData
            {
                characterName = characterName,
                runtimeData = runtimeData != null ? runtimeData.Clone() : new CharacterRuntimeData()
            };
        }
    }
    // 存档数据：定义当前需要持久化的核心字段
    [Serializable]
    public class SaveData
    {
        //场景
        public string sceneName = string.Empty; // 当前场景名

        //角色
        public List<TeamCharacterSlotData> teamSlots = new(); // 队伍槽位数据
        public int currentCharacterIndex = 0; // 当前受控角色槽位
        
        //位置
        public Vector3 playerPosition = Vector3.zero; // 玩家位置
        public Vector3 playerEulerAngles = Vector3.zero; // 玩家旋转（欧拉角）

        //背包
        public InventoryData inventory = new InventoryData();

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

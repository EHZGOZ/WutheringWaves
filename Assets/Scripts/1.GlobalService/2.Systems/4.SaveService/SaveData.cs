using System;
using System.Collections.Generic;
using UnityEngine;

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
        #region 存档数据
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
        #endregion

        #region 默认队伍配置
        // 默认队伍配置：只在创建默认存档时使用，不需要暴露给外部系统
        private static readonly CharacterName[] defaultTeamCharacterIds =
        {
            CharacterName.今汐,
            CharacterName.卡提希娅
         };
        #endregion

        #region 创建默认存档
        public static SaveData CreateDefault(
         string sceneName,
         Vector3 playerPosition,
         Vector3 playerEulerAngles)
        {
            // 创建默认存档基础数据
            SaveData saveData = new SaveData
            {
                sceneName = sceneName,
                playerPosition = playerPosition,
                playerEulerAngles = playerEulerAngles,
                currentCharacterIndex = 0,
                inventory = new InventoryData(),
                teamSlots = new List<TeamCharacterSlotData>()
            };

            //默认背包 暂无后续写

            //按默认角色配置写入队伍槽位
            for (int i = 0; i < defaultTeamCharacterIds.Length; i++)
            {
                CharacterName characterName = defaultTeamCharacterIds[i];

                saveData.teamSlots.Add(new TeamCharacterSlotData
                {
                    // 默认存档先写入角色名，生命值等基础属性由角色初始化时从CharacterDataSO补齐这样可以避免新账号角色出现0血、角色名默认值等异常数据
                    characterName = characterName,
                    runtimeData = new CharacterRuntimeData
                    {
                        characterName = characterName
                    }
                });
            }

            return saveData;
        }
        #endregion


    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    public class PlayerRuntimeData : MonoBehaviour
    {
        [Header("=== 运行时依赖 ===")]
        [SerializeField] private PlayerController playerController; // 玩家控制器

        [Header("=== 玩家运行时数据 ===")]
        [Header("当前场景名")]
        public string sceneName = string.Empty; // 当前场景名
        [Header("队伍角色列表")]
        public List<CharacterContext> teamCharacters = new(); // 运行时队伍角色列表
        [Header("队伍槽位数据")]
        public List<TeamCharacterSlotData> teamSlots = new(); // 队伍槽位数据
        [Header("当前受控角色槽位")]
        public int currentCharacterIndex = 0; // 当前受控角色索引

        [Header("玩家位置")]
        public Vector3 playerPosition = Vector3.zero; // 玩家位置
        public Vector3 playerEulerAngles = Vector3.zero; // 玩家旋转


        #region 初始化
        public void Injected(PlayerController playerController)
        {
            this.playerController = playerController;
            if (this.playerController == null)
            {
                this.playerController = FindObjectOfType<PlayerController>();
            }
        }
        #endregion

        #region 从 存档数据  中获取 运行数据
        // 从 存档数据  中获取 运行数据
        public void SyncRuntimeDataFromSaveData(SaveData saveData)
        {
            // 1.空值检查
            if (saveData == null)
            {
                return;
            }

            // 2.同步基础字段
            sceneName = saveData.sceneName;

            // 3.同步队伍槽位数据：克隆每个槽位，避免和存档对象共享引用
            teamSlots.Clear();
            if (saveData.teamSlots != null)
            {
                for (int i = 0; i < saveData.teamSlots.Count; i++)
                {
                    TeamCharacterSlotData slotData = saveData.teamSlots[i];
                    if (slotData == null)
                    {
                        continue;
                    }

                    teamSlots.Add(slotData.Clone());
                }
            }

            // 4.同步当前受控角色索引
            currentCharacterIndex = saveData.currentCharacterIndex;
            // 5.同步玩家位置旋转
            playerPosition = saveData.playerPosition;
            playerEulerAngles = saveData.playerEulerAngles;
        }
        #endregion

        #region 从 运行数据  中应用 场景数据
        // 从 运行数据  中应用 场景数据
        public void SyncSceneFromRuntimeData()
        {
            // 为角色数据赋值：将 PlayerRuntimeData 中的角色数据回填到场景角色
            ApplyTeamRuntimeData();
            // 移动玩家队伍位置：将场景中的队伍对象同步到 PlayerRuntimeData 记录的位置和朝向
            ApplyTeamPosition();
        }

        // 为角色数据赋值：将 PlayerRuntimeData 中的角色数据回填到场景角色
        public void ApplyTeamRuntimeData()
        {
            // 1.空应用检查
            if (teamSlots == null)
            {
                return;
            }

            // 2.按槽位数据回填运行时数据
            for (int i = 0; i < teamSlots.Count; i++)
            {
                TeamCharacterSlotData slotData = teamSlots[i];
                if (slotData == null)
                {
                    continue;
                }

                CharacterContext context = FindTeamCharacterContext(slotData.characterName);
                if (context == null || context.CharacterRuntimeData == null)
                {
                    continue;
                }

                context.CharacterRuntimeData.CopyFrom(slotData.runtimeData);
            }
        }

        // 移动玩家队伍位置：将场景中的队伍对象同步到 PlayerRuntimeData 记录的位置和朝向
        public void ApplyTeamPosition()
        {
            // 1.队伍列表为空直接返回
            if (teamCharacters == null || teamCharacters.Count == 0)
            {
                return;
            }

            // 2.统一把队伍移动到当前记录的玩家落点
            for (int i = 0; i < teamCharacters.Count; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null)
                {
                    continue;
                }

                context.transform.position = playerPosition;
                context.transform.rotation = Quaternion.Euler(playerEulerAngles);
            }
        }
        #endregion

        #region  从 场景数据 中获取 运行数据
        //  从 场景数据 中获取 运行数据
        public void SyncRuntimeDataFromScene()
        {
            // 1.同步基础字段
            sceneName = SceneManager.GetActiveScene().name;

            if (playerController != null)
            {
                currentCharacterIndex = ResolveCurrentCharacterIndex();
            }

            if (playerController != null && playerController.CurrentCharacterContext != null)
            {
                playerPosition = playerController.CurrentCharacterContext.transform.position;
                playerEulerAngles = playerController.CurrentCharacterContext.transform.eulerAngles;
            }

            // 2.重新收集队伍槽位数据
            teamSlots.Clear();

            for (int i = 0; i < teamCharacters.Count; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null || context.CharacterDataSO == null || context.CharacterRuntimeData == null)
                {
                    continue;
                }

                teamSlots.Add(new TeamCharacterSlotData
                {
                    characterName = context.CharacterDataSO.characterName,
                    runtimeData = context.CharacterRuntimeData.Clone()
                });
            }
        }
        #endregion

        #region 从 运行数据 中应用 存档数据
        // 从 运行数据 中应用 存档数据
        public void SyncSaveDataFromRuntimeData(SaveData saveData)
        {
            // 1.空值检查
            if (saveData == null)
            {
                return;
            }

            // 2.同步基础存档字段
            saveData.sceneName = sceneName;

            // 3.同步队伍槽位数据：克隆每个槽位，避免和运行时对象共享引用
            saveData.teamSlots.Clear();
            for (int i = 0; i < teamSlots.Count; i++)
            {
                TeamCharacterSlotData slotData = teamSlots[i];
                if (slotData == null)
                {
                    continue;
                }

                saveData.teamSlots.Add(slotData.Clone());
            }

            // 4.同步当前受控角色索引
            saveData.currentCharacterIndex = currentCharacterIndex;
            // 5.同步玩家位置旋转
            saveData.playerPosition = playerPosition;
            saveData.playerEulerAngles = playerEulerAngles;
        }
        #endregion

        #region 刷新运行时玩家控制队伍角色列表
        // 刷新运行时玩家控制队伍角色列表
        private void RefreshRuntimeTeamCharacters()
        {
            teamCharacters.Clear();

            if (playerController == null || playerController.TeamCharacters == null)
            {
                return;
            }

            for (int i = 0; i < playerController.TeamCharacters.Length; i++)
            {
                CharacterContext context = playerController.TeamCharacters[i];
                if (context == null)
                {
                    continue;
                }

                teamCharacters.Add(context);
            }
        }
        #endregion

        // 按角色标识查找当前场景中的队伍角色
        private CharacterContext FindTeamCharacterContext(CharacterName characterName)
        {
            for (int i = 0; i < teamCharacters.Count; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null || context.CharacterDataSO == null)
                {
                    continue;
                }

                if (context.CharacterDataSO.characterName == characterName)
                {
                    return context;
                }
            }

            return null;
        }

        // 解析当前受控角色索引
        private int ResolveCurrentCharacterIndex()
        {
            if (playerController == null || playerController.TeamCharacters == null || playerController.CurrentCharacterContext == null)
            {
                return currentCharacterIndex;
            }

            for (int i = 0; i < playerController.TeamCharacters.Length; i++)
            {
                if (playerController.TeamCharacters[i] == playerController.CurrentCharacterContext)
                {
                    return i;
                }
            }

            return currentCharacterIndex;
        }

    }
}

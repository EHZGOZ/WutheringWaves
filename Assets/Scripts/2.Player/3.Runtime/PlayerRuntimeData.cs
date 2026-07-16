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

        #region 生命周期


        #endregion

        #region 初始化

        #endregion

        #region 角色绑定
        public void Bind(PlayerController playerController)
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
            if (teamSlots == null)
            {
                teamSlots = new List<TeamCharacterSlotData>();
            }

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

        #region  从 场景数据 中获取 运行数据
        //  从 场景数据 中获取 运行数据
        public void SyncRuntimeDataFromScene()
        {
            // 1.兜底获取玩家控制器，保证存档前能拿到当前玩家状态
            if (playerController == null)
            {
                playerController = PlayerController.Instance;
            }

            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }

            // 2.刷新当前运行时队伍角色列表，保证后续收集的是最新队伍对象
            RefreshRuntimeTeamCharacters();

            // 3.同步当前场景名
            sceneName = SceneManager.GetActiveScene().name;

            // 4.同步当前受控角色索引
            if (playerController != null)
            {
                // 解析当前受控角色索引
                currentCharacterIndex = ResolveCurrentCharacterIndex();
            }

            // 5.同步当前受控角色位置和旋转：玩家父节点不移动，所以以当前角色为准
            if (playerController != null && playerController.CurrentCharacterContext != null)
            {
                playerPosition = playerController.CurrentCharacterContext.transform.position;
                playerEulerAngles = playerController.CurrentCharacterContext.transform.eulerAngles;
            }

            // 6.确保队伍槽位列表存在，避免后续写入空引用
            if (teamSlots == null)
            {
                teamSlots = new List<TeamCharacterSlotData>();
            }

            // 7.按队伍顺序刷新角色运行时数据：只覆盖已经初始化过的角色，避免下场未初始化角色用空数据覆盖存档
            for (int i = 0; i < teamCharacters.Count; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (!CanCollectCharacterRuntimeData(context))
                {
                    continue;
                }

                UpdateTeamSlotFromCharacterContext(i, context);
            }
        }


        // 刷新运行时玩家控制队伍角色列表
        private void RefreshRuntimeTeamCharacters()
        {
            if (teamCharacters == null)
            {
                teamCharacters = new List<CharacterContext>();
            }

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

        // 判断角色运行时数据是否可以被收集：未初始化角色不参与覆盖，避免存档数据被0血空数据污染
        private bool CanCollectCharacterRuntimeData(CharacterContext context)
        {
            if (context == null || context.CharacterDataSO == null || context.RuntimeData == null)
            {
                return false;
            }

            return context.RuntimeData.maxHealth > 0f;
        }

        // 按队伍槽位写入角色运行时数据，保证teamSlots顺序和PlayerController.TeamCharacters顺序一致
        private void UpdateTeamSlotFromCharacterContext(int slotIndex, CharacterContext context)
        {
            if (slotIndex < 0 || context == null)
            {
                return;
            }

            // 1.补齐槽位数量，避免队伍角色数量大于当前teamSlots数量时无法写入
            while (teamSlots.Count <= slotIndex)
            {
                teamSlots.Add(new TeamCharacterSlotData());
            }

            // 2.确保当前槽位对象存在
            if (teamSlots[slotIndex] == null)
            {
                teamSlots[slotIndex] = new TeamCharacterSlotData();
            }

            // 3.用场景中的角色真实数据覆盖对应槽位
            CharacterRuntimeData runtimeData = context.RuntimeData.Clone();
            runtimeData.characterName = context.CharacterDataSO.characterName;

            teamSlots[slotIndex].characterName = context.CharacterDataSO.characterName;
            teamSlots[slotIndex].runtimeData = runtimeData;
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

            // 3.同步队伍槽位数据：克隆每个槽位，避免存档数据和运行时数据共享引用
            if (saveData.teamSlots == null)
            {
                saveData.teamSlots = new List<TeamCharacterSlotData>();
            }

            saveData.teamSlots.Clear();
            if (teamSlots != null)
            {
                for (int i = 0; i < teamSlots.Count; i++)
                {
                    TeamCharacterSlotData slotData = teamSlots[i];
                    if (slotData == null)
                    {
                        continue;
                    }

                    saveData.teamSlots.Add(slotData.Clone());
                }
            }

            // 4.同步当前受控角色索引
            saveData.currentCharacterIndex = currentCharacterIndex;

            // 5.同步玩家位置旋转
            saveData.playerPosition = playerPosition;
            saveData.playerEulerAngles = playerEulerAngles;
        }
        #endregion

    }
}

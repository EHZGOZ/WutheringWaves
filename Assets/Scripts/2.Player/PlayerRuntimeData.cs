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
        public List<GameObject> teamCharacters = new(); // 运行时队伍角色列表
        [Header("队伍角色标识列表")]
        public List<CharacterName> teamCharacterIds = new(); // 队伍角色标识列表
        [Header("队伍角色数据")]
        public List<CharacterRuntimeData> teamCharacterRuntimeData = new(); // 队伍角色数据
        [Header("当前受控角色槽位")]
        public int currentCharacterIndex = 0; // 当前受控角色索引
        [Header("玩家位置")]
        public Vector3 playerPosition = Vector3.zero; // 玩家位置
        public Vector3 playerEulerAngles = Vector3.zero; // 玩家旋转

        #region 初始化
      
        public void Injected(PlayerController playerController)
        {
            this.playerController = playerController;
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }
        }
        #endregion

        #region 读档
        // 读档：将 SaveData 中的数据复制到当前 PlayerRuntimeData
        public void LoadPlayerRuntimeData(SaveData saveData)
        {
            // 1.空值检查
            if (saveData == null)
            {
                return;
            }


            // 3.同步基础字段
            sceneName = saveData.sceneName;
            // 4.同步队伍角色标识列表：使用 AddRange 复制列表内容，避免共享同一引用
            teamCharacterIds.Clear();
            if (saveData.teamCharacterIds != null)
            {
                teamCharacterIds.AddRange(saveData.teamCharacterIds);
            }

            // 5.同步队伍角色数据：克隆每个角色运行时数据，避免和存档对象共享引用
            teamCharacterRuntimeData.Clear();
            if (saveData.teamCharacterRuntimeData != null)
            {
                for (int i = 0; i < saveData.teamCharacterRuntimeData.Count; i++)
                {
                    CharacterRuntimeData runtimeData = saveData.teamCharacterRuntimeData[i];
                    if (runtimeData == null)
                    {
                        continue;
                    }

                    teamCharacterRuntimeData.Add(runtimeData.Clone());
                }
            }
            currentCharacterIndex = saveData.currentCharacterIndex;
            playerPosition = saveData.playerPosition;
            playerEulerAngles = saveData.playerEulerAngles;

            
           

            // 6.刷新运行时队伍角色列表
            RefreshRuntimeTeamCharacters();
            // 7.将角色数据回填到场景中的角色对象
            ApplyTeamRuntimeData();
            // 8.移动玩家队伍到读档位置
            ApplyTeamPosition();
        }

        // 为角色数据赋值：将 PlayerRuntimeData 中的角色数据回填到场景角色
        public void ApplyTeamRuntimeData()
        {
            // 1.空应用检查
            if (teamCharacterIds == null || teamCharacterRuntimeData == null)
            {
                return;
            }

            int count = Mathf.Min(teamCharacterIds.Count, teamCharacterRuntimeData.Count);
            // 2.按角色标识回填运行时数据
            for (int i = 0; i < count; i++)
            {
                CharacterFacade facade = FindTeamCharacterFacade(teamCharacterIds[i]);
                if (facade == null || facade.Context == null || facade.Context.CharacterRuntimeData == null)
                {
                    continue;
                }

                facade.Context.CharacterRuntimeData.CopyFrom(teamCharacterRuntimeData[i]);
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
                GameObject teamCharacter = teamCharacters[i];
                if (teamCharacter == null)
                {
                    continue;
                }

                teamCharacter.transform.position = playerPosition;
                teamCharacter.transform.rotation = Quaternion.Euler(playerEulerAngles);
            }
        }

       
        #endregion

        #region 存档
        // 存档：将当前 PlayerRuntimeData 中的数据复制到 SaveData
        public void CollectTeamRuntimeData(SaveData saveData)
        {
            // 1.空应用检查
            if (saveData == null)
            {
                return;
            }


            RefreshRuntimeTeamCharacters();

            // 3.先从场景中的玩家和角色对象刷新当前运行时数据
            SyncRuntimeDataFromScene();

            // 4.同步基础存档字段
            saveData.sceneName = sceneName;
            saveData.currentCharacterIndex = currentCharacterIndex;
            saveData.playerPosition = playerPosition;
            saveData.playerEulerAngles = playerEulerAngles;

            // 5.清除旧记录
            saveData.teamCharacterIds.Clear();
            saveData.teamCharacterRuntimeData.Clear();

            // 6.复制队伍角色标识列表
            saveData.teamCharacterIds.AddRange(teamCharacterIds);

            // 7.复制队伍角色数据：克隆每个角色运行时数据，避免存档和运行时共享引用
            for (int i = 0; i < teamCharacterRuntimeData.Count; i++)
            {
                CharacterRuntimeData runtimeData = teamCharacterRuntimeData[i];
                if (runtimeData == null)
                {
                    continue;
                }

                saveData.teamCharacterRuntimeData.Add(runtimeData.Clone());
            }
        }
        #endregion

        #region 内部工具


        // 刷新运行时队伍角色列表：GameObject 只来自场景，不参与存档
        private void RefreshRuntimeTeamCharacters()
        {
            teamCharacters.Clear();

            if (playerController == null || playerController.TeamCharacters == null)
            {
                return;
            }

            for (int i = 0; i < playerController.TeamCharacters.Length; i++)
            {
                CharacterFacade facade = playerController.TeamCharacters[i];
                if (facade == null)
                {
                    continue;
                }

                teamCharacters.Add(facade.gameObject);
            }
        }

        // 从场景中的真实角色对象同步当前运行时数据，保证 PlayerRuntimeData 始终记录最新状态
        private void SyncRuntimeDataFromScene()
        {
            // 1.同步基础字段
            sceneName = SceneManager.GetActiveScene().name;

            if (playerController != null)
            {
                currentCharacterIndex = playerController.CurrentCharacterIndex;
            }

            if (playerController != null && playerController.CurrentCharacterFacade != null)
            {
                playerPosition = playerController.CurrentCharacterFacade.transform.position;
                playerEulerAngles = playerController.CurrentCharacterFacade.transform.eulerAngles;
            }

            // 2.重新收集队伍角色标识和角色运行时数据
            teamCharacterIds.Clear();
            teamCharacterRuntimeData.Clear();

            for (int i = 0; i < teamCharacters.Count; i++)
            {
                GameObject teamCharacter = teamCharacters[i];
                if (teamCharacter == null)
                {
                    continue;
                }

                CharacterFacade facade = teamCharacter.GetComponent<CharacterFacade>();
                if (facade == null || facade.CharacterDataSO == null || facade.Context == null || facade.Context.CharacterRuntimeData == null)
                {
                    continue;
                }

                CharacterRuntimeData runtimeData = facade.Context.CharacterRuntimeData.Clone();
                runtimeData.Position = facade.transform.position;
                runtimeData.EulerAngles = facade.transform.eulerAngles;

                teamCharacterIds.Add(facade.CharacterDataSO.characterName);
                teamCharacterRuntimeData.Add(runtimeData);
            }
        }

        // 按角色标识查找当前场景中的队伍角色
        private CharacterFacade FindTeamCharacterFacade(CharacterName characterName)
        {
            for (int i = 0; i < teamCharacters.Count; i++)
            {
                GameObject teamCharacter = teamCharacters[i];
                if (teamCharacter == null)
                {
                    continue;
                }

                CharacterFacade facade = teamCharacter.GetComponent<CharacterFacade>();
                if (facade == null || facade.CharacterDataSO == null)
                {
                    continue;
                }

                if (facade.CharacterDataSO.characterName == characterName)
                {
                    return facade;
                }
            }

            return null;
        }
        #endregion
    }
}

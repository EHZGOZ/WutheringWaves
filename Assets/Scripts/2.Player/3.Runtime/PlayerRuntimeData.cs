using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    public class PlayerRuntimeData : MonoBehaviour
    {
        [Header("=== 玩家运行时数据 ===")]
        [Header("当前场景名")]
        [SerializeField] private string sceneName = string.Empty; // 当前场景名
        [Header("队伍槽位数据")]
        [SerializeField] private List<TeamCharacterSlotData> teamSlots = new(); // 队伍槽位数据
        [Header("当前受控角色槽位")]
        [SerializeField] private int currentCharacterIndex = 0; // 当前受控角色索引
        [Header("玩家位置")]
        [SerializeField] private Vector3 playerPosition = Vector3.zero; // 玩家位置
        [Header("玩家旋转")]
        [SerializeField] private Vector3 playerEulerAngles = Vector3.zero; // 玩家旋转

        #region 外部访问
        public bool IsInitialized { get; private set; }
        public string SceneName => sceneName;
        public IReadOnlyList<TeamCharacterSlotData> TeamSlots => teamSlots;
        public int CurrentCharacterIndex => currentCharacterIndex;
        public Vector3 PlayerPosition => playerPosition;
        public Vector3 PlayerEulerAngles => playerEulerAngles;
        #endregion

        #region 生命周期
        private void OnDestroy()
        {
            // 组件销毁时解绑运行数据事件，避免静态事件残留引用
            UnsubscribeRuntimeEvents();

            // 标记运行数据组件已经失效
            IsInitialized = false;
        }
        #endregion

        #region 初始化
        // 初始化玩家运行时数据：补齐数据、校验数据并订阅同步事件
        public void Initialize()
        {
            // 1.已经初始化时不重复订阅事件
            if (IsInitialized)
            {
                return;
            }

            // 2.补齐玩家运行时数据容器
            ResolveData();

            // 3.校验运行时数据是否可用
            if (!ValidateData())
            {
                IsInitialized = false;
                return;
            }

            // 4.订阅玩家运行数据和场景变化事件
            SubscribeRuntimeEvents();

            // 5.标记初始化完成
            IsInitialized = true;
        }

        // 补齐玩家运行时数据容器
        private void ResolveData()
        {
            // 1.确保队伍槽位列表存在
            if (teamSlots == null)
            {
                teamSlots = new List<TeamCharacterSlotData>();
            }

            // 2.场景名称为空时使用当前激活场景作为兜底
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                sceneName = SceneManager.GetActiveScene().name;
            }
        }

        // 校验玩家运行时数据
        private bool ValidateData()
        {
            bool isValid = true;

            // 1.校验队伍槽位列表
            if (teamSlots == null)
            {
                Debug.LogError("[PlayerRuntimeData] 初始化失败：teamSlots为空。", this);
                isValid = false;
            }

            // 2.校验队伍槽位数量
            if (teamSlots != null && teamSlots.Count == 0)
            {
                Debug.LogError("[PlayerRuntimeData] 初始化失败：teamSlots数量为0。", this);
                isValid = false;
            }

            return isValid;
        }
        #endregion

        #region 玩家运行数据事件
        // 订阅玩家运行数据和场景变化事件
        private void SubscribeRuntimeEvents()
        {
            // 1.先移除再添加，避免重复初始化造成重复订阅
            PlayerRuntimeEvents.OnCurrentCharacterIndexChanged -= HandleCurrentCharacterIndexChanged;
            PlayerRuntimeEvents.OnCurrentCharacterIndexChanged +=HandleCurrentCharacterIndexChanged;
               
            // 2.订阅当前角色位置旋转变化事件
            PlayerRuntimeEvents.OnPlayerTransformChanged -= HandlePlayerTransformChanged; 
            PlayerRuntimeEvents.OnPlayerTransformChanged +=HandlePlayerTransformChanged;           

            // 3.订阅Unity当前激活场景变化事件
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        // 解绑玩家运行数据和场景变化事件
        private void UnsubscribeRuntimeEvents()
        {
            PlayerRuntimeEvents.OnCurrentCharacterIndexChanged -=  HandleCurrentCharacterIndexChanged;
              
            PlayerRuntimeEvents.OnPlayerTransformChanged -=HandlePlayerTransformChanged;
                
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        }

        // 处理当前受控角色索引变化
        private void HandleCurrentCharacterIndexChanged(int characterIndex)
        {
            // 1.队伍槽位不存在时不能更新索引
            if (teamSlots == null || teamSlots.Count == 0)
            {
                Debug.LogError("[PlayerRuntimeData] 更新当前角色索引失败：teamSlots为空。", this);
                return;
            }

            // 2.索引超出队伍范围时拒绝写入
            if (characterIndex < 0 || characterIndex >= teamSlots.Count)
            {
                Debug.LogError(
                    $"[PlayerRuntimeData] 更新当前角色索引失败：索引非法。characterIndex = {characterIndex}",
                    this
                );
                return;
            }

            // 3.更新当前受控角色索引
            currentCharacterIndex = characterIndex;
        }

        // 处理当前角色位置旋转变化
        private void HandlePlayerTransformChanged( Vector3 position, Vector3 eulerAngles  )
        {
            // 1.更新玩家当前世界位置
            playerPosition = position;

            // 2.更新玩家当前欧拉角旋转
            playerEulerAngles = eulerAngles;
        }

        // 处理Unity激活场景变化
        private void HandleActiveSceneChanged(Scene previousScene, Scene currentScene)
        {
            // 1.新场景无效时不更新场景名称
            if (!currentScene.IsValid())
            {
                return;
            }

            // 2.同步当前激活场景名称
            sceneName = currentScene.name;
        }
        #endregion

        #region 数据获取与应用

        #region 从 存档数据 中获取 运行数据(SaveData → PlayerRuntimeData)
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

        #region  从 场景数据 中获取 运行数据(当前场景 → PlayerRuntimeData)已废弃
        ////  从 场景数据 中获取 运行数据
        //public void SyncRuntimeDataFromScene()
        //{
        //    // 同步当前场景名
        //    sceneName = SceneManager.GetActiveScene().name;

        //    //确保队伍槽位列表存在，避免后续写入空引用
        //    if (teamSlots == null)
        //    {
        //        teamSlots = new List<TeamCharacterSlotData>();
        //    }
        //    // 按队伍顺序刷新角色运行时数据：只覆盖已经初始化过的角色，避免下场未初始化角色用空数据覆盖存档
        //    for (int i = 0; i < playerController.TeamCharacters.Length; i++)
        //    {
        //        CharacterContext context = playerController.TeamCharacters[i];
        //        if (!CanCollectCharacterRuntimeData(context))
        //        {
        //            continue;
        //        }

        //        UpdateTeamSlotFromCharacterContext(i, context);
        //    }

        //    //同步当前受控角色索引
        //    currentCharacterIndex = ResolveCurrentCharacterIndex();

        //    // 同步当前受控角色位置和旋转：玩家父节点不移动，所以以当前角色为准
        //    playerPosition = playerController.CurrentCharacterContext.transform.position;
        //    playerEulerAngles = playerController.CurrentCharacterContext.transform.eulerAngles; 
        //}

        //// 解析当前受控角色索引
        //private int ResolveCurrentCharacterIndex()
        //{
        //    if (playerController == null || playerController.TeamCharacters == null || playerController.CurrentCharacterContext == null)
        //    {
        //        return currentCharacterIndex;
        //    }

        //    for (int i = 0; i < playerController.TeamCharacters.Length; i++)
        //    {
        //        if (playerController.TeamCharacters[i] == playerController.CurrentCharacterContext)
        //        {
        //            return i;
        //        }
        //    }

        //    return currentCharacterIndex;
        //}

        //// 判断角色运行时数据是否可以被收集：未初始化角色不参与覆盖，避免存档数据被0血空数据污染
        //private bool CanCollectCharacterRuntimeData(CharacterContext context)
        //{
        //    if (context == null || context.CharacterDataSO == null || context.RuntimeData == null)
        //    {
        //        return false;
        //    }

        //    return context.RuntimeData.maxHealth > 0f;
        //}

        //// 按队伍槽位写入角色运行时数据，保证teamSlots顺序和PlayerController.TeamCharacters顺序一致
        //private void UpdateTeamSlotFromCharacterContext(int slotIndex, CharacterContext context)
        //{
        //    if (slotIndex < 0 || context == null)
        //    {
        //        return;
        //    }

        //    // 1.补齐槽位数量，避免队伍角色数量大于当前teamSlots数量时无法写入
        //    while (teamSlots.Count <= slotIndex)
        //    {
        //        teamSlots.Add(new TeamCharacterSlotData());
        //    }

        //    // 2.确保当前槽位对象存在
        //    if (teamSlots[slotIndex] == null)
        //    {
        //        teamSlots[slotIndex] = new TeamCharacterSlotData();
        //    }

        //    // 3.用场景中的角色真实数据覆盖对应槽位
        //    CharacterRuntimeData runtimeData = context.RuntimeData.Clone();
        //    runtimeData.characterName = context.CharacterDataSO.characterName;

        //    teamSlots[slotIndex].characterName = context.CharacterDataSO.characterName;
        //    teamSlots[slotIndex].runtimeData = runtimeData;
        //}
        #endregion

        #region 从 运行数据 中应用 存档数据(PlayerRuntimeData → SaveData)
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

        #endregion

    }
}

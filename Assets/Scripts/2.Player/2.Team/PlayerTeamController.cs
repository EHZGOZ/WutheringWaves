using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 玩家队伍控制器：负责队伍角色的生成、清理和运行数据注入
    public class PlayerTeamController : MonoBehaviour
    {
        #region 核心引用
        private PlayerController playerController; // 玩家总控制器
        private PlayerRuntimeData playerRuntimeData; // 当前账号对应的玩家运行时数据
        private Transform characterParent; // 队伍角色生成后的父节点
        #endregion

        #region 队伍角色
        [Header("=== 队伍角色 ===")]
        [Header("运行时队伍角色列表（不可手动修改）")]
        [SerializeField] private CharacterContext[] teamCharacters = new CharacterContext[0];
        #endregion

        #region 外部访问
        public bool IsInitialized { get; private set; }

        // 对外只读访问运行时队伍，外部不能整体替换数组
        public IReadOnlyList<CharacterContext> TeamCharacters => teamCharacters;

        // 对外只读访问当前队伍角色数量
        public int TeamCharacterCount => teamCharacters != null ? teamCharacters.Length   : 0;
        #endregion

        #region 初始化
        // 初始化队伍控制器：注入玩家运行时数据和角色父节点
        public void Initialize(PlayerController playerController)
        {
            //缓存数据
            ResolveComponent(playerController);
            //校验核心依赖
            if (!ValidateDependencies())
            {
                IsInitialized = false;
                return;
            }
            //标记初始化完成
            IsInitialized = true;
        }
        //缓存数据
        private void ResolveComponent(PlayerController playerController)
        {
            this.playerController = playerController;
            // 1.缓存队伍生成需要的运行时数据
            playerRuntimeData = playerController.PlayerRuntimeData;
            // 2.缓存角色生成后的父节点
            characterParent = playerController.transform;
        }
        //校验核心依赖
        private bool ValidateDependencies()
        {
            bool isValid = true;

            // 校验玩家运行数据
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerTeamController] 初始化失败：playerRuntimeData为空。", this);
                isValid = false;
            }
            // 校验玩家运行数据
            if (characterParent == null)
            {
                Debug.LogError("[PlayerTeamController] 初始化失败：characterParent为空。", this);
                isValid = false;
            }


            return isValid;
        }
        #endregion

        #region 清理队伍
        // 清理当前已经生成的队伍角色
        public void ClearTeamCharacters()
        {
            // 1.队伍数组为空时只创建空数组
            if (teamCharacters == null)
            {
                teamCharacters = new CharacterContext[0];
                return;
            }

            // 2.逐个禁用并销毁已经生成的角色对象
            for (int i = 0; i < teamCharacters.Length; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null)
                {
                    continue;
                }

                // 先禁用角色，立即停止Update、LateUpdate和位置事件发布
                context.gameObject.SetActive(false);

                // 再安排角色对象在帧末销毁
                Destroy(context.gameObject);
                teamCharacters[i] = null;
            }

            // 3.重置为空数组，避免外部继续访问旧队伍
            teamCharacters = new CharacterContext[0];
        }
        #endregion

        #region 生成队伍
        // 根据玩家运行时数据生成完整队伍
        public bool SpawnTeam()
        {
            // 1.生成前进行完整校验
            if (!CanSpawnTeam())
            {
                return false;
            }

            // 2.清理当前队伍
            ClearTeamCharacters();

            // 3.按照队伍槽位数量创建运行时角色数组
            // 数组索引必须与teamSlots保持一致，避免角色对象和槽位数据错位
            teamCharacters = new CharacterContext[playerRuntimeData.TeamSlots.Count];

            // 4.按照队伍槽位顺序逐个生成并初始化角色
            for (int i = 0; i < playerRuntimeData.TeamSlots.Count; i++)
            {
                // 5.直接读取PlayerRuntimeData持有的队伍槽位
                TeamCharacterSlotData slotData = playerRuntimeData.TeamSlots[i];
                if (slotData == null)
                {
                    Debug.LogError(
                        $"[PlayerTeamController] 队伍生成失败：槽位 {i} 为空。",
                        this
                    );

                    ClearTeamCharacters();
                    return false;
                }

                // 6.直接读取当前槽位持有的角色运行时数据
                // 不在角色生成阶段创建新数据，确保PlayerRuntimeData是唯一数据来源
                CharacterRuntimeData runtimeData = slotData.runtimeData;
                if (runtimeData == null)
                {
                    Debug.LogError(
                        $"[PlayerTeamController] 队伍生成失败：槽位 {i} 的CharacterRuntimeData为空。",
                        this
                    );

                    ClearTeamCharacters();
                    return false;
                }

                // 7.根据当前槽位数据生成角色对象
                CharacterContext context = SpawnSingleCharacter(slotData);
                if (context == null)
                {
                    Debug.LogError(
                        $"[PlayerTeamController] 队伍生成失败：角色 {slotData.characterName} 生成失败。",
                        this
                    );

                    ClearTeamCharacters();
                    return false;
                }

                // 8.先记录生成结果，保证后续失败时能够清理已经生成的角色
                teamCharacters[i] = context;

                // 9.将当前槽位中的同一份运行时数据绑定给角色上下文
                context.Initialize(runtimeData);
            }

            return true;
        }
        // 判断当前是否可以生成队伍
        private bool CanSpawnTeam()
        {
            // 1.队伍控制器尚未初始化时不能生成
            if (!IsInitialized)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍：组件尚未初始化。", this);
                return false;
            }

            // 2.玩家运行时数据为空时不能读取槽位
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍：PlayerRuntimeData为空。", this);
                return false;
            }

            // 3.队伍槽位为空时不能确定生成内容
            if (playerRuntimeData.TeamSlots == null)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍：teamSlots为空。", this);
                return false;
            }

            // 4.队伍槽位数量为0时不能生成角色
            if (playerRuntimeData.TeamSlots.Count == 0)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍：teamSlots数量为0。", this);
                return false;
            }

            // 5.角色生成服务为空时不能获取角色预制体
            if (CharacterSpawnService.Instance == null)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍：CharacterSpawnService为空。", this);
                return false;
            }

            // 6.角色生成服务尚未初始化时不能生成角色
            if (!CharacterSpawnService.Instance.IsInitialized)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍：CharacterSpawnService尚未初始化。", this);
                return false;
            }

            return true;
        }

        // 根据单个队伍槽位生成角色
        private CharacterContext SpawnSingleCharacter(TeamCharacterSlotData slotData)
        {
            // 1.槽位数据为空时停止生成
            if (slotData == null)
            {
                return null;
            }

            // 2.根据角色名称、存档位置和父节点生成角色对象
            GameObject character = CharacterSpawnService.Instance.SpawnCharacter(
                slotData.characterName,
                playerRuntimeData.PlayerPosition,
                Quaternion.Euler(playerRuntimeData.PlayerEulerAngles),
                characterParent
            );
            if (character == null)
            {
                return null;
            }

            // 3.获取角色共享上下文
            CharacterContext context = character.GetComponent<CharacterContext>();
            if (context == null)
            {
                Debug.LogError(
                    $"[PlayerTeamController] 角色 {slotData.characterName} 缺少CharacterContext组件。",
                    character
                );

                Destroy(character);
                return null;
            }

            // 4.角色显隐交给PlayerCharacterSwitcher统一处理
            return context;
        }
        #endregion

        #region 获取队伍角色（外部调用）
        // 根据队伍索引获取运行时角色
        public CharacterContext GetCharacter(int characterIndex)
        {
            // 1.队伍数组为空时返回空
            if (teamCharacters == null)
            {
                return null;
            }

            // 2.索引非法时返回空
            if (characterIndex < 0 || characterIndex >= teamCharacters.Length)
            {
                return null;
            }

            return teamCharacters[characterIndex];
        }
        #endregion

        // 统一推进队伍中所有角色的运行时数据
        public void UpdateTeamCharacterRuntimeData(float deltaTime)
        {
            // 1.队伍为空时不更新
            if (teamCharacters == null || teamCharacters.Length == 0)
            {
                return;
            }

            // 2.逐个推进角色运行时数据
            for (int i = 0; i < teamCharacters.Length; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null || context.RuntimeData == null)
                {
                    continue;
                }

                context.RuntimeData.UpdateRuntime(deltaTime);
            }
        }

    }
}
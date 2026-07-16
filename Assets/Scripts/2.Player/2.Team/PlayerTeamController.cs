using UnityEngine;

namespace WutheringWaves
{
    // 玩家队伍控制器：负责队伍角色的生成、清理和运行数据注入
    public class PlayerTeamController : MonoBehaviour
    {
        #region 核心引用
        private PlayerRuntimeData playerRuntimeData; // 当前账号对应的玩家运行数据
        private Transform characterParent; // 队伍角色生成后的父节点
        #endregion

        #region 队伍运行数据
        [Header("队伍角色列表")]
        [SerializeField] private CharacterContext[] teamCharacters; // 数组索引与存档队伍槽位保持一致
        #endregion

        #region 外部访问
        public bool IsInitialized { get; private set; }
        public CharacterContext[] TeamCharacters => teamCharacters; // 外部只读访问当前队伍角色
        #endregion

        #region 初始化
        // 初始化队伍控制器：注入玩家运行数据和角色父节点
        public void Initialize(PlayerRuntimeData playerRuntimeData, Transform characterParent)
        {
            // 1.缓存玩家运行数据，后续按存档槽位生成队伍
            this.playerRuntimeData = playerRuntimeData;

            // 2.优先使用外部指定的角色父节点，缺失时使用当前组件节点兜底
            this.characterParent = characterParent != null
                ? characterParent
                : transform;

            // 3.玩家运行数据为空时初始化失败，避免后续生成无来源队伍
            if (this.playerRuntimeData == null)
            {
                IsInitialized = false;
                Debug.LogError("[PlayerTeamController] 初始化失败：PlayerRuntimeData为空。", this);
                return;
            }

            // 4.标记队伍控制器初始化完成
            IsInitialized = true;
        }
        #endregion

        #region 清理队伍角色
        // 清理当前已经生成的队伍角色：新建、读档或者更换队伍时调用
        public void ClearTeamCharacters()
        {
            // 1.队伍数组为空时不需要清理
            if (teamCharacters == null)
            {
                return;
            }

            // 2.逐个销毁队伍角色对象
            for (int i = 0; i < teamCharacters.Length; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null)
                {
                    continue;
                }

                Destroy(context.gameObject);
                teamCharacters[i] = null;
            }

            // 3.清空队伍数组引用，避免后续继续访问旧角色
            teamCharacters = null;
        }
        #endregion

        #region 生成队伍角色
        // 根据玩家运行数据中的队伍槽位生成全部角色
        public bool SpawnTeamCharacters()
        {
            // 1.生成前进行完整校验
            if (!CanSpawnTeamCharacters())
            {
                return false;
            }

            // 2.按照存档槽位数量创建队伍数组
            // 数组索引必须与teamSlots保持一致，避免空槽位导致角色数据错位
            teamCharacters = new CharacterContext[playerRuntimeData.teamSlots.Count];

            bool allCharactersSpawned = true;

            // 3.按存档槽位索引逐个生成角色
            for (int i = 0; i < playerRuntimeData.teamSlots.Count; i++)
            {
                TeamCharacterSlotData slotData = playerRuntimeData.teamSlots[i];
                if (slotData == null)
                {
                    allCharactersSpawned = false;
                    Debug.LogError($"[PlayerTeamController] 队伍槽位 {i} 为空，无法生成对应角色。", this);
                    continue;
                }

                CharacterContext context = SpawnSingleCharacter(slotData);
                if (context == null)
                {
                    allCharactersSpawned = false;
                    continue;
                }

                // 4.写入与存档槽位相同的数组索引
                teamCharacters[i] = context;
            }

            return allCharactersSpawned;
        }

        // 判断当前是否可以生成队伍角色
        private bool CanSpawnTeamCharacters()
        {
            // 1.队伍控制器尚未初始化时不允许生成
            if (!IsInitialized)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍角色：组件尚未初始化。", this);
                return false;
            }

            // 2.玩家运行数据为空时无法读取队伍槽位
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍角色：PlayerRuntimeData为空。", this);
                return false;
            }

            // 3.队伍槽位列表为空时无法确定生成内容
            if (playerRuntimeData.teamSlots == null)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍角色：teamSlots为空。", this);
                return false;
            }

            // 4.队伍槽位数量为0时不生成角色
            if (playerRuntimeData.teamSlots.Count == 0)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍角色：teamSlots数量为0。", this);
                return false;
            }

            // 5.角色生成服务为空时无法获取角色预制体
            if (CharacterSpawnService.Instance == null)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍角色：CharacterSpawnService为空。", this);
                return false;
            }

            // 6.角色生成服务尚未初始化时，预制体映射可能不可用
            if (!CharacterSpawnService.Instance.IsInitialized)
            {
                Debug.LogError("[PlayerTeamController] 无法生成队伍角色：CharacterSpawnService尚未初始化。", this);
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

            // 2.根据角色名称生成角色对象
            GameObject character = CharacterSpawnService.Instance.SpawnCharacter(
                slotData.characterName,
                playerRuntimeData.playerPosition,
                Quaternion.Euler(playerRuntimeData.playerEulerAngles),
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
                Debug.LogError($"[PlayerTeamController] 角色 {slotData.characterName} 缺少CharacterContext组件。", character);
                Destroy(character);
                return null;
            }

            // 4.角色显隐由后续当前角色绑定流程统一处理
            return context;
        }
        #endregion

        #region 注入角色运行数据
        // 按队伍槽位为所有已生成角色注入运行数据
        public bool InjectCharacterRuntimeData()
        {
            // 1.注入前进行完整校验
            if (!CanInjectCharacterRuntimeData())
            {
                return false;
            }

            bool allRuntimeDataInjected = true;

            // 2.按相同索引为角色注入对应槽位数据
            for (int i = 0; i < teamCharacters.Length; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null)
                {
                    allRuntimeDataInjected = false;
                    Debug.LogError($"[PlayerTeamController] 注入失败：teamCharacters[{i}]为空。", this);
                    continue;
                }

                CharacterRuntimeData runtimeData = ResolveCharacterRuntimeData(i);
                if (runtimeData == null)
                {
                    allRuntimeDataInjected = false;
                    Debug.LogError($"[PlayerTeamController] 注入失败：槽位 {i} 的CharacterRuntimeData为空。", context);
                    continue;
                }

                // 3.由角色上下文完成通用组件和角色专属模块初始化
                context.Initialize(runtimeData);
            }

            return allRuntimeDataInjected;
        }

        // 判断当前是否可以注入角色运行数据
        private bool CanInjectCharacterRuntimeData()
        {
            // 1.队伍角色数组为空时无法注入
            if (teamCharacters == null)
            {
                Debug.LogError("[PlayerTeamController] 注入失败：teamCharacters为空。", this);
                return false;
            }

            // 2.队伍角色数量为0时无需注入
            if (teamCharacters.Length == 0)
            {
                Debug.LogError("[PlayerTeamController] 注入失败：teamCharacters数量为0。", this);
                return false;
            }

            // 3.玩家运行数据为空时无法读取槽位
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerTeamController] 注入失败：PlayerRuntimeData为空。", this);
                return false;
            }

            // 4.存档队伍槽位为空时无法读取角色运行数据
            if (playerRuntimeData.teamSlots == null)
            {
                Debug.LogError("[PlayerTeamController] 注入失败：teamSlots为空。", this);
                return false;
            }

            // 5.角色数组和存档槽位必须保持相同长度
            if (teamCharacters.Length != playerRuntimeData.teamSlots.Count)
            {
                Debug.LogError(
                    $"[PlayerTeamController] 注入失败：角色数量和槽位数量不一致。" +
                    $"teamCharacters = {teamCharacters.Length}, teamSlots = {playerRuntimeData.teamSlots.Count}",
                    this
                );
                return false;
            }

            return true;
        }

        // 根据队伍槽位索引获取角色运行数据
        private CharacterRuntimeData ResolveCharacterRuntimeData(int characterIndex)
        {
            // 1.索引超出范围时返回空
            if (characterIndex < 0 || characterIndex >= playerRuntimeData.teamSlots.Count)
            {
                Debug.LogError($"[PlayerTeamController] 解析角色运行数据失败：索引非法。characterIndex = {characterIndex}", this);
                return null;
            }

            // 2.读取对应槽位
            TeamCharacterSlotData slotData = playerRuntimeData.teamSlots[characterIndex];
            if (slotData == null)
            {
                Debug.LogError($"[PlayerTeamController] 解析角色运行数据失败：槽位 {characterIndex} 为空。", this);
                return null;
            }

            return slotData.runtimeData;
        }
        #endregion
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    [System.Serializable]
    public class CharacterPrefabMapping
    {
        [Header("角色名称")]
        public CharacterName characterName;
        [Header("角色预制体")]
        public GameObject character;
        
    }
    // 设置脚本执行顺序为-1000，确保该引导脚本优先于所有其他脚本执行
    [DefaultExecutionOrder(-1000)]
    // 游戏启动引导核心类：单例模式，负责全局初始化、服务启动、玩家生成
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }
        [Header("=== 全局服务 (Inspector 注入) ===")]
        [SerializeField] private SaveService saveService; // 存档服务：管理游戏数据的存储与读取
        [SerializeField] private AudioService audioService; // 音频服务：管理游戏音效、背景音乐播放
        [SerializeField] private EffectService effectService; // 特效服务：管理粒子特效、视觉效果
        [SerializeField] private GameTimeService gameTimeService; // 游戏时间服务：管理游戏内时间系统
        [SerializeField] private UIRoot uiRoot; // 游戏ui总节点

        [Header("=== 玩家服务 (Inspector 注入) ===")]
        [Header(" 玩家预制体")]
        [SerializeField] private GameObject playerPrefab; // 玩家预制体
        [Header(" 玩家父节点")]
        [SerializeField] private Transform Target; // 玩家预制体
        [Header(" 玩家控制")]
        [SerializeField] private PlayerController playerController; // 玩家控制
        [Header(" 玩家数据")]
        [SerializeField] private PlayerRuntimeData playerRuntimeData; // 玩家数据
        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;  // 是否输出详细日志

        [Header("默认队伍")]
        [SerializeField] private List<CharacterName> defaultTeamCharacterIds = new();

        [Header(" 角色预制体映射")]
        [SerializeField] private List<CharacterPrefabMapping> characterPrefabMappings = new(); // Inspector 配置的角色预制体映射表

        // 运行时缓存：通过角色名快速获取对应预制体
        private readonly Dictionary<CharacterName, GameObject> characterPrefabMap = new();
        private bool _bootstrapped; // 标记：是否已完成游戏初始化引导，防止重复执行
        private GameObject _spawnedPlayer; // 存储生成后的玩家游戏对象

        #region 外部访问
        public SaveData CurrentSaveData => saveService != null ? saveService.CurrentData : null;  // 外部只读属性：获取当前存档数据（空值安全）
        public GameTimeService TimeService => gameTimeService;  // 外部只读属性：获取时间服务（供其他系统统一使用）
        public PlayerController PlayerController => playerController;  // 外部只读属性：获取生成后的玩家对象
        public GameObject SpawnedPlayer => _spawnedPlayer;  // 外部只读属性：获取生成后的玩家对象
        public PlayerRuntimeData PlayerRuntimeData => playerRuntimeData;  // 外部只读属性：获取生成后的玩家对象
        public Dictionary<CharacterName, GameObject> CharacterPrefabMap => characterPrefabMap;  // 外部只读属性：获取角色字典

        #endregion


        #region 生命周期
        private void Awake()
        {
            // 单例模式核心：如果已存在实例，销毁当前重复对象
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this; // 赋值单例实例
            DontDestroyOnLoad(gameObject); // 场景切换时，不销毁该引导对象
            Bootstrap(); // 执行游戏初始化引导流程
        }
        private void OnDestroy()
        {
            SaveCurrentGame();
        }
        #endregion

        #region  执行游戏初始化引导流程
        // 游戏核心引导方法：统一调度所有初始化逻辑
        public void Bootstrap()
        {
            // 1.已完成初始化，直接返回，避免重复执行
            if (_bootstrapped)
                return;

            // 2.验证Inspector面板引用是否完整，缺失则禁用脚本
            if (!ValidateInspectorReferences())
            {
                Debug.Log("ValidateInspectorReferences验证面板引用是不完整");
                enabled = false;
                return;
            }

            // 3.初始化所有全局服务
            InitializeServices();

            // 4.构建角色预制体映射表
            BuildCharacterPrefabMap();

            // 5.标记初始化完成 开启详细日志 → 输出引导完成提示
            _bootstrapped = true;
            if (verboseLog)
            {
                Debug.Log("[GameBootstrap] 初始化引导流程已完成。");
            }

        }

        #region 验证Inspector面板的所有引用是否赋值完整
        // 验证Inspector面板的所有引用是否赋值完整
        private bool ValidateInspectorReferences()
        {
            bool ok = true; // 初始化验证结果为通过

            // 逐一检查全局服务，缺失则报错并标记失败
            if (saveService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少存档服务(SaveService)引用。", this);
                ok = false;
            }

            if (audioService == null)
            {
                Debug.LogWarning("[GameBootstrap] 当前未配置音频服务(AudioService)，项目将以音频空壳模式运行。", this);
            }

            if (effectService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少特效服务(EffectService)引用。", this);
                ok = false;
            }

            if (gameTimeService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少游戏时间服务(GameTimeService)引用。", this);
                ok = false;
            }

            return ok; // 返回最终验证结果
        }
        // 初始化所有全局游戏服务（空值安全调用）
        private void InitializeServices()
        {
            gameTimeService?.Initialize();
            saveService?.Initialize();
            audioService?.Initialize();
            effectService?.Initialize();
        }
        #endregion

        #region 构建角色预制体 的运行时字典
        // 构建角色名 -> 角色预制体 的运行时字典
        private void BuildCharacterPrefabMap()
        {
            // 1.先清空旧缓存，避免重复引导时残留旧数据
            characterPrefabMap.Clear();

            // 2.空列表直接返回
            if (characterPrefabMappings == null || characterPrefabMappings.Count == 0)
            {
                return;
            }

            // 3.逐个写入映射
            for (int i = 0; i < characterPrefabMappings.Count; i++)
            {
                CharacterPrefabMapping mapping = characterPrefabMappings[i];
                if (mapping == null || mapping.character == null)
                {
                    continue;
                }

                // 同名角色后写覆盖前写，方便你在 Inspector 里调整
                characterPrefabMap[mapping.characterName] = mapping.character;
            }
        }
        #endregion

        #endregion

        #region 进入游戏
        // 根据存档数据正式进入游戏：只处理玩家生成、数据注入和玩家初始化
        private void EnterGameWithSaveData(SaveData saveData)
        {
            // 1.空值检查
            if (saveData == null)
            {
                Debug.LogError("[GameBootstrap] 进入游戏失败：存档数据为空。", this);
                return;
            }

            // 2.确保默认队伍存在，避免旧存档或异常存档没有角色
            EnsureDefaultTeam(saveData);

            // 3.解析玩家控制器和运行时数据，并互相注入
            ResolvePlayer();

            // 4.玩家控制器或玩家运行时数据为空时，无法进入游戏
            if (playerController == null || playerRuntimeData == null)
            {
                Debug.LogError("[GameBootstrap] 进入游戏失败：缺少PlayerController或PlayerRuntimeData。", this);
                return;
            }

            // 5.记录当前常驻玩家对象，避免旧逻辑认为玩家不存在
            _spawnedPlayer = playerController.gameObject;

            // 6.把存档数据写入PlayerRuntimeData
            playerRuntimeData.SyncRuntimeDataFromSaveData(saveData);

            // 7.初始化玩家控制器，生成队伍角色并绑定当前角色
            playerController.Initialize();

            Debug.Log("[GameBootstrap] 已根据存档数据进入游戏。");
        }
        #endregion

        #region 新建存档 读取存档  删除存档  保存存档
        // 新建游戏：由空存档槽位点击新建时调用
        public void StartNewGame(int slotIndex)
        {
            // 1.基础引导未完成时先补齐
            Bootstrap();

            // 2.通过存档服务新建指定槽位存档
            SaveData saveData = saveService != null ? saveService.CreateSave(slotIndex) : null;
            if (saveData == null)
            {
                Debug.LogError($"[GameBootstrap] 新建游戏失败：槽位 {slotIndex + 1} 存档数据为空。", this);
                return;
            }

            // 3.根据新建出来的存档进入游戏
            EnterGameWithSaveData(saveData);
        }

        // 读取游戏：由已有存档槽位点击读取时调用
        public void LoadGame(int slotIndex)
        {
            // 1.基础引导未完成时先补齐
            Bootstrap();

            // 2.通过存档服务读取指定槽位存档
            SaveData saveData = saveService != null ? saveService.LoadSave(slotIndex) : null;
            if (saveData == null)
            {
                Debug.LogError($"[GameBootstrap] 读取游戏失败：槽位 {slotIndex + 1} 存档数据为空。", this);
                return;
            }

            // 3.根据读取出来的存档进入游戏
            EnterGameWithSaveData(saveData);
        }

        // 删除游戏存档：由存档菜单点击删除时调用
        public bool DeleteGameSave(int slotIndex)
        {
            // 1.基础引导未完成时先补齐
            Bootstrap();

            // 2.存档服务为空时无法删除
            if (saveService == null)
            {
                Debug.LogError("[GameBootstrap] 删除存档失败：SaveService为空。", this);
                return false;
            }

            // 3.交给存档服务删除指定槽位
            bool ok = saveService.DeleteSave(slotIndex);

            // 4.输出流程日志
            if (ok)
            {
                Debug.Log($"[GameBootstrap] 槽位 {slotIndex + 1} 存档删除成功。");
            }
            else
            {
                Debug.Log($"[GameBootstrap] 槽位 {slotIndex + 1} 存档删除失败。");
            }

            return ok;
        }

        // 保存当前游戏：由游戏内保存、返回主菜单或退出游戏前调用
        public bool SaveCurrentGame()
        {
            // 1.基础引导未完成时先补齐
            Bootstrap();

            // 2.解析玩家控制器和运行时数据，保证存档前依赖是最新的
            ResolvePlayer();

            // 3.存档服务为空时无法保存
            if (saveService == null)
            {
                Debug.LogError("[GameBootstrap] 保存当前游戏失败：SaveService为空。", this);
                return false;
            }

            // 4.玩家运行时数据为空时无法收集场景数据
            if (playerRuntimeData == null)
            {
                Debug.LogError("[GameBootstrap] 保存当前游戏失败：PlayerRuntimeData为空。", this);
                return false;
            }

            // 5.当前没有正在使用的存档时，不允许保存
            if (saveService.CurrentData == null || saveService.CurrentSlotIndex < 0)
            {
                Debug.LogWarning("[GameBootstrap] 保存当前游戏失败：当前没有正在使用的存档槽。", this);
                return false;
            }

            // 6.先从场景中收集最新运行时数据
            playerRuntimeData.SyncRuntimeDataFromScene();

            // 7.把运行时数据写回当前存档数据
            playerRuntimeData.SyncSaveDataFromRuntimeData(saveService.CurrentData);

            // 8.交给存档服务保存当前槽位
            bool ok = saveService.SaveCurrentSave();
            if (ok)
            {
                Debug.Log("[GameBootstrap] 当前游戏保存成功。");
            }
            else
            {
                Debug.Log("[GameBootstrap] 当前游戏保存失败。");
            }

            return ok;
        }

        #endregion

        #region 数据注入
        // 数据注入
        private void ResolvePlayer()
        {
            // 1.优先使用PlayerController单例，保证拿到的是常驻玩家根节点
            if (playerController == null)
            {
                playerController = PlayerController.Instance;
            }

            // 2.兜底：如果单例还没建立，就从场景中查找
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }

            // 3.优先从PlayerController身上获取PlayerRuntimeData
            if (playerRuntimeData == null && playerController != null)
            {
                playerRuntimeData = playerController.PlayerRuntimeData;
            }

            // 4.兜底：如果PlayerController里还没绑定，就从场景中查找
            if (playerRuntimeData == null)
            {
                playerRuntimeData = FindObjectOfType<PlayerRuntimeData>();
            }

            // 5.互相注入，保证两边引用一致
            if (playerRuntimeData != null && playerController != null)
            {
                playerRuntimeData.Injected(playerController);
                playerController.Injected(playerRuntimeData);
            }
        }


        #endregion

        #region 角色生成

        // 根据角色名称生成角色
        public GameObject SpawnCharacter(CharacterName characterName, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!TryGetCharacterPrefab(characterName, out GameObject prefab) || prefab == null)
            {
                Debug.LogError($"[GameBootstrap] 未找到角色 {characterName} 对应的预制体。", this);
                return null;
            }

            return Instantiate(prefab, position, rotation, parent);
        }
        // 根据角色名称获取对应预制体
        public bool TryGetCharacterPrefab(CharacterName characterName, out GameObject prefab)
        {
            return characterPrefabMap.TryGetValue(characterName, out prefab);
        }
        public void EnsureDefaultTeam(SaveData saveData)
        {
            // 1.空值检查
            if (saveData == null)
            {
                return;
            }

            // 2.已有队伍则不覆盖，避免读档时把玩家队伍重置
            if (saveData.teamSlots != null && saveData.teamSlots.Count > 0)
            {
                return;
            }

            // 3.根据默认队伍配置创建槽位
            for (int i = 0; i < defaultTeamCharacterIds.Count; i++)
            {
                CharacterName characterName = defaultTeamCharacterIds[i];

                saveData.teamSlots.Add(new TeamCharacterSlotData
                {
                    characterName = characterName,
                    runtimeData = CreateDefaultCharacterRuntimeData(characterName)
                });
            }

            // 5.默认当前操控第一个角色
            saveData.currentCharacterIndex = 0;
        }

        // 创建默认角色运行时数据：从角色预制体上的CharacterDataSO读取初始生命值，避免默认存档生成0血角色
        private CharacterRuntimeData CreateDefaultCharacterRuntimeData(CharacterName characterName)
        {
            CharacterRuntimeData runtimeData = new CharacterRuntimeData
            {
                characterName = characterName
            };

            CharacterDataSO characterDataSO = ResolveCharacterDataSO(characterName);
            if (characterDataSO != null)
            {
                runtimeData.Initialize(characterDataSO);
            }

            return runtimeData;
        }

        // 根据角色名称解析角色基础数据
        private CharacterDataSO ResolveCharacterDataSO(CharacterName characterName)
        {
            GameObject prefab = null;
            TryGetCharacterPrefab(characterName, out prefab);

            if (prefab == null && characterPrefabMappings != null)
            {
                for (int i = 0; i < characterPrefabMappings.Count; i++)
                {
                    CharacterPrefabMapping mapping = characterPrefabMappings[i];
                    if (mapping == null || mapping.characterName != characterName)
                    {
                        continue;
                    }

                    prefab = mapping.character;
                    break;
                }
            }

            CharacterContext context = prefab != null ? prefab.GetComponent<CharacterContext>() : null;
            return context != null ? context.CharacterDataSO : null;
        }

        #endregion

        
    }
}

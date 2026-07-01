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
        [Header(" 游戏时间服务")]
        [SerializeField] private GameTimeService gameTimeService; // 游戏时间服务：管理暂停、鼠标状态与游戏时间缩放
        [Header(" 账号服务")]
        [SerializeField] private AccountManager accountManager; // 账号服务：管理本地账号注册、登录与当前账号状态
        [Header(" 存档服务")]
        [SerializeField] private SaveService saveService; // 存档服务：管理当前账号存档的创建、读取与保存
        [Header(" 背包服务")]
        [SerializeField] private InventoryService inventoryService; // 背包服务：管理当前账号存档中的背包数据
        [Header(" 音频服务")]
        [SerializeField] private AudioService audioService; // 音频服务：管理游戏音效、背景音乐播放与音量设置
        [Header(" 特效服务")]
        [SerializeField] private EffectService effectService; // 特效服务：管理粒子特效、技能效果与视觉表现
        [Header(" UI根节点")]
        [SerializeField] private UIRoot uiRoot; // 游戏UI总节点：管理主菜单、存档菜单、HUD和小地图
        [Header("=== 玩家服务 (Inspector 注入) ===")]
        [Header(" 玩家控制")]
        [SerializeField] private PlayerController playerController; // 玩家控制器：负责队伍角色生成、切换与当前角色绑定
        [Header(" 玩家数据")]
        [SerializeField] private PlayerRuntimeData playerRuntimeData; // 玩家运行时数据：负责玩家位置、队伍与角色运行数据同步
        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;  // 是否输出详细日志
        [Header("默认队伍")]
        [SerializeField] private List<CharacterName> defaultTeamCharacterIds = new();
        [Header(" 角色预制体映射")]
        [SerializeField] private List<CharacterPrefabMapping> characterPrefabMappings = new(); // Inspector 配置的角色预制体映射表
        // 运行时缓存：通过角色名快速获取对应预制体
        private readonly Dictionary<CharacterName, GameObject> characterPrefabMap = new();

        private bool _bootstrapped; // 标记：是否已完成游戏初始化引导，防止重复执行

        #region 外部访问
        public GameTimeService TimeService => gameTimeService;  // 外部只读属性：获取时间服务
        public AccountManager AccountManager => accountManager;  // 外部只读属性：获取账号服务
        public SaveService SaveService => saveService;  // 外部只读属性：获取当前存档数据（空值安全）
        public InventoryService InventoryService => inventoryService;  // 外部只读属性：获取背包服务
        public PlayerController PlayerController => playerController;  // 外部只读属性：获取玩家控制器
        public PlayerRuntimeData PlayerRuntimeData => playerRuntimeData;  // 外部只读属性：获取玩家运行时数据
        public Dictionary<CharacterName, GameObject> CharacterPrefabMap => characterPrefabMap;  // 外部只读属性：获取角色预制体字典
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
        }

        private void Start()
        {
            // 等其他全局服务完成Awake后，再统一执行初始化流程
            Bootstrap();
        }
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        private void OnApplicationQuit()
        {
            SaveCurrentGame();
        }
        #endregion

        #region  游戏启动
        // 游戏必要核心启动
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
        }

        #region 验证Inspector面板的所有引用是否赋值完整
        // 验证Inspector面板的所有引用是否赋值完整
        private bool ValidateInspectorReferences()
        {
            bool ok = true; // 初始化验证结果为通过

            // 1.检查时间服务：时间服务是UI暂停、鼠标状态和时间缩放的前置基础
            if (gameTimeService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少游戏时间服务(GameTimeService)引用。", this);
                ok = false;
            }

            // 2.检查账号服务：账号服务是账号存档流程的入口
            if (accountManager == null)
            {
                Debug.LogError("[GameBootstrap] 缺少账号服务(AccountManager)引用。", this);
                ok = false;
            }

            // 3.检查存档服务：存档服务负责当前账号存档的创建、读取和保存
            if (saveService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少存档服务(SaveService)引用。", this);
                ok = false;
            }

            // 4.检查背包服务：背包服务依赖账号存档中的InventoryData
            if (inventoryService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少背包服务(InventoryService)引用。", this);
                ok = false;
            }

            // 5.检查音频服务：音频缺失时项目仍可运行，因此只输出警告
            if (audioService == null)
            {
                Debug.LogWarning("[GameBootstrap] 当前未配置音频服务(AudioService)，项目将以音频空壳模式运行。", this);
            }

            // 6.检查特效服务：特效服务是角色技能和战斗表现的必要依赖
            if (effectService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少特效服务(EffectService)引用。", this);
                ok = false;
            }

            // 7.检查UI根节点：UI根节点依赖前置服务完成初始化后再启动
            if (uiRoot == null)
            {
                Debug.LogError("[GameBootstrap] 缺少游戏UI根节点(UIRoot)引用。", this);
                ok = false;
            }

            // 8.检查玩家控制器：进入游戏时需要通过它生成队伍角色并绑定当前角色
            if (playerController == null)
            {
                Debug.LogError("[GameBootstrap] 缺少玩家节点(playerController)引用。", this);
                ok = false;
            }

            // 9.检查玩家运行时数据：存档读写和角色生成都依赖该运行时数据
            if (playerRuntimeData == null)
            {
                Debug.LogError("[GameBootstrap] 缺少运行数据节点(playerRuntimeData)引用。", this);
                ok = false;
            }

            return ok; // 返回最终验证结果
        }
        #endregion

        #region 初始化所有全局游戏服务
        // 初始化所有全局游戏服务：按系统依赖关系链式启动，禁止随意调整顺序
        private void InitializeServices()
        {
            // 1.初始化时间服务：为后续UI暂停、鼠标状态和时间缩放提供基础能力
            gameTimeService?.Initialize();

            // 2.初始化账号服务：为后续账号存档流程提供当前账号状态
            accountManager?.Initialize();

            // 3.初始化存档服务：创建设置仓储，并等待账号登录后绑定存档数据
            saveService?.Initialize();

            // 4.初始化背包服务：先启动服务本体，等账号存档加载后再绑定InventoryData
            inventoryService?.Initialize();

            // 5.初始化音频服务：音量设置依赖SaveService中的设置仓储
            audioService?.Initialize();

            // 6.初始化特效服务：为角色与战斗表现提供特效播放能力
            effectService?.Initialize();

            // 7.初始化UI根节点：UI初始化会访问时间、音频、存档等前置服务，因此最后启动
            uiRoot?.Initialize();
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
        private void EnterGameWithCurrentAccountSave(SaveData saveData)
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

            // 4.把存档数据写入PlayerRuntimeData
            playerRuntimeData.SyncRuntimeDataFromSaveData(saveData);

            // 5.初始化玩家控制器，生成队伍角色并绑定当前角色
            playerController.Initialize();

            // 6.进入游戏后绑定UI并显示玩法界面
            uiRoot?.ShowGameplayUI();

            if (verboseLog)
            Debug.Log("[GameBootstrap] 已根据存档数据进入游戏。");
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
                playerRuntimeData.Bind(playerController);
                playerController.Bind(playerRuntimeData);
            }
        }


        #endregion

        #region 新建存档  保存存档 保存至指定槽位 读取存档  删除存档 
        // 获取当前登录账号名：账号未登录时返回空字符串，避免存档系统误操作
        private string GetCurrentUsername()
        {
            // 1.账号管理器为空时，无法获取当前账号
            if (accountManager == null)
            {
                Debug.LogError("[GameBootstrap] 获取当前账号失败：AccountManager为空。", this);
                return string.Empty;
            }

            // 2.当前未登录账号时，不允许进行账号存档操作
            if (!accountManager.IsLoggedIn || accountManager.CurrentAccount == null)
            {
                Debug.LogWarning("[GameBootstrap] 当前没有登录账号，无法进行存档操作。", this);
                return string.Empty;
            }

            // 3.返回当前登录账号名，用于SaveService定位账号存档目录
            return accountManager.CurrentAccount.username;
        }
        // 新建游戏：由空存档槽位点击新建时调用
        public void StartNewGame(int slotIndex)
        {
            // 1.基础引导未完成时先补齐
            Bootstrap();

            // 2.通过存档服务新建指定槽位存档
            string username = GetCurrentUsername();

            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            SaveData saveData = saveService != null ? saveService.CreateSave(username) : null;
            if (saveData == null)
            {
                Debug.LogError($"[GameBootstrap] 新建游戏失败：槽位 {slotIndex + 1} 存档数据为空。", this);
                return;
            }

            // 3.根据新建出来的存档进入游戏
            EnterGameWithCurrentAccountSave(saveData);
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

            // 5.当前没有正在使用的账号存档时，不允许保存
            if (saveService.CurrentData == null || string.IsNullOrWhiteSpace(saveService.CurrentUsername))
            {
                Debug.LogWarning("[GameBootstrap] 保存当前游戏失败：当前没有正在使用的账号存档。", this);
                return false;
            }

            // 6.先从场景中收集最新运行时数据
            playerRuntimeData.SyncRuntimeDataFromScene();

            // 7.把运行时数据写回当前存档数据
            playerRuntimeData.SyncSaveDataFromRuntimeData(saveService.CurrentData);

            // 8.交给存档服务保存当前账号存档
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

        // 保存当前游戏到当前账号存档
        public bool SaveCurrentGameToSlot(int slotIndex)
        {
            // 1.基础引导未完成时先补齐
            Bootstrap();

            // 2.解析玩家控制器和运行时数据，保证存档前依赖是最新的
            ResolvePlayer();

            // 3.存档服务为空时无法保存
            if (saveService == null)
            {
                Debug.LogError("[GameBootstrap] 保存游戏失败：SaveService为空。", this);
                return false;
            }

            // 4.玩家运行时数据为空时无法收集场景数据
            if (playerRuntimeData == null)
            {
                Debug.LogError("[GameBootstrap] 保存游戏失败：PlayerRuntimeData为空。", this);
                return false;
            }

            // 5.当前没有进入游戏时，不允许主动存档
            if (playerController == null || playerController.CurrentCharacterContext == null)
            {
                Debug.LogWarning("[GameBootstrap] 保存游戏失败：当前还没有可保存的玩家角色。", this);
                return false;
            }

            // 6.当前没有正在使用的账号存档时，不允许主动保存
            if (saveService.CurrentData == null || string.IsNullOrWhiteSpace(saveService.CurrentUsername))
            {
                Debug.LogWarning("[GameBootstrap] 保存游戏失败：当前没有正在使用的账号存档。", this);
                return false;
            }

            // 7.先从场景中收集最新运行时数据
            playerRuntimeData.SyncRuntimeDataFromScene();

            // 8.把运行时数据写回当前账号存档数据
            playerRuntimeData.SyncSaveDataFromRuntimeData(saveService.CurrentData);

            // 9.交给存档服务保存当前账号存档
            bool ok = saveService.SaveCurrentSave();

            if (ok)
            {
                Debug.Log("[GameBootstrap] 当前游戏已保存到当前账号存档。");
            }
            else
            {
                Debug.Log("[GameBootstrap] 当前游戏保存到当前账号存档失败。");
            }

            return ok;
        }

        // 读取游戏：由当前登录账号读取账号存档
        public void LoadGame(int slotIndex)
        {
            // 1.基础引导未完成时先补齐
            Bootstrap();

            // 2.获取当前登录账号名，账号为空时不允许读档
            string username = GetCurrentUsername();
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            // 3.通过存档服务读取当前账号存档
            SaveData saveData = saveService != null ? saveService.LoadSave(username) : null;
            if (saveData == null)
            {
                Debug.LogError($"[GameBootstrap] 读取游戏失败：账号 '{username}' 存档数据为空。", this);
                return;
            }

            // 4.根据读取出来的存档进入游戏
            EnterGameWithCurrentAccountSave(saveData);
        }

        // 删除游戏存档：删除当前登录账号的账号存档
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

            // 3.获取当前登录账号名，账号为空时不允许删除存档
            string username = GetCurrentUsername();
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            // 4.交给存档服务删除当前账号存档
            bool ok = saveService.DeleteSave(username);

            // 5.输出流程日志
            if (ok)
            {
                Debug.Log($"[GameBootstrap] 账号 '{username}' 存档删除成功。");
            }
            else
            {
                Debug.Log($"[GameBootstrap] 账号 '{username}' 存档删除失败。");
            }

            return ok;
        }

        #endregion

        #region 角色生成（外部调用）

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

using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 设置脚本执行顺序为-1000，确保该引导脚本优先于所有其他脚本执行
    [DefaultExecutionOrder(-1000)]
    // 游戏启动引导核心类：单例模式，负责全局初始化、服务启动、玩家生成
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        #region 全局服务
        [Header("=== 全局服务 (Inspector 注入) ===")]
        [Header(" 时间服务")]
        [SerializeField] private GameTimeService gameTimeService; // 游戏时间服务：管理暂停、鼠标状态与游戏时间缩放
        [Header(" 输入服务")]
        [SerializeField] private InputService inputService; // 输入服务：统一管理UI输入状态和玩家输入开关
        [Header(" 账号服务")]
        [SerializeField] private AccountManager accountManager; // 账号服务：管理本地账号注册、登录与当前账号状态
        [Header(" 存档服务")]
        [SerializeField] private SaveService saveService; // 存档服务：管理当前账号存档的创建、读取与保存
        [Header(" 背包服务")]
        [SerializeField] private InventoryService inventoryService; // 背包服务：管理当前账号存档中的背包数据
        [Header(" 角色生成服务")]
        [SerializeField] private CharacterSpawnService characterSpawnService; // 角色生成服务：管理角色预制体映射、角色实例化与生成入口
        [Header(" 音频服务")]
        [SerializeField] private AudioService audioService; // 音频服务：管理游戏音效、背景音乐播放与音量设置
        [Header(" 特效服务")]
        [SerializeField] private EffectService effectService; // 特效服务：管理粒子特效、技能效果与视觉表现
        [Header(" 游戏会话服务")]
        [SerializeField] private GameSessionService gameSessionService; // 游戏会话服务：管理当前账号开始游戏、进入游戏和退出自动保存
        [Header(" UI总节点")]
        [SerializeField] private UIRoot uiRoot; // 游戏UI总节点：管理主菜单、存档菜单、HUD和小地图
        #endregion

        #region 玩家服务
        [Header("=== 玩家服务 (Inspector 注入) ===")]
        [Header(" 玩家控制")]
        [SerializeField] private PlayerController playerController; // 玩家控制器：负责队伍角色生成、切换与当前角色绑定
        [Header(" 玩家数据")]
        [SerializeField] private PlayerRuntimeData playerRuntimeData; // 玩家运行时数据：负责玩家位置、队伍与角色运行数据同步
        #endregion

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;  // 是否输出详细日志

        private bool _bootstrapped; // 标记：是否已完成游戏初始化引导，防止重复执行

        #region 外部访问
        public GameTimeService TimeService => gameTimeService;  // 外部只读属性：获取时间服务
        public InputService InputService => inputService;  // 外部只读属性：获取输入服务
        public AccountManager AccountManager => accountManager;  // 外部只读属性：获取账号服务
        public SaveService SaveService => saveService;  // 外部只读属性：获取存档服务
        public InventoryService InventoryService => inventoryService;  // 外部只读属性：获取背包服务
        public CharacterSpawnService CharacterSpawnService => characterSpawnService;  // 外部只读属性：获取角色生成服务
        public AudioService AudioService => audioService;  // 外部只读属性：获取音频服务
        public EffectService EffectService => effectService;  // 外部只读属性：获取特效服务
        public GameSessionService GameSessionService => gameSessionService;  // 外部只读属性：获取游戏会话服务
        public UIRoot UIRoot => uiRoot;  // 外部只读属性：获取UI根节点

        public PlayerController PlayerController => playerController;  // 外部只读属性：获取玩家控制器
        public PlayerRuntimeData PlayerRuntimeData => playerRuntimeData;  // 外部只读属性：获取玩家运行时数据
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

            // 统一执行初始化流程
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
            // 1.退出程序时交给游戏会话服务自动保存当前账号存档
            gameSessionService?.SaveCurrentGameOnExit();
        }
        #endregion

        #region  游戏启动
        public void Bootstrap()
        {
            // 1.已完成初始化，直接返回，避免重复执行
            if (_bootstrapped)
            {
                return;
            }

            if (verboseLog)
            {
                Debug.Log("[GameBootstrap] 开始初始化全局服务。", this);
            }

            // 2.验证Inspector面板引用是否完整，缺失则禁用脚本
            if (!ValidateInspectorReferences())
            {
                Debug.Log("ValidateInspectorReferences验证面板引用是不完整");
                enabled = false;
                return;
            }

            // 3.初始化所有全局服务
            InitializeServices();

            // 4.标记初始化完成 开启详细日志 → 输出引导完成提示
            _bootstrapped = true;

            if (verboseLog)
            {
                Debug.Log("[GameBootstrap] 全局服务初始化完成。", this);
            }
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

            // 2.检查输入服务：登录、注册、菜单和玩家输入开关都依赖输入服务
            if (inputService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少输入服务(InputService)引用。", this);
                ok = false;
            }

            // 3.检查账号服务：账号服务是登录注册和账号存档流程的入口
            if (accountManager == null)
            {
                Debug.LogError("[GameBootstrap] 缺少账号服务(AccountManager)引用。", this);
                ok = false;
            }

            // 4.检查存档服务：存档服务负责当前账号存档的创建、读取和保存
            if (saveService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少存档服务(SaveService)引用。", this);
                ok = false;
            }

            // 5.检查背包服务：背包服务依赖账号存档中的InventoryData
            if (inventoryService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少背包服务(InventoryService)引用。", this);
                ok = false;
            }

            // 6.检查角色生成服务：角色生成服务负责角色预制体映射和角色实例化
            if (characterSpawnService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少角色生成服务(CharacterSpawnService)引用。", this);
                ok = false;
            }

            // 7.检查音频服务：音频缺失时项目仍可运行，因此只输出警告
            if (audioService == null)
            {
                Debug.LogWarning("[GameBootstrap] 当前未配置音频服务(AudioService)，项目将以音频空壳模式运行。", this);
            }

            // 8.检查特效服务：特效服务是角色技能和战斗表现的必要依赖
            if (effectService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少特效服务(EffectService)引用。", this);
                ok = false;
            }

            // 9.检查游戏会话服务：开始游戏、进入游戏和退出自动保存都交给它处理
            if (gameSessionService == null)
            {
                Debug.LogError("[GameBootstrap] 缺少游戏会话服务(GameSessionService)引用。", this);
                ok = false;
            }

            // 10.检查UI根节点：UI根节点依赖前置服务完成初始化后再启动
            if (uiRoot == null)
            {
                Debug.LogError("[GameBootstrap] 缺少游戏UI根节点(UIRoot)引用。", this);
                ok = false;
            }

            // 11.检查玩家控制器：玩家控制器依赖前置服务完成初始化后再启动
            if (playerController == null)
            {
                Debug.LogError("[GameBootstrap] 缺少玩家控制器(PlayerController)引用。", this);
                ok = false;
            }

            // 12检查玩家运行时数据：玩家运行时数据依赖前置服务完成初始化后再启动
            if (playerRuntimeData == null)
            {
                Debug.LogError("[GameBootstrap] 缺少游戏UI根节点(PlayerRuntimeData)引用。", this);
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

            // 2.初始化输入服务：登录、注册、菜单和玩家输入开关都依赖它
            inputService?.Initialize();

            // 3.初始化账号服务：为后续登录注册和账号存档流程提供当前账号状态
            accountManager?.Initialize();

            // 4.初始化存档服务：创建设置仓储，并等待账号登录后绑定存档数据
            saveService?.Initialize();

            // 5.初始化背包服务：先启动服务本体，等账号存档加载后再绑定InventoryData
            inventoryService?.Initialize();

            // 6.初始化角色生成服务：为后续玩家队伍生成提供统一角色生成入口
            characterSpawnService?.Initialize();

            // 7.初始化音频服务：音量设置依赖SaveService中的设置仓储
            audioService?.Initialize();

            // 8.初始化特效服务：为角色与战斗表现提供特效播放能力
            effectService?.Initialize();

            // 9.初始化游戏会话服务：负责当前账号游戏流程
            gameSessionService?.Initialize();

            // 10.初始化UI根节点：UI初始化会访问时间、输入、音频、存档等前置服务，因此最后启动
            uiRoot?.Initialize();
        }
        #endregion

        #endregion

    }
}

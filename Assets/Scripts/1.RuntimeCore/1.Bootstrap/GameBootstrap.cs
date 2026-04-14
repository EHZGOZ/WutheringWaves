using UnityEngine;

namespace WutheringWaves
{
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

        [Header("=== 玩家生成 ===")]
        [SerializeField] private PlayerSpawnConfigSO playerSpawnConfig; // 玩家生成配置文件（ScriptableObject）
        [SerializeField] private Transform playerParent; // 玩家生成后的父物体，用于场景层级管理
        [Header("是否跨场景保留玩家对象")]
        [SerializeField] private bool keepSpawnedPlayerAcrossScenes = false; // 是否跨场景保留玩家对象

        [Header("=== 启动选项 ===")]
        [Header("游戏启动时是否自动加载存档")]
        [SerializeField] private bool loadSaveOnStart = true; // 游戏启动时是否自动加载存档
        [Header("游戏启动时是否自动生成玩家")]
        [SerializeField] private bool spawnPlayerOnStart = true; // 游戏启动时是否自动生成玩家
        [Header("是否输出详细调试日志")]
        [SerializeField] private bool verboseLog = true; // 是否输出详细调试日志

        private bool _bootstrapped; // 标记：是否已完成游戏初始化引导，防止重复执行

        private GameObject _spawnedPlayer; // 存储生成后的玩家游戏对象
        public SaveData CurrentSaveData => saveService != null ? saveService.CurrentData : null;  // 外部只读属性：获取当前存档数据（空值安全）
        public GameObject SpawnedPlayer => _spawnedPlayer;  // 外部只读属性：获取生成后的玩家对象
        public GameTimeService TimeService => gameTimeService;  // 外部只读属性：获取时间服务（供其他系统统一使用）


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

        // Unity生命周期：Start在Awake之后执行，启动核心初始化
        private void Start()
        {
            Bootstrap(); // 执行游戏初始化引导流程
        }
        private void OnDisable()
        {
            //存档(逻辑没写好暂时不存档)
            //SaveCurrentPlayerTransform();
        }
        private void OnApplicationQuit()
        {
            //存档(逻辑没写好暂时不存档)
            //SaveCurrentPlayerTransform();
        }
        #endregion

        #region 统一调度所有初始化逻辑

        #endregion
        // 游戏核心引导方法：统一调度所有初始化逻辑
        public void Bootstrap()
        {
            // 1.已完成初始化，直接返回，避免重复执行
            if (_bootstrapped)
            {
                return;
            }

            // 2.验证Inspector面板引用是否完整，缺失则禁用脚本
            if (!ValidateInspectorReferences())
            {
                enabled = false;
                return;
            }

            // 3.初始化所有全局服务
            InitializeServices(); 

            // 4.开启自动加载存档 且 存档服务存在 → 加载/创建存档
            if (loadSaveOnStart && saveService != null)
            {
                saveService.LoadOrCreate();
            }

            // 5.开启自动生成玩家 → 执行玩家生成逻辑
            if (spawnPlayerOnStart)
            {
                SpawnPlayerFromConfig();
            }

            _bootstrapped = true; // 标记初始化完成
            // 开启详细日志 → 输出引导完成提示
            if (verboseLog)
            {
                Debug.Log("[GameBootstrap] 初始化引导流程已完成。");
            }
        }

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
                Debug.LogError("[GameBootstrap] 缺少音频服务(AudioService)引用。", this);
                ok = false;
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

            // 开启自动生成玩家但缺失配置 → 报错并标记失败
            if (spawnPlayerOnStart && playerSpawnConfig == null)
            {
                Debug.LogError("[GameBootstrap] 已启用自动生成玩家，但缺少PlayerSpawnConfigSO配置文件引用。", this);
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
        // 根据配置文件生成玩家对象
        public GameObject SpawnPlayerFromConfig()
        {
            // 缺失玩家配置文件 → 报错并返回空
            if (playerSpawnConfig == null)
            {
                Debug.LogError("[GameBootstrap] 缺少PlayerSpawnConfigSO配置文件引用。", this);
                return null;
            }

            // 配置文件中缺失玩家预制体 → 报错并返回空
            if (playerSpawnConfig.playerPrefab == null)
            {
                Debug.LogError("[GameBootstrap] PlayerSpawnConfigSO配置文件中缺少玩家预制体引用。", this);
                return null;
            }

            // 玩家已生成 → 直接返回现有玩家对象
            if (_spawnedPlayer != null)
            {
                return _spawnedPlayer;
            }

            // 获取配置文件中的默认生成位置和旋转
            Vector3 spawnPosition = playerSpawnConfig.defaultSpawnPosition;
            Quaternion spawnRotation = Quaternion.Euler(playerSpawnConfig.defaultSpawnEuler);

            // 获取存档数据，若启用存档位置 → 替换为存档中的玩家坐标
            SaveData data = CurrentSaveData;
            if (data != null && data.hasPlayerTransform && playerSpawnConfig.useSavedTransformWhenAvailable)
            {
                spawnPosition = data.playerPosition;
                spawnRotation = Quaternion.Euler(data.playerEulerAngles);
            }

            // 实例化玩家预制体到指定位置、旋转和父物体下
            _spawnedPlayer = Instantiate(playerSpawnConfig.playerPrefab, spawnPosition, spawnRotation, playerParent);
            // 配置中指定了玩家名称 → 重命名玩家对象
            if (!string.IsNullOrWhiteSpace(playerSpawnConfig.spawnedPlayerName))
            {
                _spawnedPlayer.name = playerSpawnConfig.spawnedPlayerName;
            }

            // 开启跨场景保留 → 玩家对象不随场景切换销毁
            if (keepSpawnedPlayerAcrossScenes)
            {
                DontDestroyOnLoad(_spawnedPlayer);
            }

            // 开启详细日志 → 输出玩家生成信息
            if (verboseLog)
            {
                Debug.Log($"[GameBootstrap] 玩家生成成功：{_spawnedPlayer.name}，生成位置：{spawnPosition}");
            }

            return _spawnedPlayer; // 返回生成的玩家对象
        }

        // 保存当前玩家的位置和旋转信息到存档
        public void SaveCurrentPlayerTransform()
        {
            // 存档服务/玩家对象为空 → 直接返回
            if (saveService == null || _spawnedPlayer == null)
            {
                return;
            }

            // 获取当前存档，无存档则创建默认数据
            SaveData data = saveService.CurrentData ?? SaveData.CreateDefault();
            data.hasPlayerTransform = true; // 标记已存储玩家坐标
            data.playerPosition = _spawnedPlayer.transform.position; // 保存玩家位置
            data.playerEulerAngles = _spawnedPlayer.transform.eulerAngles; // 保存玩家旋转
            saveService.Save(data); // 执行存档写入
        }

        

        
    }
}

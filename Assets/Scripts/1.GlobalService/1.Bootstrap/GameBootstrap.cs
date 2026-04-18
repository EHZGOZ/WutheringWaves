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
        [SerializeField] private PlayerRuntimeData playerRuntimeData = new PlayerRuntimeData(); // 玩家数据
        
        [Header(" 角色标识列表")]
        public List<CharacterName> teamCharacterIds = new(); // 队伍角色标识列表
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
            // 5.玩家生成
            SpawnCharacter(Target);
            // 6.数据注入
            ResolvePlayer();

            // 7.玩家控制初始化
            PlayerController.Initialize();

            // 8.标记初始化完成 开启详细日志 → 输出引导完成提示
            _bootstrapped = true; 
            Debug.Log("[GameBootstrap] 初始化引导流程已完成。");
        }
        #endregion

        #region 初始化所有全局服务
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

        #region 玩家生成
        public void SpawnCharacter(Transform parent = null)
        {
            if (playerPrefab == null)
            {
                Debug.LogError($"[GameBootstrap] 未找到playerPrefab对应的预制体。", this);
                return;
            }

            _spawnedPlayer=Instantiate(playerPrefab, Vector3.zero, Quaternion.identity, parent);
        }
        #endregion

        #region 数据注入
        // 数据注入
        private void ResolvePlayer()
        {
            if (playerRuntimeData == null)
            {
                playerRuntimeData = FindObjectOfType<PlayerRuntimeData>();
            }
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }
            playerRuntimeData.Injected(PlayerController);
            
            playerController.Injected(PlayerRuntimeData);
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
        #endregion





    }
}

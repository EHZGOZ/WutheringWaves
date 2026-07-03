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

    // 角色生成服务：负责角色预制体映射、角色实例化与生成入口管理
    public class CharacterSpawnService : MonoBehaviour
    {
        public static CharacterSpawnService Instance { get; private set; }

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;

        [Header(" 角色预制体映射")]
        [SerializeField] private List<CharacterPrefabMapping> characterPrefabMappings = new(); // Inspector配置的角色预制体映射表

        public bool IsInitialized { get; private set; }

        // 运行时缓存：通过角色名快速获取对应预制体
        private readonly Dictionary<CharacterName, GameObject> characterPrefabMap = new();

        #region 外部访问
        public Dictionary<CharacterName, GameObject> CharacterPrefabMap => characterPrefabMap;  // 外部只读属性：获取角色预制体字典
        #endregion

        #region 生命周期
        private void Awake()
        {
            // 1.保持单例，避免多个CharacterSpawnService争抢角色生成管理权
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 2.缓存单例引用
            Instance = this;
        }

        private void OnDestroy()
        {
            // 1.如果销毁的是当前单例，清空单例引用
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region 初始化
        // 初始化角色生成服务
        public void Initialize()
        {
            // 1.已经初始化过时直接返回，避免重复初始化
            if (IsInitialized)
            {
                return;
            }

            // 2.构建角色预制体映射表，保证后续可以按角色名生成角色
            BuildCharacterPrefabMap();

            // 3.标记初始化完成
            IsInitialized = true;

            // 4.打印初始化日志，方便确认启动链是否执行到角色生成服务
            if (verboseLog)
            {
                Debug.Log("[角色生成服务] 初始化完成。");
            }
        }
        #endregion

        #region 构建角色预制体的运行时字典
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

                // 4.同名角色后写覆盖前写，方便在Inspector里调整
                characterPrefabMap[mapping.characterName] = mapping.character;
            }
        }
        #endregion

        #region 角色生成
        // 根据角色名称生成角色
        public GameObject SpawnCharacter(CharacterName characterName, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // 1.找不到角色预制体时，停止生成并输出错误
            if (!TryGetCharacterPrefab(characterName, out GameObject prefab) || prefab == null)
            {
                Debug.LogError($"[角色生成服务] 未找到角色 {characterName} 对应的预制体。", this);
                return null;
            }

            // 2.实例化角色对象，并挂到指定父节点下
            return Instantiate(prefab, position, rotation, parent);
        }

        // 根据角色名称获取对应预制体
        public bool TryGetCharacterPrefab(CharacterName characterName, out GameObject prefab)
        {
            // 1.从运行时字典中查找角色预制体
            return characterPrefabMap.TryGetValue(characterName, out prefab);
        }
        #endregion
    }
}
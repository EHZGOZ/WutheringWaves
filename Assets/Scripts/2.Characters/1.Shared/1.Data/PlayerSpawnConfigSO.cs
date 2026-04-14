using UnityEngine;

namespace WutheringWaves
{
    // 玩家生成配置：集中管理启动时玩家预制体和出生点信息
    [CreateAssetMenu(menuName = "WutheringWaves/Bootstrap/Player Spawn Config", fileName = "PlayerSpawnConfigSO", order = 0)]
    public class PlayerSpawnConfigSO : ScriptableObject
    {
        [Header("=== Prefab ===")]
        public GameObject playerPrefab; // 玩家预制体
        public string spawnedPlayerName = "Player"; // 生成后的玩家对象名称

        [Header("=== Default Spawn Transform ===")]
        public Vector3 defaultSpawnPosition = Vector3.zero; // 默认出生位置
        public Vector3 defaultSpawnEuler = Vector3.zero; // 默认出生朝向（欧拉角）

        [Header("=== Save Integration ===")]
        public bool useSavedTransformWhenAvailable = true; // 若存在存档位姿则优先使用存档数据
    }
}

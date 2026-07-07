using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WutheringWaves
{
    [System.Serializable]
    // 单条攻击特效配置：按 AttackId 映射一组需要播放的特效
    public class AttackEffectActionConfig
    {
        [Header("攻击步骤ID")]
        public AttackId attackId = AttackId.None;

        [Header("特效播放列表")]
        public List<EffectSpawnConfig> effectSpawns = new List<EffectSpawnConfig>();

        public override string ToString()
        {
            return $"攻击特效动作：{attackId}";
        }
    }

    [System.Serializable]
    // 单个特效生成配置：包含预制体、延迟、偏移与跟随方式
    public class EffectSpawnConfig
    {
        [Header("特效预制体")]
        public ParticleSystem effectPrefab;

        [Header("延迟播放时间")]
        public float delayTime = 0f;

        [Header("是否跟随角色")]
        public bool followCharacter = false;

        [Header("特效本地位置偏移")]
        public Vector3 localPositionOffset = Vector3.zero;

        [Header("特效本地旋转偏移")]
        public Vector3 localRotationOffset = Vector3.zero;

        [Header("是否在手动结束时强制销毁")]
        public bool destroyOnStop = true;
    }

    [CreateAssetMenu(menuName = "WutheringWaves/EffectActionConfig", fileName = "EffectActionConfig", order = 4)]
    // 角色攻击特效配置：集中维护 AttackId 到特效播放列表的映射
    public class EffectActionConfigSO : ScriptableObject
    {
        [Header("===角色信息===")]
        [SerializeField] internal CharacterName characterName;
        [Header("=== 攻击特效动作列表 ===")]
        [Tooltip("按 AttackId 映射不同攻击步骤对应的特效表现")]
        [SerializeField] internal List<AttackEffectActionConfig> effectActions = new List<AttackEffectActionConfig>();

        // 根据攻击步骤ID获取对应的特效动作配置
        public AttackEffectActionConfig GetEffectActionConfig(AttackId attackId)
        {
            return effectActions.FirstOrDefault(config => config != null && config.attackId == attackId);
        }
    }
}

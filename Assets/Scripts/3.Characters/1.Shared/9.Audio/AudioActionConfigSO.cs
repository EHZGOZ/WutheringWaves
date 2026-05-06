using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WutheringWaves
{
    [System.Serializable]
    // 单个音效播放配置：包含音频、延迟、音量、循环和2D/3D混合
    public class AudioSpawnConfig
    {
        [Header("音效Clip")]
        public AudioClip audioClip;

        [Header("延迟播放时间")]
        public float delayTime = 0f;

        [Header("销毁延迟时间")]
        public float destroyDelay = 0f;

        [Header("单条音效音量")]
        [Range(0f, 1f)] public float volume = 1f;

        [Header("是否循环")]
        public bool isLoop = false;

        [Header("2D/3D混合：0=2D，1=3D")]
        [Range(0f, 1f)] public float spatialBlend = 0f;
    }

    [System.Serializable]
    // 单条攻击音效配置：按 AttackId 映射一组音效
    public class AttackAudioActionConfig
    {
        [Header("攻击步骤ID")]
        public AttackId attackId = AttackId.None;

        [Header("音效播放列表")]
        public List<AudioSpawnConfig> audioSpawns = new List<AudioSpawnConfig>();

        public override string ToString()
        {
            return $"攻击音效动作：{attackId}";
        }
    }

    [System.Serializable]
    // 单条移动音效配置：按 LocomotionAnimationId 映射一组音效
    public class LocomotionAudioActionConfig
    {
        [Header("移动动画ID")]
        public LocomotionAnimationId locomotionAnimationId = LocomotionAnimationId.None;

        [Header("音效播放列表")]
        public List<AudioSpawnConfig> audioSpawns = new List<AudioSpawnConfig>();

        public override string ToString()
        {
            return $"移动音效动作：{locomotionAnimationId}";
        }
    }

    [CreateAssetMenu(menuName = "WutheringWaves/AudioActionConfig", fileName = "AudioActionConfig", order = 5)]
    // 角色音效配置：集中维护 AttackId / LocomotionAnimationId 到音效表现的映射
    public class AudioActionConfigSO : ScriptableObject
    {
        [Header("===角色信息===")]
        [SerializeField] internal CharacterName characterName;

        [Header("=== 攻击音效动作列表 ===")]
        [Tooltip("按 AttackId 映射不同攻击步骤对应的音效表现")]
        [SerializeField] internal List<AttackAudioActionConfig> attackAudioActions = new List<AttackAudioActionConfig>();

        [Header("=== 移动音效动作列表 ===")]
        [Tooltip("按 LocomotionAnimationId 映射不同移动动作对应的音效表现")]
        [SerializeField] internal List<LocomotionAudioActionConfig> locomotionAudioActions = new List<LocomotionAudioActionConfig>();

        // 根据攻击步骤ID获取对应的音效动作配置
        public AttackAudioActionConfig GetAudioActionConfig(AttackId attackId)
        {
            return attackAudioActions.FirstOrDefault(config => config != null && config.attackId == attackId);
        }

        // 根据移动动画ID获取对应的音效动作配置
        public LocomotionAudioActionConfig GetAudioActionConfig(LocomotionAnimationId locomotionAnimationId)
        {
            return locomotionAudioActions.FirstOrDefault(config => config != null && config.locomotionAnimationId == locomotionAnimationId);
        }
    }
}

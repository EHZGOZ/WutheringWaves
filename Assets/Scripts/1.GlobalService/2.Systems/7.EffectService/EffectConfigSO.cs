using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 特效配置文件：集中维护所有可被代码调用的特效条目
    [CreateAssetMenu(fileName = "EffectConfig", menuName = "WutheringWaves/Effects/Effect Config")]
    public class EffectConfigSO : ScriptableObject
    {
        [Header("特效定义列表")]
        [SerializeField] private List<EffectEntry> effectEntries = new List<EffectEntry>(); // 所有特效配置项

        public IReadOnlyList<EffectEntry> EffectEntries => effectEntries; // 对外只读，避免运行时被随意改写
        public bool HasEntries => effectEntries != null && effectEntries.Count > 0; // 供服务层快速判断是否存在有效配置
    }

    // 单条特效配置：描述一个特效的查找名、预制体和生成偏移
    [System.Serializable]
    public class EffectEntry
    {
        [Tooltip("特效名称（用于代码调用时查找）")]
        public string effectName;

        [Tooltip("对应的粒子特效预制体")]
        public ParticleSystem prefab;

        [Tooltip("延迟生成时长")]
        public float delayTime;

        [Tooltip("相对调用者的XYZ位置偏移")]
        public Vector3 posOffset;

        [Tooltip("相对调用者的XYZ角度偏移")]
        public Vector3 rotOffset;
    }
}

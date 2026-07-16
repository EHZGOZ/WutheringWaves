using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 特效服务：负责根据特效名称生成、播放和销毁特效
    public class EffectService : MonoBehaviour
    {
        public static EffectService Instance { get; private set; }

        [Header("=== 特效配置 ===")]
        [SerializeField] private EffectConfigSO effectConfig; // 特效配置表

        public bool IsInitialized { get; private set; } // 是否已初始化

        private readonly Dictionary<string, EffectEntry> _effectDict = new Dictionary<string, EffectEntry>(); // 特效名称 -> 特效配置

        #region 生命周期
        private void Awake()
        {
            // 1.保持单例，避免多个EffectService争抢特效管理权
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
        // 初始化特效服务
        public void Initialize()
        {
            // 1.已经初始化过时直接返回，避免重复重建特效字典
            if (IsInitialized)
            {
                return;
            }

            // 2.初始化特效字典
            InitializeEffectDictionary();

            // 3.标记初始化完成
            IsInitialized = true;
        }
        // 初始化特效字典
        private void InitializeEffectDictionary()
        {
            // 1.先清空旧缓存，避免重复配置残留
            _effectDict.Clear();

            // 2.配置为空时进入空实现状态
            if (effectConfig == null || !effectConfig.HasEntries)
            {
                Debug.LogWarning("[EffectService] EffectConfigSO未配置或为空，特效服务将处于空实现状态。", this);
                return;
            }

            // 3.逐个写入特效配置
            IReadOnlyList<EffectEntry> entries = effectConfig.EffectEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                EffectEntry entry = entries[i];
                if (entry == null || entry.prefab == null || string.IsNullOrWhiteSpace(entry.effectName))
                {
                    continue;
                }

                // 4.重复名称跳过，避免覆盖导致排查困难
                if (_effectDict.ContainsKey(entry.effectName))
                {
                    Debug.LogWarning($"[EffectService] 重复的特效名称 '{entry.effectName}'，已跳过。", this);
                    continue;
                }

                // 5.写入字典
                _effectDict.Add(entry.effectName, entry);
            }
        }
        #endregion

        #region 播放特效
        // 播放指定名称的特效
        public void PlayEffect(string effectName, Transform caller)
        {

            // 2.调用者为空时不播放
            if (caller == null)
            {
                Debug.LogWarning("[EffectService] 播放特效失败：调用者Transform为空。", this);
                return;
            }

            // 3.特效名称为空时不播放
            if (string.IsNullOrWhiteSpace(effectName))
            {
                Debug.LogWarning("[EffectService] 播放特效失败：特效名称为空。", this);
                return;
            }

            // 4.找不到特效配置时不播放
            if (!_effectDict.TryGetValue(effectName, out EffectEntry entry))
            {
                Debug.LogWarning($"[EffectService] 找不到名称为 '{effectName}' 的特效配置。", this);
                return;
            }

            // 5.有延迟时走协程播放
            if (entry.delayTime > 0f)
            {
                StartCoroutine(DelayPlayCoroutine(entry, caller));
                return;
            }

            // 6.无延迟时立即播放
            SpawnAndPlayEffect(entry, caller);
        }
        #endregion

        #region 销毁特效
        // 销毁当前特效服务节点下的所有特效
        public void DestroyAllEffects()
        {
            // 1.逐个销毁子物体特效
            foreach (Transform effect in transform)
            {
                UnityEngine.Object.Destroy(effect.gameObject);
            }
        }

        // 销毁指定特效对象
        public void DestroyEffect(GameObject effect)
        {
            // 1.空对象不处理
            if (effect == null)
            {
                return;
            }

            // 2.销毁指定特效对象
            UnityEngine.Object.Destroy(effect);
        }
        #endregion

        #region 查询
        // 判断是否存在指定名称的特效
        public bool HasEffect(string effectName)
        {


            // 1.空名称直接返回false
            if (string.IsNullOrWhiteSpace(effectName))
            {
                return false;
            }

            // 1.查询特效字典
            return _effectDict.ContainsKey(effectName);
        }
        #endregion

        #region 静态快捷入口
        // 静态播放入口：方便其他脚本快速播放特效
        public static void Play(string effectName, Transform caller)
        {
            EffectService service = ResolveService();
            if (service != null)
            {
                service.PlayEffect(effectName, caller);
            }
        }

        // 静态销毁全部入口
        public static void DestroyAll()
        {
            EffectService service = ResolveService();
            if (service != null)
            {
                service.DestroyAllEffects();
            }
        }

        // 静态销毁指定特效入口
        public static void Destroy(GameObject effect)
        {
            EffectService service = ResolveService();
            if (service != null)
            {
                service.DestroyEffect(effect);
            }
        }
        #endregion

        #region 协程播放
        // 延迟播放特效
        private IEnumerator DelayPlayCoroutine(EffectEntry entry, Transform caller)
        {
            // 1.等待配置的延迟时间
            yield return new WaitForSeconds(entry.delayTime);

            // 2.调用者仍然存在时再播放
            if (caller != null)
            {
                SpawnAndPlayEffect(entry, caller);
            }
        }
        #endregion

        #region 生成特效
        // 生成并播放特效
        private void SpawnAndPlayEffect(EffectEntry entry, Transform caller)
        {
            // 1.空值检查
            if (entry == null || entry.prefab == null || caller == null)
            {
                return;
            }

            // 2.生成特效实例，并挂到特效服务节点下统一管理
            ParticleSystem effectInstance = Instantiate(entry.prefab, transform);

            // 3.根据调用者位置和配置偏移设置特效位置旋转
            effectInstance.transform.position = caller.position + caller.rotation * entry.posOffset;
            effectInstance.transform.rotation = caller.rotation * Quaternion.Euler(entry.rotOffset);

            // 4.播放特效
            effectInstance.Play();

            // 5.非循环特效播放完成后自动销毁
            if (!effectInstance.main.loop)
            {
                float totalDuration = CalculateEffectTotalDuration(effectInstance);
                Destroy(effectInstance.gameObject, totalDuration);
            }
        }

        // 计算特效总持续时间：包含子粒子系统
        private float CalculateEffectTotalDuration(ParticleSystem ps)
        {
            // 1.空值检查
            if (ps == null)
            {
                return 0f;
            }

            // 2.先读取主粒子系统持续时间
            float maxDuration = 0f;
            ParticleSystem.MainModule main = ps.main;
            maxDuration = main.duration + main.startLifetime.constantMax;

            // 3.遍历子粒子系统，取最长持续时间
            ParticleSystem[] allParticles = ps.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < allParticles.Length; i++)
            {
                ParticleSystem.MainModule childMain = allParticles[i].main;
                float childDuration = childMain.duration + childMain.startLifetime.constantMax;
                if (childDuration > maxDuration)
                {
                    maxDuration = childDuration;
                }
            }

            return maxDuration;
        }
        #endregion

        #region 服务解析
        // 解析特效服务：优先使用单例，兜底从场景查找
        private static EffectService ResolveService()
        {
            // 1.优先使用现有单例
            if (Instance != null)
            {
                if (!Instance.IsInitialized)
                {
                    Instance.Initialize();
                }

                return Instance;
            }

            // 2.兜底从场景中查找
            EffectService discovered = FindObjectOfType<EffectService>(true);
            if (discovered != null)
            {
                Instance = discovered;

                if (!Instance.IsInitialized)
                {
                    Instance.Initialize();
                }
            }

            return Instance;
        }
        #endregion
    }
}

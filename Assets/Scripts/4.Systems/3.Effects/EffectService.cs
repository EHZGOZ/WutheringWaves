using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    public class EffectService : MonoBehaviour
    {
        private static EffectService _instance;

        public static EffectService Instance => _instance;

        [SerializeField] private EffectConfigSO effectConfig;

        public bool IsInitialized { get; private set; }

        private readonly Dictionary<string, EffectEntry> _effectDict = new Dictionary<string, EffectEntry>();

        private void Awake()
        {
            if (!ReferenceEquals(_instance, null) && !ReferenceEquals(_instance, this))
            {
                Debug.LogWarning($"[EffectService] Duplicate instance detected on {gameObject.name}, keeping the first one.", this);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
            }
        }

        public void Initialize()
        {
            InitializeEffectDictionary();
            IsInitialized = true;
        }

        public void PlayEffect(string effectName, Transform caller)
        {
            EnsureInitialized();

            if (caller == null)
            {
                Debug.LogWarning("[EffectService] 调用者Transform为空！", this);
                return;
            }

            if (!_effectDict.TryGetValue(effectName, out EffectEntry entry))
            {
                Debug.LogWarning($"[EffectService] 找不到名称为 '{effectName}' 的特效配置！", this);
                return;
            }

            if (entry.delayTime > 0f)
            {
                StartCoroutine(DelayPlayCoroutine(entry, caller));
                return;
            }

            SpawnAndPlayEffect(entry, caller);
        }

        public void DestroyAllEffects()
        {
            EnsureInitialized();

            foreach (Transform effect in transform)
            {
                // 这里必须显式调用 Unity 的销毁接口，避免误调到本类的静态 Destroy 包装方法导致递归。
                UnityEngine.Object.Destroy(effect.gameObject);
            }
        }

        public void DestroyEffect(GameObject effect)
        {
            EnsureInitialized();

            if (effect != null)
            {
                // 这里必须显式调用 Unity 的销毁接口，避免误调到本类的静态 Destroy 包装方法导致递归。
                UnityEngine.Object.Destroy(effect);
            }
        }

        public bool HasEffect(string effectName)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(effectName) && _effectDict.ContainsKey(effectName);
        }

        public static void Play(string effectName, Transform caller)
        {
            EffectService service = ResolveService();
            if (!ReferenceEquals(service, null))
            {
                service.PlayEffect(effectName, caller);
            }
        }

        public static void DestroyAll()
        {
            EffectService service = ResolveService();
            if (!ReferenceEquals(service, null))
            {
                service.DestroyAllEffects();
            }
        }

        public static void Destroy(GameObject effect)
        {
            EffectService service = ResolveService();
            if (!ReferenceEquals(service, null))
            {
                service.DestroyEffect(effect);
            }
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
        }

        private void InitializeEffectDictionary()
        {
            _effectDict.Clear();

            if (effectConfig == null || !effectConfig.HasEntries)
            {
                Debug.LogWarning("[EffectService] EffectConfigSO 未配置或为空，特效服务将处于空实现状态。", this);
                return;
            }

            IReadOnlyList<EffectEntry> entries = effectConfig.EffectEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                EffectEntry entry = entries[i];
                if (entry == null || entry.prefab == null || string.IsNullOrWhiteSpace(entry.effectName))
                {
                    continue;
                }

                if (_effectDict.ContainsKey(entry.effectName))
                {
                    Debug.LogWarning($"[EffectService] 重复的特效名称 '{entry.effectName}'，已跳过。", this);
                    continue;
                }

                _effectDict.Add(entry.effectName, entry);
            }
        }

        private IEnumerator DelayPlayCoroutine(EffectEntry entry, Transform caller)
        {
            yield return new WaitForSeconds(entry.delayTime);

            if (caller != null)
            {
                SpawnAndPlayEffect(entry, caller);
            }
        }

        private void SpawnAndPlayEffect(EffectEntry entry, Transform caller)
        {
            ParticleSystem effectInstance = Instantiate(entry.prefab, transform);
            effectInstance.transform.position = caller.position + caller.rotation * entry.posOffset;
            effectInstance.transform.rotation = caller.rotation * Quaternion.Euler(entry.rotOffset);
            effectInstance.Play();

            if (!effectInstance.main.loop)
            {
                float totalDuration = CalculateEffectTotalDuration(effectInstance);
                Destroy(effectInstance.gameObject, totalDuration);
            }
        }

        private float CalculateEffectTotalDuration(ParticleSystem ps)
        {
            float maxDuration = 0f;
            ParticleSystem.MainModule main = ps.main;
            maxDuration = main.duration + main.startLifetime.constantMax;

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

        private static EffectService ResolveService()
        {
            if (!ReferenceEquals(_instance, null))
            {
                if (!_instance.IsInitialized)
                {
                    _instance.Initialize();
                }

                return _instance;
            }

            EffectService discovered = FindObjectOfType<EffectService>(true);
            if (!ReferenceEquals(discovered, null))
            {
                _instance = discovered;
                if (!_instance.IsInitialized)
                {
                    _instance.Initialize();
                }
            }

            return _instance;
        }
    }
}

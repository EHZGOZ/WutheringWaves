using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 特效控制器：负责攻击特效的播放、延迟生成、跟随、自动回收与中断清理
    public class EffectController : MonoBehaviour
    {
        private class ActiveEffectInstance
        {
            public GameObject effectObject;
            public bool destroyOnStop;
        }

        private CharacterContext context; // 角色共享上下文
        private EffectActionConfigSO effectConfig; // 角色攻击特效配置

        private readonly List<Coroutine> delayCoroutines = new List<Coroutine>(); // 当前仍在等待播放的特效协程
        private readonly List<ActiveEffectInstance> activeEffects = new List<ActiveEffectInstance>(); // 当前已生成但尚未清理的特效对象

        // 初始化：由 CharacterContext 统一调用，绑定共享上下文与特效配置
        public void Initialize(CharacterContext context)
        {
            this.context = context;
            effectConfig = context != null && context.CharacterDataSO != null ? context.CharacterDataSO.effectActionConfigSO : null;
        }

        // 根据攻击步骤播放对应特效
        public void PlayEffectAction(AttackStep attackStep)
        {
            EndEffectAction();

            if (attackStep == null || attackStep.attackId == AttackId.None || effectConfig == null)
            {
                return;
            }

            AttackEffectActionConfig actionConfig = effectConfig.GetEffectActionConfig(attackStep.attackId);
            if (actionConfig == null || actionConfig.effectSpawns == null || actionConfig.effectSpawns.Count == 0)
            {
                return;
            }

            for (int i = 0; i < actionConfig.effectSpawns.Count; i++)
            {
                EffectSpawnConfig spawnConfig = actionConfig.effectSpawns[i];
                if (spawnConfig == null || spawnConfig.effectPrefab == null)
                {
                    continue;
                }

                if (spawnConfig.delayTime > 0f)
                {
                    Coroutine delayCoroutine = StartCoroutine(DelayPlayEffect(spawnConfig));
                    delayCoroutines.Add(delayCoroutine);
                    continue;
                }

                SpawnEffect(spawnConfig);
            }
        }

        // 结束当前特效表现：停止未触发的延迟协程，并清理已生成特效
        public void EndEffectAction()
        {
            for (int i = 0; i < delayCoroutines.Count; i++)
            {
                Coroutine delayCoroutine = delayCoroutines[i];
                if (delayCoroutine != null)
                {
                    StopCoroutine(delayCoroutine);
                }
            }
            delayCoroutines.Clear();

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveEffectInstance activeEffect = activeEffects[i];
                if (activeEffect != null && activeEffect.destroyOnStop && activeEffect.effectObject != null)
                {
                    Destroy(activeEffect.effectObject);
                }
            }
            activeEffects.Clear();
        }

        // 延迟播放单个特效
        private IEnumerator DelayPlayEffect(EffectSpawnConfig spawnConfig)
        {
            yield return new WaitForSeconds(spawnConfig.delayTime);
            SpawnEffect(spawnConfig);
        }

        // 生成并播放单个特效
        private void SpawnEffect(EffectSpawnConfig spawnConfig)
        {
            if (spawnConfig == null || spawnConfig.effectPrefab == null)
            {
                return;
            }

            ParticleSystem effectInstance = Instantiate(spawnConfig.effectPrefab);
            if (effectInstance == null)
            {
                return;
            }

            Transform effectTransform = effectInstance.transform;
            Transform parent = spawnConfig.followCharacter ? transform : null;
            effectTransform.SetParent(parent, false);

            if (spawnConfig.followCharacter)
            {
                effectTransform.localPosition = spawnConfig.localPositionOffset;
                effectTransform.localRotation = Quaternion.Euler(spawnConfig.localRotationOffset);
            }
            else
            {
                effectTransform.position = transform.position + transform.rotation * spawnConfig.localPositionOffset;
                effectTransform.rotation = transform.rotation * Quaternion.Euler(spawnConfig.localRotationOffset);
            }

            ActiveEffectInstance activeEffect = new ActiveEffectInstance
            {
                effectObject = effectInstance.gameObject,
                destroyOnStop = spawnConfig.destroyOnStop
            };
            activeEffects.Add(activeEffect);
            effectInstance.Play();

            if (!effectInstance.main.loop)
            {
                float totalDuration = CalculateEffectTotalDuration(effectInstance);
                StartCoroutine(AutoRecycleEffect(activeEffect, totalDuration));
            }
        }

        // 自动回收非循环特效，避免场景中残留一次性粒子对象
        private IEnumerator AutoRecycleEffect(ActiveEffectInstance activeEffect, float delayTime)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delayTime));

            if (activeEffect != null && activeEffect.effectObject != null)
            {
                activeEffects.Remove(activeEffect);
                Destroy(activeEffect.effectObject);
            }
        }

        // 计算特效总播放时长：取根粒子与所有子粒子的最大持续时间
        private float CalculateEffectTotalDuration(ParticleSystem ps)
        {
            float maxDuration = 0f;
            ParticleSystem[] allParticles = ps.GetComponentsInChildren<ParticleSystem>(true);

            for (int i = 0; i < allParticles.Length; i++)
            {
                ParticleSystem.MainModule main = allParticles[i].main;
                float totalDuration = main.duration + main.startLifetime.constantMax;
                if (totalDuration > maxDuration)
                {
                    maxDuration = totalDuration;
                }
            }

            return maxDuration;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 音效控制器：负责攻击/移动音效的播放、延迟生成、循环播放与中断清理
    public class AudioController : MonoBehaviour
    {
        private class ActiveAudioInstance
        {
            public GameObject audioObject; // 当前生成的音源对象
            public AudioSource audioSource; // 当前播放的AudioSource
            public bool isLoop; // 当前音效是否循环
            public float destroyDelay; // 退出状态后的销毁延迟时间
        }

        private CharacterContext context; // 角色共享上下文
        private AudioActionConfigSO audioConfig; // 角色音效配置

        [Header("=== 音效总音量 ===")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

        private readonly List<Coroutine> delayCoroutines = new List<Coroutine>(); // 当前等待播放的延迟协程
        private readonly List<Coroutine> recycleCoroutines = new List<Coroutine>(); // 当前等待回收的音源协程
        private readonly List<ActiveAudioInstance> activeAudios = new List<ActiveAudioInstance>(); // 当前已生成但尚未清理的音源对象

        #region 初始化
        // 初始化：由 CharacterContext 统一调用，绑定角色上下文与音效配置
        public void Initialize(CharacterContext context)
        {
            this.context = context;
            audioConfig = context != null && context.CharacterDataSO != null
                ? context.CharacterDataSO.audioActionConfigSO
                : null;
        }
        #endregion

        #region 生命周期
        private void OnDisable()
        {
            EndAudioAction();
        }
        #endregion

        #region 对外接口
        // 根据攻击步骤播放对应音效
        public void PlayAudioAction(AttackStep attackStep)
        {
            if (attackStep == null)
            {
                return;
            }

            PlayAudioAction(attackStep.attackId);
        }

        // 根据攻击ID播放对应音效
        public void PlayAudioAction(AttackId attackId)
        {
            // 新动作开始前先清理旧音效，避免多个动作音效互相残留
            EndAudioAction();

            if (attackId == AttackId.None || audioConfig == null)
            {
                return;
            }

            AttackAudioActionConfig actionConfig = audioConfig.GetAudioActionConfig(attackId);
            if (actionConfig == null || actionConfig.audioSpawns == null || actionConfig.audioSpawns.Count == 0)
            {
                return;
            }

            PlayAudioSpawns(actionConfig.audioSpawns);
        }

        // 根据移动动画ID播放对应音效
        public void PlayAudioAction(LocomotionAnimationId locomotionAnimationId)
        {
            // 新动作开始前先清理旧音效，避免移动音效互相残留
            EndAudioAction();

            if (locomotionAnimationId == LocomotionAnimationId.None || audioConfig == null)
            {
                return;
            }

            LocomotionAudioActionConfig actionConfig = audioConfig.GetAudioActionConfig(locomotionAnimationId);
            if (actionConfig == null || actionConfig.audioSpawns == null || actionConfig.audioSpawns.Count == 0)
            {
                return;
            }

            PlayAudioSpawns(actionConfig.audioSpawns);
        }

        // 结束当前音效表现：停止未触发的延迟协程，并清理所有已生成音源
        public void EndAudioAction()
        {
            StopDelayCoroutines();
            StopRecycleCoroutines();
            ClearActiveAudios();
        }
        #endregion

        #region 播放入口
        // 播放一组音效配置
        private void PlayAudioSpawns(List<AudioSpawnConfig> audioSpawns)
        {
            for (int i = 0; i < audioSpawns.Count; i++)
            {
                AudioSpawnConfig spawnConfig = audioSpawns[i];
                if (spawnConfig == null || spawnConfig.audioClip == null)
                {
                    continue;
                }

                if (spawnConfig.delayTime > 0f)
                {
                    Coroutine delayCoroutine = StartCoroutine(DelayPlayAudio(spawnConfig));
                    delayCoroutines.Add(delayCoroutine);
                    continue;
                }

                SpawnAudio(spawnConfig);
            }
        }
        #endregion

        #region 协程控制
        // 延迟播放单个音效
        private IEnumerator DelayPlayAudio(AudioSpawnConfig spawnConfig)
        {
            yield return new WaitForSeconds(spawnConfig.delayTime);

            SpawnAudio(spawnConfig);
            delayCoroutines.RemoveAll(coroutine => coroutine == null);
        }

        // 自动回收非循环音效，避免场景中残留一次性音源对象
        private IEnumerator AutoRecycleAudio(ActiveAudioInstance activeAudio, float delayTime)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delayTime));

            if (activeAudio != null && activeAudio.audioObject != null)
            {
                activeAudios.Remove(activeAudio);
                Destroy(activeAudio.audioObject);
            }

            recycleCoroutines.RemoveAll(coroutine => coroutine == null);
        }

        // 停止所有延迟播放协程
        private void StopDelayCoroutines()
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
        }

        // 停止所有自动回收协程
        private void StopRecycleCoroutines()
        {
            for (int i = 0; i < recycleCoroutines.Count; i++)
            {
                Coroutine recycleCoroutine = recycleCoroutines[i];
                if (recycleCoroutine != null)
                {
                    StopCoroutine(recycleCoroutine);
                }
            }

            recycleCoroutines.Clear();
        }
        #endregion

        #region 音效生成
        // 生成并播放单个音效
        private void SpawnAudio(AudioSpawnConfig spawnConfig)
        {
            if (spawnConfig == null || spawnConfig.audioClip == null)
            {
                return;
            }

            GameObject audioObject = new GameObject("CharacterAudio_" + spawnConfig.audioClip.name);
            audioObject.transform.SetParent(transform, false);
            audioObject.transform.localPosition = Vector3.zero;

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = spawnConfig.audioClip;
            audioSource.volume = Mathf.Clamp01(masterVolume * spawnConfig.volume);
            audioSource.loop = spawnConfig.isLoop;
            audioSource.spatialBlend = Mathf.Clamp01(spawnConfig.spatialBlend);
            audioSource.playOnAwake = false;

            ActiveAudioInstance activeAudio = new ActiveAudioInstance
            {
                audioObject = audioObject,
                audioSource = audioSource,
                isLoop = spawnConfig.isLoop,
                destroyDelay = Mathf.Max(0f, spawnConfig.destroyDelay)
            };

            activeAudios.Add(activeAudio);
            audioSource.Play();

            if (!spawnConfig.isLoop)
            {
                Coroutine recycleCoroutine = StartCoroutine(AutoRecycleAudio(activeAudio, spawnConfig.audioClip.length + spawnConfig.destroyDelay));
                recycleCoroutines.Add(recycleCoroutine);
            }
        }

        // 清理当前已生成音效
        private void ClearActiveAudios()
        {
            for (int i = activeAudios.Count - 1; i >= 0; i--)
            {
                ActiveAudioInstance activeAudio = activeAudios[i];
                if (activeAudio == null)
                {
                    continue;
                }

                if (activeAudio.audioSource != null)
                {
                    // 循环音效必须立刻停止，避免移动/奔跑这类循环声在退出状态后残留
                    // 非循环音效如果配置了销毁延迟，就允许它继续播放一小段时间
                    if (activeAudio.isLoop || activeAudio.destroyDelay <= 0f)
                    {
                        activeAudio.audioSource.Stop();
                    }
                }

                if (activeAudio.audioObject != null)
                {
                    Destroy(activeAudio.audioObject, activeAudio.destroyDelay);
                }
            }

            activeAudios.Clear();
        }
        #endregion
    }
}

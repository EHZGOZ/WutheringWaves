namespace WutheringWaves
{
    using UnityEngine;
    using System.Collections;
    using System.Linq;

    [System.Serializable]
    public class SoundEffect
    {
        public string soundName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("播放速度（1为正常速度，>1加速，<1减速）")]
        [Range(0.1f, 3.0f)] public float speed = 1f;
        public bool loop = false;
        [HideInInspector] public AudioSource source;
        [Tooltip("跳过开头的空白时间（秒）")]
        public float startOffset = 0f;
        [Tooltip("跳过结尾的空白时间（秒）")]
        public float endOffset = 0f;
    }

    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        [Header("战斗音效")]
        public SoundEffect weaponSwing;
        public SoundEffect takeDamage;
        public SoundEffect weaponImpact;
        public SoundEffect perfectDodge;
        public SoundEffect perfectBlock;
        public SoundEffect backGround;
        public SoundEffect PlayerWeaponSwing1;
        public SoundEffect PlayerWeaponSwing2;
        public SoundEffect PlayerWeaponSwing3;
        public SoundEffect EnemyWeaponSwing;
        public SoundEffect PlayertakeDamage;
        public SoundEffect EnemytakeDamage;
        public SoundEffect playerRun;  // 跑步音效
        public SoundEffect playerWalk;
        public SoundEffect playerHeal;

        [Header("音频源设置")]
        [SerializeField] private int poolSize = 8;
        [SerializeField] private AudioSource backgroundSource;
        [SerializeField] private AudioSource playerRunSource;  // 跑步音效专用音频源
        private AudioSource[] audioSources;
        private int currentSourceIndex = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
            }
            else
            {
                Debug.LogWarning($"[SoundManager] 发现重复实例，销毁当前物体: {gameObject.name}");
                Destroy(gameObject);
            }
        }

        private void InitializeAudioSources()
        {
            // 初始化通用音效池
            audioSources = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                GameObject sourceObj = new GameObject($"EffectAudioSource_{i}");
                sourceObj.transform.parent = transform;
                audioSources[i] = sourceObj.AddComponent<AudioSource>();
            }

            // 初始化背景音乐源
            if (backgroundSource == null)
            {
                GameObject bgObj = new GameObject("BackgroundAudioSource");
                bgObj.transform.parent = transform;
                backgroundSource = bgObj.AddComponent<AudioSource>();
            }
            else
            {
                if (audioSources.Contains(backgroundSource))
                {
                    Debug.LogWarning("[SoundManager] 背景音乐源不能属于音效池，已自动修复");
                    backgroundSource.transform.parent = transform;
                    backgroundSource.gameObject.name = "BackgroundAudioSource";
                }
            }

            // 初始化跑步音效专用源
            if (playerRunSource == null)
            {
                GameObject runObj = new GameObject("PlayerRunAudioSource");
                runObj.transform.parent = transform;
                playerRunSource = runObj.AddComponent<AudioSource>();
            }
        }

        private AudioSource GetNextAvailableSource()
        {
            if (audioSources == null)
            {
                Debug.LogError("Audio source pool not initialized! Check SoundManager's Awake execution.");
                InitializeAudioSources();
            }

            currentSourceIndex = (currentSourceIndex + 1) % poolSize;
            return audioSources[currentSourceIndex];
        }

        // 跑步音效控制（使用专用音频源）
        public void PlayPlayerRun() => PlaySoundEffect(playerRun, playerRunSource);
        public void StopPlayerRunSound()
        {
            if (playerRunSource != null)
            {
                playerRunSource.Stop();
            }
        }
        public void PausePlayerRunSound()
        {
            if (playerRunSource != null && playerRunSource.isPlaying)
            {
                playerRunSource.Pause();
            }
        }
        public void ResumePlayerRunSound()
        {
            if (playerRunSource != null && !playerRunSource.isPlaying && playerRunSource.clip != null)
            {
                playerRunSource.UnPause();
            }
        }

        // 其他音效播放方法
        public void PlayPlayerWeaponSwing1() => PlaySoundEffect(PlayerWeaponSwing1);
        public void PlayPlayerWeaponSwing2() => PlaySoundEffect(PlayerWeaponSwing2);
        public void PlayPlayerWeaponSwing3() => PlaySoundEffect(PlayerWeaponSwing3);
        public void PlayEnemyWeaponSwing() => PlaySoundEffect(EnemyWeaponSwing);
        public void PlayPlayerTakeDamage() => PlaySoundEffect(PlayertakeDamage);
        public void PlayEnemyTakeDamage() => PlaySoundEffect(EnemytakeDamage);
        public void PlayWeaponSwing() => PlaySoundEffect(weaponSwing);
        public void PlayTakeDamage() => PlaySoundEffect(takeDamage);
        public void PlayWeaponImpact() => PlaySoundEffect(weaponImpact);
        public void PlayPerfectDodge() => PlaySoundEffect(perfectDodge);
        public void PlayPerfectBlock() => PlaySoundEffect(perfectBlock);
        public void PlayPlayerWalk() => PlaySoundEffect(playerWalk);
        public void PlayPlayerHeal() => PlaySoundEffect(playerHeal);

        // 背景音乐控制
        public void PlayBackGround()
        {
            if (backgroundSource == null)
            {
                Debug.LogError("[SoundManager] 背景音乐源未初始化！");
                return;
            }
            PlaySoundEffect(backGround, backgroundSource);
        }

        private void PlaySoundEffect(SoundEffect sound)
        {
            if (sound == null)
            {
                Debug.LogWarning("[SoundManager] 尝试播放空音效！");
                return;
            }
            PlaySoundEffect(sound, GetNextAvailableSource());
        }

        private void PlaySoundEffect(SoundEffect sound, AudioSource source)
        {
            if (sound.clip == null)
            {
                Debug.LogWarning($"[SoundManager] 音效 {sound.soundName} 未赋值音频文件！");
                return;
            }

            source.clip = sound.clip;
            source.volume = sound.volume;
            source.pitch = sound.speed;
            source.loop = sound.loop;

            if (sound.startOffset > 0)
            {
                float validStart = Mathf.Clamp(sound.startOffset, 0f, sound.clip.length - 0.01f);
                source.time = validStart;
            }

            if (!sound.loop && sound.endOffset > 0)
            {
                float totalDuration = sound.clip.length;
                float playDuration = totalDuration - sound.startOffset - sound.endOffset;
                if (playDuration > 0)
                {
                    StartCoroutine(StopAfterDuration(source, playDuration / sound.speed));
                }
            }

            source.Play();
        }

        private IEnumerator StopAfterDuration(AudioSource source, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }

        // 音效全局控制
        public void StopAllEffects()
        {
            if (audioSources != null)
            {
                foreach (var source in audioSources)
                {
                    if (source != null && source.isPlaying)
                    {
                        source.Stop();
                    }
                }
            }
            // 单独停止跑步音效
            StopPlayerRunSound();
        }

        public void StopBackgroundMusic()
        {
            if (backgroundSource != null && backgroundSource.isPlaying)
            {
                backgroundSource.Stop();
            }
        }

        public void PauseAllEffects()
        {
            if (audioSources != null)
            {
                foreach (var source in audioSources)
                {
                    if (source != null && source.isPlaying)
                    {
                        source.Pause();
                    }
                }
            }
            // 单独暂停跑步音效
            PausePlayerRunSound();
        }

        public void ResumeAllEffects()
        {
            if (audioSources != null)
            {
                foreach (var source in audioSources)
                {
                    if (source != null && !source.isPlaying && source.clip != null)
                    {
                        source.UnPause();
                    }
                }
            }
            // 单独恢复跑步音效
            ResumePlayerRunSound();
        }

        public void PauseBackgroundMusic()
        {
            if (backgroundSource != null && backgroundSource.isPlaying)
            {
                backgroundSource.Pause();
            }
        }

        public void ResumeBackgroundMusic()
        {
            if (backgroundSource != null && !backgroundSource.isPlaying && backgroundSource.clip != null)
            {
                backgroundSource.UnPause();
            }
        }

        // 状态查询
        public bool IsBackgroundMusicPlaying()
        {
            return backgroundSource != null && backgroundSource.isPlaying;
        }

        public bool IsBackgroundMusicPaused()
        {
            return backgroundSource != null && !backgroundSource.isPlaying && backgroundSource.clip != null;
        }

        // 音量控制
        public void SetBackgroundVolume(float volume)
        {
            if (backgroundSource != null)
            {
                backgroundSource.volume = Mathf.Clamp01(volume);
            }
        }

        public float GetBackgroundVolume()
        {
            return backgroundSource != null ? backgroundSource.volume : 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

}


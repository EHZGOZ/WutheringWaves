using UnityEngine;
using UnityEngine.Serialization;

namespace WutheringWaves
{
    // 音频服务：统一管理总音量、背景音乐音量、总音效音量和背景音乐播放
    public class AudioService : MonoBehaviour
    {
        public static AudioService Instance { get; private set; } // 音频服务单例入口

        [Header("背景音源")]
        [SerializeField] private AudioSource backgroundAudioSource; // 背景音乐音源，负责真正播放背景音乐
        [Header("=== 默认音量 ===")]
        private const float DefaultMasterVolume = 1f; // 默认总音量
        private const float DefaultBackgroundVolume = 0.2f; // 默认背景音乐音量
        private const float DefaultSfxVolume = 1f; // 默认总音效音量

        [Header("背景音乐")]
        [SerializeField] private AudioClip menuBackgroundClip; // 主界面和登录界面背景音乐
        [FormerlySerializedAs("defaultBackgroundClip")]
        [SerializeField] private AudioClip gameplayBackgroundClip; // 普通游戏中背景音乐，兼容原来的默认背景音乐字段
        [SerializeField] private AudioClip battleBackgroundClip; // 战斗中背景音乐，Boss战或其他战斗触发时播放

        [SerializeField] private bool verboseLog = false; // 是否输出调试日志

        public bool IsInitialized { get; private set; } // 是否已初始化

        public float MasterVolume { get; private set; } = DefaultMasterVolume;
        public float BackgroundVolume { get; private set; } = DefaultBackgroundVolume;
        public float SfxVolume { get; private set; } = DefaultSfxVolume;

        // 实际背景音乐音量 = 总音量 * 背景音乐音量
        public float EffectiveBackgroundVolume => MasterVolume * BackgroundVolume;

        // 实际音效音量 = 总音量 * 总音效音量
        public float EffectiveSfxVolume => MasterVolume * SfxVolume;

        #region 生命周期
        private void Awake()
        {
            // 单例模式核心：如果已存在实例，销毁当前重复对象
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this; // 赋值单例实例
        }
        private void OnDestroy()
        {
            // 1.如果销毁的是当前单例，清空单例引用
            if (Instance == this)
            {
                Instance = null;
            }
        }
        private void OnEnable()
        {
            GameEvents.OnGameSessionStateChanged += HandleGameSessionStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameSessionStateChanged -= HandleGameSessionStateChanged;
        }
        #endregion

        #region 初始化
        public void Initialize()
        {
            // 内部只执行一次初始化
            if (IsInitialized)
            {
                return;
            }

            // 1.初始化背景音乐音源
            InitializeBackgroundAudioSource();

            // 2.从轻量级设置中读取音量
            LoadVolumeSettings();

            // 3.应用当前音量到背景音乐音源
            ApplyBackgroundVolume();

            // 4.标记初始化完成
            IsInitialized = true;

            if (verboseLog)
            {
                Debug.Log("[AudioService] 音频服务初始化完成。", this);
            }
        }

        // 初始化背景音乐音源
        private void InitializeBackgroundAudioSource()
        {
            // 如果没有手动拖入 AudioSource，就尝试从当前物体上获取
            if (backgroundAudioSource == null)
            {
                backgroundAudioSource = GetComponent<AudioSource>();
            }

            // 如果当前物体上也没有 AudioSource，就运行时自动添加一个
            if (backgroundAudioSource == null)
            {
                backgroundAudioSource = gameObject.AddComponent<AudioSource>();
            }

            // 背景音乐通常循环播放
            backgroundAudioSource.loop = true;
            backgroundAudioSource.playOnAwake = false;
        }

        // 读取本地音量设置
        private void LoadVolumeSettings()
        {
            PlayerPrefsSettingsRepository settingsRepository = SaveService.Instance != null
                ? SaveService.Instance.SettingsRepository
                : null;

            MasterVolume = settingsRepository != null ? settingsRepository.GetMasterVolume(DefaultMasterVolume) : DefaultMasterVolume;
            BackgroundVolume = settingsRepository != null ? settingsRepository.GetBackgroundVolume(DefaultBackgroundVolume) : DefaultBackgroundVolume;
            SfxVolume = settingsRepository != null ? settingsRepository.GetSfxVolume(DefaultSfxVolume) : DefaultSfxVolume;

            MasterVolume = Mathf.Clamp01(MasterVolume);
            BackgroundVolume = Mathf.Clamp01(BackgroundVolume);
            SfxVolume = Mathf.Clamp01(SfxVolume);
        }
        #endregion

        #region 音量控制
        public void SetMasterVolume(float volume)
        {
            // 设置总音量，并立即保存
            MasterVolume = Mathf.Clamp01(volume);
            SaveService.Instance?.SettingsRepository?.SetMasterVolume(MasterVolume);

            ApplyBackgroundVolume();
        }

        public void SetBackgroundVolume(float volume)
        {
            // 设置背景音乐音量，并立即保存
            BackgroundVolume = Mathf.Clamp01(volume);
            SaveService.Instance?.SettingsRepository?.SetBackgroundVolume(BackgroundVolume);

            ApplyBackgroundVolume();
        }

        public void SetSfxVolume(float volume)
        {
            // 设置总音效音量，并立即保存
            SfxVolume = Mathf.Clamp01(volume);
            SaveService.Instance?.SettingsRepository?.SetSfxVolume(SfxVolume);
        }

        // 应用背景音乐真实音量
        private void ApplyBackgroundVolume()
        {
            if (backgroundAudioSource == null)
            {
                return;
            }

            backgroundAudioSource.volume = EffectiveBackgroundVolume;
        }
        #endregion

        #region 背景音乐播放
        // 根据游戏会话状态切换背景音乐
        private void HandleGameSessionStateChanged(GameSessionState state)
        {
            switch (state)
            {
                case GameSessionState.InGame:
                    PlayGameplayBackgroundMusic();
                    break;

                case GameSessionState.InGameBattle:
                    PlayBattleBackgroundMusic();
                    break;

                case GameSessionState.OutGame:
                    PlayMenuBackgroundMusic();
                    break;
            }
        }
        // 播放战斗中背景音乐
        public void PlayBattleBackgroundMusic()
        {
            // 1.如果战斗音乐没有配置，先兜底播放普通游戏中背景音乐
            AudioClip targetClip = battleBackgroundClip != null ? battleBackgroundClip : gameplayBackgroundClip;

            // 2.播放目标背景音乐
            PlayBackgroundMusic(targetClip);
        }
        // 播放默认背景音乐：兼容旧调用，优先播放主界面音乐
        public void PlayBackgroundMusic()
        {
            AudioClip targetClip = menuBackgroundClip != null ? menuBackgroundClip : gameplayBackgroundClip;
            PlayBackgroundMusic(targetClip);
        }

        // 播放主界面 / 登录界面背景音乐
        public void PlayMenuBackgroundMusic()
        {
            PlayBackgroundMusic(menuBackgroundClip);
        }

        // 播放游戏中背景音乐
        public void PlayGameplayBackgroundMusic()
        {
            PlayBackgroundMusic(gameplayBackgroundClip);
        }

        public void PlayBackgroundMusic(AudioClip clip)
        {
            if (backgroundAudioSource == null || clip == null)
            {
                return;
            }

            // 如果正在播放同一首背景音乐，就只刷新音量
            if (backgroundAudioSource.clip == clip && backgroundAudioSource.isPlaying)
            {
                ApplyBackgroundVolume();
                return;
            }

            backgroundAudioSource.clip = clip;
            ApplyBackgroundVolume();
            backgroundAudioSource.Play();
        }

        public void PauseBackgroundMusic()
        {
            if (backgroundAudioSource == null)
            {
                return;
            }

            backgroundAudioSource.Pause();
        }

        public void ResumeBackgroundMusic()
        {
            if (backgroundAudioSource == null)
            {
                return;
            }

            // 当前没有任何背景音乐时，不自动猜测播放哪首音乐
            // 主菜单和游戏中应分别调用 PlayMenuBackgroundMusic / PlayGameplayBackgroundMusic
            if (backgroundAudioSource.clip == null)
            {
                return;
            }

            ApplyBackgroundVolume();
            backgroundAudioSource.UnPause();
        }

        public void StopBackgroundMusic()
        {
            if (backgroundAudioSource == null)
            {
                return;
            }

            backgroundAudioSource.Stop();
        }
        #endregion
    }
}
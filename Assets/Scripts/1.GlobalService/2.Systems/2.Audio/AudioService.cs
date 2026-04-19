using UnityEngine;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 音频服务：当前阶段先保留统一接口，内部实现使用空壳占位
    public class AudioService : MonoBehaviour
    {
        public static AudioService Instance { get; private set; } // 音频服务单例入口

        [Header("=== 占位配置 ===")]
        [SerializeField] private bool verboseLog = false; // 是否输出占位日志

        public bool IsInitialized { get; private set; } // 是否已初始化
        public float MasterVolume { get; private set; } = 1f; // 当前总音量缓存
        public float BackgroundVolume { get; private set; } = 1f; // 当前背景音量缓存

        #region 1. 初始化
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

        public void Initialize()
        {
            // 1.初始化阶段兜底赋值，避免外部过早调用导致单例为空
            if (Instance == null)
            {
                Instance = this;
            }

            // 2.允许外部重复调用，但内部只执行一次初始化
            if (IsInitialized)
            {
                return;
            }

            // 3.当前音频系统尚未接入真实播放逻辑，仅缓存状态供后续扩展复用
            MasterVolume = Mathf.Clamp01(MasterVolume);
            BackgroundVolume = Mathf.Clamp01(BackgroundVolume);

            // 4.标记初始化完成
            IsInitialized = true;

            if (verboseLog)
            {
                Debug.Log("[AudioService] 当前为音频空壳实现，仅保留接口与数据缓存。", this);
            }
        }
        #endregion

        #region 2. 音量控制
        public void SetMasterVolume(float volume)
        {
            // 当前阶段先只缓存总音量，不执行实际声音控制。
            MasterVolume = Mathf.Clamp01(volume);
        }

        public void SetBackgroundVolume(float volume)
        {
            // 当前阶段先只缓存背景音量，不执行实际声音控制。
            BackgroundVolume = Mathf.Clamp01(volume);
        }
        #endregion

        #region 3. 播放接口
        public void PlayBackgroundMusic()
        {
            // 背景音乐接口先保留为空壳，避免后续接入真实系统时改动调用层。
        }
        #endregion
    }
}

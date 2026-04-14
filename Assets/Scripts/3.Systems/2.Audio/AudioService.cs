using UnityEngine;

namespace WutheringWaves
{
    // 音频服务：连接新服务层与旧SoundManager的过渡桥接
    public class AudioService : MonoBehaviour
    {
        [SerializeField] private SoundManager legacySoundManager; // 旧音频管理器
        [SerializeField] private bool playBackgroundOnInitialize = false; // 初始化时是否自动播放背景音乐

        public bool IsInitialized { get; private set; } // 是否已初始化

        public void Initialize()
        {
            // 允许外部重复调用，但内部只执行一次真实初始化。
            if (IsInitialized)
            {
                return;
            }

            if (legacySoundManager == null)
            {
                Debug.LogWarning("[AudioService] Legacy SoundManager is not assigned. Audio bridge stays idle.", this);
            }
            else if (playBackgroundOnInitialize)
            {
                legacySoundManager.PlayBackGround();
            }

            IsInitialized = true;
        }

        public void SetMasterVolume(float volume)
        {
            // 总音量直接通过AudioListener统一控制。
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        public void SetBackgroundVolume(float volume)
        {
            // 背景音量仍复用旧系统能力，方便逐步迁移。
            if (legacySoundManager != null)
            {
                legacySoundManager.SetBackgroundVolume(volume);
            }
        }

        public void PlayBackgroundMusic()
        {
            // 对外提供统一命名入口，屏蔽旧方法名差异。
            legacySoundManager?.PlayBackGround();
        }
    }
}

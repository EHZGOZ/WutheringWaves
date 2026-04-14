using System;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 设置菜单控制器：管理设置面板、音量面板和对应UI事件
    public class SettingsMenuController : MonoBehaviour
    {
        private const string MasterVolumeKey = "Volume"; // 总音量存档键
        private const string BackgroundVolumeKey = "BackgroundVolume"; // 背景音量存档键

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel; // 设置主面板
        [SerializeField] private GameObject volumePanel; // 音量子面板

        [Header("Settings Buttons")]
        [SerializeField] private Button restartButton; // 重新开始按钮
        [SerializeField] private Button volumeButton; // 打开音量面板按钮
        [SerializeField] private Button quitButton; // 返回主菜单按钮

        [Header("Volume UI")]
        [SerializeField] private Slider masterVolumeSlider; // 总音量滑条
        [SerializeField] private Slider backgroundVolumeSlider; // 背景音量滑条
        [SerializeField] private Button backToSettingsButton; // 返回设置面板按钮

        private AudioService audioService; // 音频服务引用
        private Action onRestartRequested; // 重开场景回调
        private Action onReturnToMainRequested; // 返回主菜单回调
        private bool initialized; // 是否已完成初始化
        private bool listenersBound; // 是否已绑定UI事件

        public void ConfigureIfMissing(
            GameObject settings,
            GameObject volume,
            Button restart,
            Button volumeOpen,
            Button quit,
            Button backToSettings,
            Slider masterSlider,
            Slider backgroundSlider)
        {
            // 兼容旧场景接线：仅在空引用时补齐Inspector字段。
            if (settingsPanel == null) settingsPanel = settings;
            if (volumePanel == null) volumePanel = volume;
            if (restartButton == null) restartButton = restart;
            if (volumeButton == null) volumeButton = volumeOpen;
            if (quitButton == null) quitButton = quit;
            if (backToSettingsButton == null) backToSettingsButton = backToSettings;
            if (masterVolumeSlider == null) masterVolumeSlider = masterSlider;
            if (backgroundVolumeSlider == null) backgroundVolumeSlider = backgroundSlider;
        }

        public void Initialize(
            AudioService injectedAudioService,
            Action restartRequested,
            Action returnToMainRequested)
        {
            // 初始化时缓存依赖与回调，并同步一次音量面板状态。
            audioService = injectedAudioService;
            onRestartRequested = restartRequested;
            onReturnToMainRequested = returnToMainRequested;

            InitializeVolumeValues();
            initialized = true;
            BindListeners();
        }

        public void ShowSettings()
        {
            // 只显示设置面板，隐藏音量面板。
            SetPanelActive(settingsPanel, true);
            SetPanelActive(volumePanel, false);
        }

        public void ShowVolume()
        {
            // 只显示音量面板，隐藏设置主面板。
            SetPanelActive(settingsPanel, false);
            SetPanelActive(volumePanel, true);
        }

        public void CloseAll()
        {
            // 关闭当前设置体系下的所有面板。
            SetPanelActive(settingsPanel, false);
            SetPanelActive(volumePanel, false);
        }

        private void OnEnable()
        {
            if (initialized)
            {
                BindListeners();
            }
        }

        private void OnDisable()
        {
            UnbindListeners();
        }

        private void BindListeners()
        {
            // 避免重复注册，防止按钮和滑条事件叠加。
            if (listenersBound)
            {
                return;
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (volumeButton != null)
            {
                volumeButton.onClick.AddListener(HandleOpenVolumeClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(HandleQuitToMainClicked);
            }

            if (backToSettingsButton != null)
            {
                backToSettingsButton.onClick.AddListener(HandleBackToSettingsClicked);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
            }

            if (backgroundVolumeSlider != null)
            {
                backgroundVolumeSlider.onValueChanged.AddListener(HandleBackgroundVolumeChanged);
            }

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            // 失活时解绑，保证重新启用后事件干净可控。
            if (!listenersBound)
            {
                return;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (volumeButton != null)
            {
                volumeButton.onClick.RemoveListener(HandleOpenVolumeClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(HandleQuitToMainClicked);
            }

            if (backToSettingsButton != null)
            {
                backToSettingsButton.onClick.RemoveListener(HandleBackToSettingsClicked);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            }

            if (backgroundVolumeSlider != null)
            {
                backgroundVolumeSlider.onValueChanged.RemoveListener(HandleBackgroundVolumeChanged);
            }

            listenersBound = false;
        }

        private void InitializeVolumeValues()
        {
            // 初始化时先读取本地缓存，再同步到滑条和音频系统。
            float master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            float background = PlayerPrefs.GetFloat(BackgroundVolumeKey, 1f);

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(master);
            }

            if (backgroundVolumeSlider != null)
            {
                backgroundVolumeSlider.SetValueWithoutNotify(background);
            }

            ApplyMasterVolume(master);
            ApplyBackgroundVolume(background);
        }

        private void HandleRestartClicked()
        {
            onRestartRequested?.Invoke();
        }

        private void HandleOpenVolumeClicked()
        {
            ShowVolume();
        }

        private void HandleQuitToMainClicked()
        {
            onReturnToMainRequested?.Invoke();
        }

        private void HandleBackToSettingsClicked()
        {
            ShowSettings();
        }

        private void HandleMasterVolumeChanged(float value)
        {
            // 滑条变化时立即生效，并同步持久化到PlayerPrefs。
            float safeValue = Mathf.Clamp01(value);
            ApplyMasterVolume(safeValue);
            PlayerPrefs.SetFloat(MasterVolumeKey, safeValue);
            PlayerPrefs.Save();
        }

        private void HandleBackgroundVolumeChanged(float value)
        {
            // 背景音量单独控制，便于BGM与其他音效分离调节。
            float safeValue = Mathf.Clamp01(value);
            ApplyBackgroundVolume(safeValue);
            PlayerPrefs.SetFloat(BackgroundVolumeKey, safeValue);
            PlayerPrefs.Save();
        }

        private void ApplyMasterVolume(float value)
        {
            // 有音频服务时优先走服务层，否则直接回退到AudioListener。
            if (audioService != null)
            {
                audioService.SetMasterVolume(value);
            }
            else
            {
                AudioListener.volume = value;
            }
        }

        private void ApplyBackgroundVolume(float value)
        {
            // 背景音量优先交给新服务层；若未接入则回退旧系统。
            if (audioService != null)
            {
                audioService.SetBackgroundVolume(value);
                return;
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetBackgroundVolume(value);
            }
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            // 面板判空后再切换显隐，避免场景缺线时报错。
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}

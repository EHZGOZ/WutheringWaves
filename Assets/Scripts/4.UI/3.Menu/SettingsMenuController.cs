using System;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 设置菜单控制器：负责设置面板、音量面板以及对应按钮事件
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

        #region 1. 外部注入
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
            // 1. 缓存依赖与回调。
            audioService = injectedAudioService;
            onRestartRequested = restartRequested;
            onReturnToMainRequested = returnToMainRequested;

            // 2. 初始化音量显示与占位服务缓存。
            InitializeVolumeValues();

            // 3. 绑定UI事件。
            initialized = true;
            BindListeners();
        }
        #endregion

        #region 2. 面板切换
        public void ShowSettings()
        {
            // 仅显示设置面板，隐藏音量面板。
            SetPanelActive(settingsPanel, true);
            SetPanelActive(volumePanel, false);
        }

        public void ShowVolume()
        {
            // 仅显示音量面板，隐藏设置主面板。
            SetPanelActive(settingsPanel, false);
            SetPanelActive(volumePanel, true);
        }

        public void CloseAll()
        {
            // 关闭当前设置体系下的所有面板。
            SetPanelActive(settingsPanel, false);
            SetPanelActive(volumePanel, false);
        }
        #endregion

        #region 3. 生命周期
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
        #endregion

        #region 4. 事件绑定
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
        #endregion

        #region 5. 音量同步
        private void InitializeVolumeValues()
        {
            // 1. 读取本地缓存值。
            float master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            float background = PlayerPrefs.GetFloat(BackgroundVolumeKey, 1f);

            // 2. 同步到滑条显示。
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(master);
            }

            if (backgroundVolumeSlider != null)
            {
                backgroundVolumeSlider.SetValueWithoutNotify(background);
            }

            // 3. 同步到音频空壳服务缓存。
            ApplyMasterVolume(master);
            ApplyBackgroundVolume(background);
        }

        private void HandleMasterVolumeChanged(float value)
        {
            // 总音量变化时立即同步缓存，并持久化到本地。
            float safeValue = Mathf.Clamp01(value);
            ApplyMasterVolume(safeValue);
            PlayerPrefs.SetFloat(MasterVolumeKey, safeValue);
            PlayerPrefs.Save();
        }

        private void HandleBackgroundVolumeChanged(float value)
        {
            // 背景音量变化时立即同步缓存，并持久化到本地。
            float safeValue = Mathf.Clamp01(value);
            ApplyBackgroundVolume(safeValue);
            PlayerPrefs.SetFloat(BackgroundVolumeKey, safeValue);
            PlayerPrefs.Save();
        }

        private void ApplyMasterVolume(float value)
        {
            // 当前阶段音频系统为空壳，这里仅同步服务层缓存值。
            if (audioService != null)
            {
                audioService.SetMasterVolume(value);
            }
        }

        private void ApplyBackgroundVolume(float value)
        {
            // 当前阶段音频系统为空壳，这里仅同步服务层缓存值。
            if (audioService != null)
            {
                audioService.SetBackgroundVolume(value);
            }
        }
        #endregion

        #region 6. 按钮回调
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
        #endregion

        #region 7. 工具方法
        private static void SetPanelActive(GameObject panel, bool active)
        {
            // 面板判空后再切换显隐，避免场景缺线时报错。
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        #endregion
    }
}

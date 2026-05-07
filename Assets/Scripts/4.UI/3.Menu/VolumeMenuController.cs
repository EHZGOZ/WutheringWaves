using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    // 音量菜单控制器：负责显示并修改当前音量设置
    public class VolumeMenuController : MonoBehaviour
    {
        [Header("音量菜单面板")]
        [SerializeField] private GameObject volumePanel; // 音量菜单面板

        [Header("总音量")]
        [SerializeField] private Slider masterVolumeSlider; // 总音量滑条
        [SerializeField] private TextMeshProUGUI masterVolumeText; // 总音量百分比文本

        [Header("背景音乐")]
        [SerializeField] private Slider backgroundVolumeSlider; // 背景音乐滑条
        [SerializeField] private TextMeshProUGUI backgroundVolumeText; // 背景音乐百分比文本

        [Header("总音效")]
        [SerializeField] private Slider sfxVolumeSlider; // 总音效滑条
        [SerializeField] private TextMeshProUGUI sfxVolumeText; // 总音效百分比文本

        [Header("返回按钮")]
        [SerializeField] private Button backButton; // 返回设置菜单按钮

        private Action onBackRequested; // 返回设置菜单请求
        private bool initialized; // 是否已初始化
        private bool listenersBound; // 是否已绑定UI事件

        #region 生命周期
        private void OnEnable()
        {
            if (initialized)
            {
                BindListeners();
                RefreshView();
            }
        }

        private void OnDisable()
        {
            UnbindListeners();
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }
        #endregion

        #region 初始化
        // 初始化音量菜单：由UIRoot传入返回设置菜单的回调
        public void Initialize(Action backRequested)
        {
            // 1.缓存外部流程回调
            onBackRequested = backRequested;

            // 2.绑定按钮和滑条事件
            BindListeners();

            // 3.刷新当前音量显示
            RefreshView();

            // 4.启动时隐藏音量菜单
            SetVisible(false);

            // 5.标记初始化完成
            initialized = true;
        }
        #endregion

        #region UI事件绑定
        // 绑定UI事件
        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
            }

            if (backgroundVolumeSlider != null)
            {
                backgroundVolumeSlider.onValueChanged.AddListener(HandleBackgroundVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBackClicked);
            }

            listenersBound = true;
        }

        // 解绑UI事件
        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            }

            if (backgroundVolumeSlider != null)
            {
                backgroundVolumeSlider.onValueChanged.RemoveListener(HandleBackgroundVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackClicked);
            }

            listenersBound = false;
        }
        #endregion

        #region 按钮与滑条回调
        // 修改总音量
        private void HandleMasterVolumeChanged(float value)
        {
            value = Mathf.Clamp01(value);

            AudioService.Instance?.SetMasterVolume(value);
            RefreshMasterVolume(value);
        }

        // 修改背景音乐音量
        private void HandleBackgroundVolumeChanged(float value)
        {
            value = Mathf.Clamp01(value);

            AudioService.Instance?.SetBackgroundVolume(value);
            RefreshBackgroundVolume(value);
        }

        // 修改总音效音量
        private void HandleSfxVolumeChanged(float value)
        {
            value = Mathf.Clamp01(value);

            AudioService.Instance?.SetSfxVolume(value);
            RefreshSfxVolume(value);
        }

        // 点击返回按钮
        private void HandleBackClicked()
        {
            onBackRequested?.Invoke();
        }

        #endregion

        #region 刷新显示
        // 刷新整个音量面板显示
        private void RefreshView()
        {
            if (AudioService.Instance == null)
            {
                RefreshMasterVolume(1f);
                RefreshBackgroundVolume(1f);
                RefreshSfxVolume(1f);
                return;
            }

            RefreshMasterVolume(AudioService.Instance.MasterVolume);
            RefreshBackgroundVolume(AudioService.Instance.BackgroundVolume);
            RefreshSfxVolume(AudioService.Instance.SfxVolume);
        }

        // 刷新总音量显示
        private void RefreshMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(volume);
            }

            if (masterVolumeText != null)
            {
                masterVolumeText.text = FormatPercent(volume);
            }
        }

        // 刷新背景音乐音量显示
        private void RefreshBackgroundVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (backgroundVolumeSlider != null)
            {
                backgroundVolumeSlider.SetValueWithoutNotify(volume);
            }

            if (backgroundVolumeText != null)
            {
                backgroundVolumeText.text = FormatPercent(volume);
            }
        }

        // 刷新总音效音量显示
        private void RefreshSfxVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(volume);
            }

            if (sfxVolumeText != null)
            {
                sfxVolumeText.text = FormatPercent(volume);
            }
        }

        // 格式化百分比文本
        private string FormatPercent(float value)
        {
            return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }
        #endregion

        // 设置音量菜单显隐
        public void SetVisible(bool visible)
        {
            if (volumePanel != null)
            {
                volumePanel.SetActive(visible);
            }

            if (visible)
            {
                RefreshView();
            }
        }


    }
}

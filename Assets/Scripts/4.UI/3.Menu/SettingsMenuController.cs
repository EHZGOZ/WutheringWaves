using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    // 设置菜单控制器：负责设置界面的左侧分类切换、声音设置和画质设置
    public class SettingsMenuController : MonoBehaviour
    {
        private enum SettingsPage
        {
            Audio,
            Graphics
        }

        [Header("设置菜单面板")]
        [SerializeField] private GameObject settingsPanel; // 设置主面板

        [Header("左侧分类按钮")]
        [SerializeField] private Button audioTabButton; // 声音按钮
        [SerializeField] private Button graphicsTabButton; // 画质按钮

        [Header("右侧内容面板")]
        [SerializeField] private GameObject audioPagePanel; // 声音设置页
        [SerializeField] private GameObject graphicsPagePanel; // 画质设置页

        [Header("声音页控件")]
        [SerializeField] private Slider masterVolumeSlider; // 主音量滑条
        [SerializeField] private TextMeshProUGUI masterVolumeValueText; // 主音量数值文本
        [SerializeField] private Slider musicVolumeSlider; // 音乐音量滑条
        [SerializeField] private TextMeshProUGUI musicVolumeValueText; // 音乐音量数值文本
        [SerializeField] private Slider sfxVolumeSlider; // 音效音量滑条
        [SerializeField] private TextMeshProUGUI sfxVolumeValueText; // 音效音量数值文本

        [Header("画质页控件")]
        [SerializeField] private TMP_Dropdown resolutionDropdown; // 分辨率下拉框
        [SerializeField] private TMP_Dropdown displayModeDropdown; // 显示模式下拉框
        [SerializeField] private TMP_Dropdown frameRateDropdown; // 帧率上限下拉框

        [Header("返回按钮")]
        [SerializeField] private Button returnButton; // 返回按钮

        private Action onReturnRequested; // 返回请求

        private readonly List<Resolution> availableResolutions = new List<Resolution>(); // 可用分辨率列表
        private readonly int[] frameRateOptions = { -1, 30, 60, 90, 120, 144 }; // 帧率选项，-1代表不限制

        private SettingsPage currentPage = SettingsPage.Audio; // 当前页面
        private bool initialized; // 是否已初始化
        private bool listenersBound; // 是否已绑定事件
        private bool viewOptionsInitialized; // 下拉框选项是否已初始化

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
        // 初始化设置菜单：由UIRoot传入返回逻辑
        public void Initialize(Action returnRequested)
        {
            // 1.缓存返回回调
            onReturnRequested = returnRequested;

            // 2.初始化下拉框选项
            InitializeViewOptions();

            // 3.绑定按钮和控件事件
            BindListeners();

            // 4.默认显示声音页
            SelectPage(SettingsPage.Audio);

            // 5.启动时默认隐藏设置面板
            SetVisible(false);

            // 6.标记初始化完成
            initialized = true;
        }
        #endregion

        #region 事件绑定
        // 绑定所有按钮和控件事件
        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (audioTabButton != null)
            {
                audioTabButton.onClick.AddListener(HandleAudioTabClicked);
            }

            if (graphicsTabButton != null)
            {
                graphicsTabButton.onClick.AddListener(HandleGraphicsTabClicked);
            }

            if (returnButton != null)
            {
                returnButton.onClick.AddListener(HandleReturnClicked);
            }

            BindAudioListeners();
            BindGraphicsListeners();

            listenersBound = true;
        }

        // 解绑所有按钮和控件事件
        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            if (audioTabButton != null)
            {
                audioTabButton.onClick.RemoveListener(HandleAudioTabClicked);
            }

            if (graphicsTabButton != null)
            {
                graphicsTabButton.onClick.RemoveListener(HandleGraphicsTabClicked);
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(HandleReturnClicked);
            }

            UnbindAudioListeners();
            UnbindGraphicsListeners();

            listenersBound = false;
        }

        // 绑定声音页事件
        private void BindAudioListeners()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            }
        }

        // 解绑声音页事件
        private void UnbindAudioListeners()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            }
        }

        // 绑定画质页事件
        private void BindGraphicsListeners()
        {
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged);
            }

            if (displayModeDropdown != null)
            {
                displayModeDropdown.onValueChanged.AddListener(HandleDisplayModeChanged);
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.onValueChanged.AddListener(HandleFrameRateChanged);
            }
        }

        // 解绑画质页事件
        private void UnbindGraphicsListeners()
        {
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);
            }

            if (displayModeDropdown != null)
            {
                displayModeDropdown.onValueChanged.RemoveListener(HandleDisplayModeChanged);
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.onValueChanged.RemoveListener(HandleFrameRateChanged);
            }
        }
        #endregion

        #region 按钮事件
        // 点击声音按钮
        private void HandleAudioTabClicked()
        {
            SelectPage(SettingsPage.Audio);
        }

        // 点击画质按钮
        private void HandleGraphicsTabClicked()
        {
            SelectPage(SettingsPage.Graphics);
        }

        // 点击返回按钮
        private void HandleReturnClicked()
        {
            onReturnRequested?.Invoke();
        }
        #endregion

        #region 声音设置
        // 修改主音量
        private void HandleMasterVolumeChanged(float value)
        {
            value = Mathf.Clamp01(value);

            AudioService.Instance?.SetMasterVolume(value);
            RefreshMasterVolume(value);
        }

        // 修改音乐音量
        private void HandleMusicVolumeChanged(float value)
        {
            value = Mathf.Clamp01(value);

            AudioService.Instance?.SetBackgroundVolume(value);
            RefreshMusicVolume(value);
        }

        // 修改音效音量
        private void HandleSfxVolumeChanged(float value)
        {
            value = Mathf.Clamp01(value);

            AudioService.Instance?.SetSfxVolume(value);
            RefreshSfxVolume(value);
        }

        // 刷新声音页显示
        private void RefreshAudioView()
        {
            if (AudioService.Instance == null)
            {
                RefreshMasterVolume(1f);
                RefreshMusicVolume(1f);
                RefreshSfxVolume(1f);
                return;
            }

            RefreshMasterVolume(AudioService.Instance.MasterVolume);
            RefreshMusicVolume(AudioService.Instance.BackgroundVolume);
            RefreshSfxVolume(AudioService.Instance.SfxVolume);
        }

        // 刷新主音量滑条和数值
        private void RefreshMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(volume);
            }

            if (masterVolumeValueText != null)
            {
                masterVolumeValueText.text = FormatVolumeValue(volume);
            }
        }

        // 刷新音乐音量滑条和数值
        private void RefreshMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(volume);
            }

            if (musicVolumeValueText != null)
            {
                musicVolumeValueText.text = FormatVolumeValue(volume);
            }
        }

        // 刷新音效音量滑条和数值
        private void RefreshSfxVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(volume);
            }

            if (sfxVolumeValueText != null)
            {
                sfxVolumeValueText.text = FormatVolumeValue(volume);
            }
        }
        #endregion

        #region 画质设置
        // 初始化画质下拉框选项
        private void InitializeViewOptions()
        {
            if (viewOptionsInitialized)
            {
                return;
            }

            InitializeResolutionOptions();
            InitializeDisplayModeOptions();
            InitializeFrameRateOptions();

            viewOptionsInitialized = true;
        }

        // 初始化分辨率选项
        // 作用：读取当前设备支持的分辨率，把它们转换成下拉框选项，例如 "1920 x 1080"
        private void InitializeResolutionOptions()
        {
            // 1.先清空缓存的分辨率列表
            // availableResolutions 保存的是程序真正要用的 Resolution 数据
            // 下拉框显示的是文字，但真正设置分辨率时需要从这里取宽度和高度
            availableResolutions.Clear();

            // 2.如果分辨率下拉框没有在Inspector里绑定，就直接返回
            // 这样可以避免 resolutionDropdown.ClearOptions() 空引用报错
            if (resolutionDropdown == null)
            {
                return;
            }

            // 3.清空下拉框中旧的选项
            // 避免重复初始化时出现多个相同的分辨率选项
            resolutionDropdown.ClearOptions();

            // 4.创建一个文字列表，用来存放显示给玩家看的分辨率文本
            // 例如：1280 x 720、1920 x 1080、2560 x 1440
            List<string> optionNames = new List<string>();

            // 5.从Unity获取当前设备支持的所有分辨率
            // Screen.resolutions 通常会返回显示器支持的分辨率和刷新率组合
            Resolution[] resolutions = Screen.resolutions;

            // 6.遍历系统返回的所有分辨率
            for (int i = 0; i < resolutions.Length; i++)
            {
                // 7.取出当前遍历到的分辨率
                Resolution resolution = resolutions[i];

                // 8.只保留不同宽高，避免同分辨率不同刷新率重复出现
                // 例如系统可能返回：
                // 1920 x 1080 60Hz
                // 1920 x 1080 120Hz
                // 1920 x 1080 144Hz
                // 但你的画质设置当前只做分辨率，不做刷新率，所以这里只显示一次 1920 x 1080
                if (ContainsResolution(resolution.width, resolution.height))
                {
                    continue;
                }

                // 9.把真正的分辨率数据保存起来
                // 后面玩家选择下拉框时，会通过索引从 availableResolutions 里取出对应 Resolution
                availableResolutions.Add(resolution);

                // 10.把分辨率转换成玩家能看懂的文字，并加入下拉框文字列表
                optionNames.Add(resolution.width + " x " + resolution.height);
            }

            // 11.兜底处理：某些平台或特殊运行环境可能拿不到 Screen.resolutions
            // 如果没有拿到任何分辨率，就使用当前屏幕分辨率作为唯一选项
            if (availableResolutions.Count == 0)
            {
                Resolution currentResolution = Screen.currentResolution;

                // 12.保存当前分辨率数据
                availableResolutions.Add(currentResolution);

                // 13.添加当前分辨率显示文本
                optionNames.Add(currentResolution.width + " x " + currentResolution.height);
            }

            // 14.把最终整理好的文字列表添加到分辨率下拉框
            resolutionDropdown.AddOptions(optionNames);
        }

        // 初始化显示模式选项
        // 作用：给显示模式下拉框添加固定的三种显示模式
        private void InitializeDisplayModeOptions()
        {
            // 1.如果显示模式下拉框没有在Inspector里绑定，就直接返回
            if (displayModeDropdown == null)
            {
                return;
            }

            // 2.清空下拉框中旧的显示模式选项
            // 避免重复初始化时选项重复
            displayModeDropdown.ClearOptions();

            // 3.添加显示模式选项
            // 注意：这里的顺序必须和 ConvertIndexToDisplayMode() 里的索引对应
            // 第0项：窗口模式       -> FullScreenMode.Windowed
            // 第1项：无边框全屏     -> FullScreenMode.FullScreenWindow
            // 第2项：独占全屏       -> FullScreenMode.ExclusiveFullScreen
            displayModeDropdown.AddOptions(new List<string>
    {
        "窗口模式",
        "无边框全屏",
        "独占全屏"
    });
        }

        // 初始化帧率上限选项
        // 作用：把 frameRateOptions 数组里的帧率配置转换成下拉框选项
        private void InitializeFrameRateOptions()
        {
            // 1.如果帧率下拉框没有在Inspector里绑定，就直接返回
            if (frameRateDropdown == null)
            {
                return;
            }

            // 2.清空下拉框中旧的帧率选项
            // 避免重复初始化时选项重复
            frameRateDropdown.ClearOptions();

            // 3.创建一个文字列表，用来存放显示给玩家看的帧率文本
            List<string> optionNames = new List<string>();

            // 4.遍历帧率配置数组
            // frameRateOptions 一般类似：{ -1, 30, 60, 90, 120, 144 }
            for (int i = 0; i < frameRateOptions.Length; i++)
            {
                // 5.取出当前帧率配置
                int frameRate = frameRateOptions[i];

                // 6.把帧率数字转换成下拉框显示文字
                // 在Unity里，Application.targetFrameRate = -1 通常表示不限制帧率
                // 所以 -1 显示为“不限制”，其他数字直接显示为 30、60、120 等
                optionNames.Add(frameRate < 0 ? "不限制" : frameRate.ToString());
            }

            // 7.把最终整理好的帧率文字列表添加到下拉框
            frameRateDropdown.AddOptions(optionNames);
        }
        // 修改分辨率
        // 参数 optionIndex：玩家在“分辨率下拉框”中选择的选项索引
        private void HandleResolutionChanged(int optionIndex)
        {
            // 1.索引安全检查
            // optionIndex 是下拉框传进来的选项编号，例如：
            // 第0项 -> optionIndex = 0
            // 第1项 -> optionIndex = 1
            // 如果索引小于0，或者超过 availableResolutions 的范围，说明这个选择无效
            if (optionIndex < 0 || optionIndex >= availableResolutions.Count)
            {
                return;
            }

            // 2.根据下拉框索引，从分辨率缓存列表中取出真正的分辨率数据
            // 下拉框里显示的是文字，例如 "1920 x 1080"
            // availableResolutions 里保存的是 Unity 的 Resolution 数据，里面有 width 和 height
            Resolution resolution = availableResolutions[optionIndex];

            // 3.应用新的分辨率
            // resolution.width：目标宽度，例如 1920
            // resolution.height：目标高度，例如 1080
            // Screen.fullScreenMode：保留当前显示模式，不因为改分辨率而改变窗口/全屏状态
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
        }

        // 修改显示模式
        // 参数 optionIndex：玩家在“显示模式下拉框”中选择的选项索引
        private void HandleDisplayModeChanged(int optionIndex)
        {
            // 1.把下拉框索引转换成 Unity 能识别的 FullScreenMode
            // 例如：
            // 0 -> 窗口模式
            // 1 -> 无边框全屏
            // 2 -> 独占全屏
            FullScreenMode displayMode = ConvertIndexToDisplayMode(optionIndex);

            // 2.先记录当前屏幕显示模式
            // 这样 Unity 的 Screen.fullScreenMode 会同步为玩家选择的新模式
            Screen.fullScreenMode = displayMode;

            // 3.应用新的显示模式
            // Screen.width 和 Screen.height 表示当前正在使用的分辨率
            // 这里不改变分辨率，只改变显示模式
            // 例如当前是 1920 x 1080，玩家从窗口模式切到无边框全屏后，仍然保持 1920 x 1080
            Screen.SetResolution(Screen.width, Screen.height, displayMode);
        }

        // 修改帧率上限
        // 参数 optionIndex：玩家在“帧率上限下拉框”中选择的选项索引
        private void HandleFrameRateChanged(int optionIndex)
        {
            // 1.索引安全检查
            // 防止下拉框传入不存在的选项编号，避免数组越界
            if (optionIndex < 0 || optionIndex >= frameRateOptions.Length)
            {
                return;
            }

            // 2.根据下拉框索引，从帧率配置数组中取出目标帧率
            // 例如 frameRateOptions = { -1, 30, 60, 90, 120, 144 }
            // optionIndex = 0 -> -1，表示不限制帧率
            // optionIndex = 2 -> 60，表示限制为 60 FPS
            int targetFrameRate = frameRateOptions[optionIndex];

            // 3.应用帧率上限
            // Application.targetFrameRate 是 Unity 提供的目标帧率设置
            // -1 通常表示不限制帧率
            // 30 表示尽量限制在 30 FPS
            // 60 表示尽量限制在 60 FPS
            Application.targetFrameRate = targetFrameRate;
        }

        // 刷新画质页显示
        private void RefreshGraphicsView()
        {
            RefreshResolutionView();
            RefreshDisplayModeView();
            RefreshFrameRateView();
        }

        // 刷新分辨率下拉框显示
        // 作用：打开设置界面或刷新设置界面时，让分辨率下拉框显示当前游戏正在使用的分辨率
        private void RefreshResolutionView()
        {
            // 1.如果分辨率下拉框没有在Inspector里绑定，直接返回
            // 避免后面访问 resolutionDropdown 时出现空引用错误
            if (resolutionDropdown == null)
            {
                return;
            }

            // 2.默认选中第0项
            // 如果当前分辨率没有在 availableResolutions 中找到，就至少保证下拉框有一个合法显示
            int currentIndex = 0;

            // 3.遍历已经缓存好的分辨率列表
            // availableResolutions 是 InitializeResolutionOptions() 初始化出来的分辨率数据
            for (int i = 0; i < availableResolutions.Count; i++)
            {
                // 4.取出当前遍历到的分辨率
                Resolution resolution = availableResolutions[i];

                // 5.用当前游戏窗口的宽高和缓存分辨率进行比较
                // Screen.width：当前游戏画面的宽度
                // Screen.height：当前游戏画面的高度
                if (resolution.width == Screen.width && resolution.height == Screen.height)
                {
                    // 6.找到了当前分辨率对应的下拉框索引
                    currentIndex = i;

                    // 7.已经找到目标项，不需要继续遍历
                    break;
                }
            }

            // 8.刷新下拉框显示，但不触发 onValueChanged 事件
            // 这里是“同步UI显示”，不是“玩家主动修改设置”
            // 如果用 resolutionDropdown.value = currentIndex，可能会触发 HandleResolutionChanged()
            resolutionDropdown.SetValueWithoutNotify(currentIndex);
        }

        // 刷新显示模式下拉框显示
        // 作用：打开设置界面或刷新设置界面时，让显示模式下拉框显示当前游戏正在使用的显示模式
        private void RefreshDisplayModeView()
        {
            // 1.如果显示模式下拉框没有在Inspector里绑定，直接返回
            if (displayModeDropdown == null)
            {
                return;
            }

            // 2.读取当前 Unity 的显示模式
            // Screen.fullScreenMode 可能是：
            // FullScreenMode.Windowed
            // FullScreenMode.FullScreenWindow
            // FullScreenMode.ExclusiveFullScreen
            FullScreenMode currentDisplayMode = Screen.fullScreenMode;

            // 3.把 Unity 的 FullScreenMode 转换成下拉框索引
            // 例如：
            // Windowed -> 0
            // FullScreenWindow -> 1
            // ExclusiveFullScreen -> 2
            int currentIndex = ConvertDisplayModeToIndex(currentDisplayMode);

            // 4.刷新下拉框显示，但不触发 onValueChanged 事件
            // 避免只是刷新UI时，又重复调用 HandleDisplayModeChanged()
            displayModeDropdown.SetValueWithoutNotify(currentIndex);
        }

        // 刷新帧率上限下拉框显示
        // 作用：打开设置界面或刷新设置界面时，让帧率下拉框显示当前游戏正在使用的帧率上限
        private void RefreshFrameRateView()
        {
            // 1.如果帧率上限下拉框没有在Inspector里绑定，直接返回
            if (frameRateDropdown == null)
            {
                return;
            }

            // 2.读取当前 Unity 的目标帧率
            // Application.targetFrameRate = -1 通常表示不限制帧率
            // Application.targetFrameRate = 60 表示目标帧率为 60 FPS
            int currentFrameRate = Application.targetFrameRate;

            // 3.默认选中第0项
            // 一般你的 frameRateOptions 第0项是 -1，也就是“不限制”
            int currentIndex = 0;

            // 4.遍历帧率配置数组，查找当前帧率对应的下拉框索引
            for (int i = 0; i < frameRateOptions.Length; i++)
            {
                // 5.如果配置中的帧率等于当前 Unity 正在使用的帧率，就说明找到了对应选项
                if (frameRateOptions[i] == currentFrameRate)
                {
                    // 6.记录当前帧率对应的下拉框索引
                    currentIndex = i;

                    // 7.已经找到目标项，不需要继续遍历
                    break;
                }
            }

            // 8.刷新下拉框显示，但不触发 onValueChanged 事件
            // 这里是“让UI显示真实状态”，不是“玩家修改帧率”
            // 避免刷新UI时重复调用 HandleFrameRateChanged()
            frameRateDropdown.SetValueWithoutNotify(currentIndex);
        }

        // 判断分辨率列表里是否已有相同宽高
        private bool ContainsResolution(int width, int height)
        {
            for (int i = 0; i < availableResolutions.Count; i++)
            {
                Resolution resolution = availableResolutions[i];
                if (resolution.width == width && resolution.height == height)
                {
                    return true;
                }
            }

            return false;
        }

        // 把下拉框索引转换成Unity显示模式
        private FullScreenMode ConvertIndexToDisplayMode(int optionIndex)
        {
            switch (optionIndex)
            {
                case 0:
                    return FullScreenMode.Windowed;

                case 1:
                    return FullScreenMode.FullScreenWindow;

                case 2:
                    return FullScreenMode.ExclusiveFullScreen;

                default:
                    return Screen.fullScreenMode;
            }
        }

        // 把Unity显示模式转换成下拉框索引
        private int ConvertDisplayModeToIndex(FullScreenMode displayMode)
        {
            switch (displayMode)
            {
                case FullScreenMode.Windowed:
                    return 0;

                case FullScreenMode.FullScreenWindow:
                    return 1;

                case FullScreenMode.ExclusiveFullScreen:
                    return 2;

                default:
                    return 1;
            }
        }
        #endregion

        #region 页面切换
        // 切换设置子页面
        private void SelectPage(SettingsPage page)
        {
            currentPage = page;

            bool showAudioPage = currentPage == SettingsPage.Audio;
            bool showGraphicsPage = currentPage == SettingsPage.Graphics;

            if (audioPagePanel != null)
            {
                audioPagePanel.SetActive(showAudioPage);
            }

            if (graphicsPagePanel != null)
            {
                graphicsPagePanel.SetActive(showGraphicsPage);
            }

            // 选中的按钮暂时不可点，用于表现当前页面状态
            if (audioTabButton != null)
            {
                audioTabButton.interactable = !showAudioPage;
            }

            if (graphicsTabButton != null)
            {
                graphicsTabButton.interactable = !showGraphicsPage;
            }

            RefreshView();
        }

        // 刷新当前设置界面显示
        private void RefreshView()
        {
            InitializeViewOptions();

            RefreshAudioView();
            RefreshGraphicsView();
        }
        #endregion

        #region 外部调用
        // 设置设置菜单显隐
        public void SetVisible(bool visible)
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(visible);
            }

            if (visible)
            {
                SelectPage(currentPage);
            }
        }
        #endregion

        #region 工具方法
        // 格式化音量数值：0-1转换为0-100
        private string FormatVolumeValue(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f).ToString();
        }
        #endregion
    }
}
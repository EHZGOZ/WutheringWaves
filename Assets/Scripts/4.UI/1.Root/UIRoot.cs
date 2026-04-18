using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // UI根控制器：统一调度主菜单、设置菜单、HUD和小地图的生命周期
    public class UIRoot : MonoBehaviour
    {
        #region 1. 单例入口
        public static UIRoot Instance { get; private set; } // UI全局访问入口
        #endregion

        #region 2. 控制器引用
        [Header("=== UI控制器引用（优先手动拖入，缺失时自动查找） ===")]
        [Tooltip("主菜单控制器")]
        [SerializeField] private MainMenuController mainMenuController;
        [Tooltip("设置菜单控制器")]
        [SerializeField] private SettingsMenuController settingsMenuController;
        [Tooltip("角色HUD控制器")]
        [SerializeField] private CharacterHUDController characterHUDController;
        [Tooltip("小地图控制器")]
        [SerializeField] private MiniMapController miniMapController;
        #endregion

        #region 3. 注入依赖
        [Header("=== 运行时注入依赖 ===")]
        [Tooltip("当前绑定的角色门面")]
        [SerializeField] private CharacterFacade characterFacade;
        [Tooltip("当前玩家控制器")]
        [SerializeField] private PlayerController playerController;
        [Tooltip("缺少小地图控制器时，是否自动查找")]
        [SerializeField] private bool autoCreateMiniMap = true;
        [Tooltip("时间服务")]
        [SerializeField] private GameTimeService gameTimeService;
        [Tooltip("音频服务")]
        [SerializeField] private AudioService audioService;
        #endregion

        #region 4. 旧面板兼容引用
        [Header("=== 旧UI面板兼容引用（可选） ===")]
        [SerializeField] private GameObject MainPanel;
        [SerializeField] private GameObject SettingsPanel;
        [SerializeField] private GameObject VolumePanel;
        [SerializeField] private GameObject HUDPanel;

        [Header("=== 旧主菜单控件（可选） ===")]
        [SerializeField] private Button mainStartButton;
        [SerializeField] private Button mainVolumeButton;
        [SerializeField] private Button mainQuitButton;

        [Header("=== 旧设置菜单控件（可选） ===")]
        [SerializeField] private Button settingsRestartButton;
        [SerializeField] private Button settingsVolumeButton;
        [SerializeField] private Button settingsQuitButton;

        [Header("=== 旧音量面板控件（可选） ===")]
        [SerializeField] private Slider volumeAllVolumeSlider;
        [SerializeField] private Slider volumeBackgroundMusicSlider;
        [SerializeField] private Button volumeToSettingsButton;

        [Header("=== 旧HUD控件（可选） ===")]
        [SerializeField] private GameObject PlayerHealthbar;
        [SerializeField] private GameObject PlayerStaminabar;
        [SerializeField] private GameObject HealthPotionUI;
        [SerializeField] private GameObject EnemyHealthSlider;
        [SerializeField] private GameObject EnemyHealthText;
        [SerializeField] private GameObject EnemyStunBar;
        #endregion

        #region 5. 对外只读属性
        public CharacterFacade Facade => characterFacade; // 当前角色门面
        public AudioService AudioService => audioService; // 当前音频服务
        #endregion

        #region 6. 生命周期
        private void Awake()
        {
            // 1.保持单例，避免场景里出现多个UIRoot争抢状态
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 2.缓存单例引用
            Instance = this;

            // 3.先解析依赖和子控制器，保证Start阶段可以直接初始化
            ResolveDependencies();
            ResolveControllers();
            WireLegacyReferencesIfNeeded();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            InitializeAndApplyLifecycle();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region 7. 注入入口
        // 注入当前角色门面：作为UI层统一的角色入口
        public void InjectCharacterFacade(CharacterFacade facade)
        {
            if (facade == null)
            {
                return;
            }

            characterFacade = facade;
            characterHUDController?.SetCharacterFacade(characterFacade);
            miniMapController?.InjectDependencies(characterFacade);
        }

        // 注入玩家控制器：由PlayerController在角色绑定完成后主动回写
        public void InjectPlayerController(PlayerController controller)
        {
            if (controller == null)
            {
                return;
            }

            playerController = controller;
            InjectCharacterFacade(controller.CurrentCharacterFacade);
        }
        #endregion

        #region 8. 菜单生命周期
        public void OpenMainMenu()
        {
            // 1.打开主菜单前，先关闭其他菜单
            settingsMenuController?.CloseAll();
            // 2.显示主菜单和HUD
            mainMenuController?.SetVisible(true);
            characterHUDController?.SetVisible(true);
            // 3.暂停游戏并禁用角色输入
            SetPauseAndCursor(true);
            SetCharacterInputEnabled(false);
        }

        public void OpenSettingsMenu()
        {
            // 1.关闭主菜单显示
            mainMenuController?.SetVisible(false);
            // 2.切换到设置菜单
            settingsMenuController?.ShowSettings();
            // 3.暂停游戏并禁用角色输入
            SetPauseAndCursor(true);
            SetCharacterInputEnabled(false);
        }

        public void OpenVolumeMenu()
        {
            // 1.关闭主菜单显示
            mainMenuController?.SetVisible(false);
            // 2.切换到音量菜单
            settingsMenuController?.ShowVolume();
            // 3.暂停游戏并禁用角色输入
            SetPauseAndCursor(true);
            SetCharacterInputEnabled(false);
        }

        public void CloseMenusAndResumeGameplay()
        {
            // 1.关闭主菜单和设置菜单
            mainMenuController?.SetVisible(false);
            settingsMenuController?.CloseAll();
            // 2.恢复时间流动、鼠标锁定和角色输入
            SetPauseAndCursor(false);
            SetCharacterInputEnabled(true);
        }
        #endregion

        #region 9. 场景与控制器初始化
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 1.场景切换后重新解析依赖
            ResolveDependencies();
            // 2.重新抓取各UI控制器
            ResolveControllers();
            // 3.补齐旧版面板和控件引用
            WireLegacyReferencesIfNeeded();
            // 4.重新应用UI生命周期
            InitializeAndApplyLifecycle();
        }

        private void InitializeAndApplyLifecycle()
        {
            // 1.初始化主菜单控制器
            if (mainMenuController != null)
            {
                mainMenuController.Initialize(
                    HandleStartGameRequested,
                    HandleOpenVolumeRequested,
                    HandleQuitRequested);
            }

            // 2.初始化设置菜单控制器
            if (settingsMenuController != null)
            {
                settingsMenuController.Initialize(
                    audioService,
                    HandleRestartSceneRequested,
                    HandleReturnToMainMenuRequested);
            }

            // 3.初始化角色HUD控制器
            if (characterHUDController != null)
            {
                characterHUDController.Initialize(characterFacade, this);
            }

            // 4.按需自动查找小地图控制器
            if (autoCreateMiniMap && miniMapController == null)
            {
                miniMapController = FindObjectOfType<MiniMapController>(true);
            }

            // 5.初始化小地图控制器
            if (miniMapController != null)
            {
                miniMapController.Initialize(characterFacade);
            }

            // 6.默认进入主菜单
            OpenMainMenu();
        }
        #endregion

        #region 10. UI回调
        private void HandleStartGameRequested()
        {
            CloseMenusAndResumeGameplay();
        }

        private void HandleOpenVolumeRequested()
        {
            OpenVolumeMenu();
        }

        private void HandleRestartSceneRequested()
        {
            SetPauseAndCursor(false);
            SetCharacterInputEnabled(true);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void HandleReturnToMainMenuRequested()
        {
            OpenMainMenu();
        }

        private void HandleQuitRequested()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        #endregion

        #region 11. 依赖解析
        private void ResolveDependencies()
        {
            // 1.先解析时间服务
            ResolveGameTimeService();

            // 2.解析音频服务
            if (audioService == null)
            {
                audioService = FindObjectOfType<AudioService>(true);
            }

            // 3.解析玩家控制器
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>(true);
            }

            // 4.优先从玩家控制器同步当前角色门面
            if (characterFacade == null && playerController != null)
            {
                characterFacade = playerController.CurrentCharacterFacade;
            }

            // 5.兜底直接查找角色门面
            if (characterFacade == null)
            {
                characterFacade = FindObjectOfType<CharacterFacade>(true);
            }
        }

        private void ResolveControllers()
        {
            // 1.主菜单控制器：优先Inspector，其次子节点，最后自动补组件
            if (mainMenuController == null)
            {
                mainMenuController = GetComponentInChildren<MainMenuController>(true);
            }

            if (mainMenuController == null)
            {
                mainMenuController = GetOrAddComponent<MainMenuController>(gameObject);
            }

            // 2.设置菜单控制器：优先Inspector，其次子节点，最后自动补组件
            if (settingsMenuController == null)
            {
                settingsMenuController = GetComponentInChildren<SettingsMenuController>(true);
            }

            if (settingsMenuController == null)
            {
                settingsMenuController = GetOrAddComponent<SettingsMenuController>(gameObject);
            }

            // 3.HUD控制器：优先Inspector，其次子节点，最后自动补组件
            if (characterHUDController == null)
            {
                characterHUDController = GetComponentInChildren<CharacterHUDController>(true);
            }

            if (characterHUDController == null)
            {
                characterHUDController = GetOrAddComponent<CharacterHUDController>(gameObject);
            }

            // 4.按配置自动查找小地图控制器
            if (miniMapController == null && autoCreateMiniMap)
            {
                miniMapController = FindObjectOfType<MiniMapController>(true);
            }
        }
        #endregion

        #region 12. 旧版UI兼容桥接
        private void WireLegacyReferencesIfNeeded()
        {
            // 1.补齐旧版主菜单面板和按钮引用
            if (mainMenuController != null)
            {
                mainMenuController.ConfigureIfMissing(
                    MainPanel,
                    mainStartButton,
                    mainVolumeButton,
                    mainQuitButton);
            }

            // 2.补齐旧版设置菜单和音量面板引用
            if (settingsMenuController != null)
            {
                settingsMenuController.ConfigureIfMissing(
                    SettingsPanel,
                    VolumePanel,
                    settingsRestartButton,
                    settingsVolumeButton,
                    settingsQuitButton,
                    volumeToSettingsButton,
                    volumeAllVolumeSlider,
                    volumeBackgroundMusicSlider);
            }

            // 3.补齐旧版HUD控件引用
            if (characterHUDController != null)
            {
                characterHUDController.ConfigureIfMissing(
                    HUDPanel,
                    PlayerHealthbar,
                    PlayerStaminabar,
                    HealthPotionUI,
                    EnemyHealthSlider,
                    EnemyHealthText,
                    EnemyStunBar);
            }
        }
        #endregion

        #region 13. 通用控制
        private void SetCharacterInputEnabled(bool enabled)
        {
            // 1.优先使用当前角色门面控制角色输入
            if (characterFacade == null)
            {
                // 2.门面为空时，尝试重新抓取玩家控制器
                if (playerController == null)
                {
                    playerController = FindObjectOfType<PlayerController>(true);
                }

                if (playerController == null)
                {
                    return;
                }

                //if (enabled)
                //{
                //    playerController.EnableCurrentCharacterInput();
                //}
                //else
                //{
                //    playerController.DisableCurrentCharacterInput();
                //}

                return;
            }

            //if (enabled)
            //{
            //    characterFacade.EnablePlayerInput();
            //}
            //else
            //{
            //    characterFacade.DisablePlayerInput();
            //}
        }

        private void SetPauseAndCursor(bool pause)
        {
            // 1.优先通过时间服务统一控制暂停与鼠标状态
            GameTimeService timeService = ResolveGameTimeService();
            if (timeService != null)
            {
                timeService.SetPauseAndCursor(pause);
                return;
            }

            // 2.兜底直接操作Time和Cursor
            if (pause)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        #endregion

        #region 14. 服务解析与工具方法
        private GameTimeService ResolveGameTimeService()
        {
            // 1.显式引用存在时，直接返回
            if (gameTimeService != null)
            {
                return gameTimeService;
            }

            // 2.尝试从单例获取
            gameTimeService = GameTimeService.Instance;
            if (gameTimeService != null)
            {
                return gameTimeService;
            }

            // 3.尝试从Bootstrap获取
            if (GameBootstrap.Instance != null)
            {
                gameTimeService = GameBootstrap.Instance.TimeService;
            }

            // 4.最后从场景里兜底查找
            if (gameTimeService == null)
            {
                gameTimeService = FindObjectOfType<GameTimeService>(true);
            }

            return gameTimeService;
        }

        private static T GetOrAddComponent<T>(GameObject host) where T : Component
        {
            // 1.宿主为空时，直接返回空
            if (host == null)
            {
                return null;
            }

            // 2.优先获取现有组件
            T component = host.GetComponent<T>();
            if (component == null)
            {
                // 3.缺失时自动补齐组件
                component = host.AddComponent<T>();
            }

            return component;
        }
        #endregion
    }
}

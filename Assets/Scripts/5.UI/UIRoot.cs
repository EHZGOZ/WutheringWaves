using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // UI根控制器：统一调度主菜单、设置菜单、HUD和小地图生命周期
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; } // UI全局访问入口

        [Header("Controller References")]
        [SerializeField] private MainMenuController mainMenuController; // 主菜单控制器
        [SerializeField] private SettingsMenuController settingsMenuController; // 设置菜单控制器
        [SerializeField] private CharacterHUDController characterHUDController; // HUD控制器
        [SerializeField] private MiniMapController miniMapController; // 小地图控制器

        [Header("Injected Dependencies")]
        [SerializeField] private CharacterFacade characterFacade; // 当前绑定的角色门面
        [SerializeField] private CharacterCore legacyCore; // 兼容旧链路保留的角色核心引用
        [SerializeField] private PlayerController playerController; // 当前玩家控制器
        [SerializeField] private bool autoCreateMiniMap = true; // 缺少引用时是否自动查找小地图控制器
        [SerializeField] private GameTimeService gameTimeService; // 时间服务
        [SerializeField] private AudioService audioService; // 音频服务

        [Header("Legacy Panel Wiring (Optional)")]
        [SerializeField] private GameObject MainPanel;
        [SerializeField] private GameObject SettingsPanel;
        [SerializeField] private GameObject VolumePanel;
        [SerializeField] private GameObject HUDPanel;

        [Header("Legacy Main Menu (Optional)")]
        [SerializeField] private Button mainStartButton;
        [SerializeField] private Button mainVolumeButton;
        [SerializeField] private Button mainQuitButton;

        [Header("Legacy Settings Menu (Optional)")]
        [SerializeField] private Button settingsRestartButton;
        [SerializeField] private Button settingsVolumeButton;
        [SerializeField] private Button settingsQuitButton;

        [Header("Legacy Volume Menu (Optional)")]
        [SerializeField] private Slider volumeAllVolumeSlider;
        [SerializeField] private Slider volumeBackgroundMusicSlider;
        [SerializeField] private Button volumeToSettingsButton;

        [Header("Legacy HUD Widgets (Optional)")]
        [SerializeField] private GameObject PlayerHealthbar;
        [SerializeField] private GameObject PlayerStaminabar;
        [SerializeField] private GameObject HealthPotionUI;
        [SerializeField] private GameObject EnemyHealthSlider;
        [SerializeField] private GameObject EnemyHealthText;
        [SerializeField] private GameObject EnemyStunBar;

        public CharacterFacade Facade => characterFacade; // 当前角色门面
        public CharacterCore Core => legacyCore; // 兼容旧版调用：当前角色核心
        public GameTimeService TimeService => gameTimeService; // 当前时间服务
        public AudioService AudioService => audioService; // 当前音频服务

        private void Awake()
        {
            // UIRoot保持单例，避免场景里出现多个根控制器抢状态
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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

        // 注入当前角色门面：作为UI层统一角色入口
        public void InjectCharacterFacade(CharacterFacade facade)
        {
            if (facade == null)
            {
                return;
            }

            characterFacade = facade;
            legacyCore = facade.LegacyCore;
            characterHUDController?.SetCharacterFacade(characterFacade);
            miniMapController?.InjectDependencies(characterFacade);
        }

        // 兼容旧链路：允许外部仍按旧接口注入角色核心
        public void InjectCharacterCore(CharacterCore core)
        {
            if (core == null)
            {
                return;
            }

            legacyCore = core;
            InjectCharacterFacade(core.Facade != null ? core.Facade : core.GetComponent<CharacterFacade>());
        }

        public void InjectPlayerController(PlayerController controller)
        {
            if (controller == null)
            {
                return;
            }

            playerController = controller;
            InjectCharacterFacade(controller.CurrentCharacterFacade);
        }

        public void OpenMainMenu()
        {
            // 打开主菜单时关闭其他面板，并暂停游戏输入
            settingsMenuController?.CloseAll();
            mainMenuController?.SetVisible(true);
            characterHUDController?.SetVisible(true);
            SetPauseAndCursor(true);
            SetCharacterInputEnabled(false);
        }

        public void OpenSettingsMenu()
        {
            // 设置菜单属于暂停态UI，打开时同步暂停游戏逻辑
            mainMenuController?.SetVisible(false);
            settingsMenuController?.ShowSettings();
            SetPauseAndCursor(true);
            SetCharacterInputEnabled(false);
        }

        public void OpenVolumeMenu()
        {
            // 音量面板复用设置菜单控制器管理
            mainMenuController?.SetVisible(false);
            settingsMenuController?.ShowVolume();
            SetPauseAndCursor(true);
            SetCharacterInputEnabled(false);
        }

        public void CloseMenusAndResumeGameplay()
        {
            // 关闭菜单后恢复时间流动、鼠标锁定和角色输入
            mainMenuController?.SetVisible(false);
            settingsMenuController?.CloseAll();
            SetPauseAndCursor(false);
            SetCharacterInputEnabled(true);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 场景切换后重新抓取依赖，兼容跨场景UI或动态加载场景
            ResolveDependencies();
            ResolveControllers();
            WireLegacyReferencesIfNeeded();
            InitializeAndApplyLifecycle();
        }

        private void InitializeAndApplyLifecycle()
        {
            // 统一在这里初始化各个子控制器，避免初始化顺序分散
            if (mainMenuController != null)
            {
                mainMenuController.Initialize(
                    HandleStartGameRequested,
                    HandleOpenVolumeRequested,
                    HandleQuitRequested);
            }

            if (settingsMenuController != null)
            {
                settingsMenuController.Initialize(
                    audioService,
                    HandleRestartSceneRequested,
                    HandleReturnToMainMenuRequested);
            }

            if (characterHUDController != null)
            {
                characterHUDController.Initialize(characterFacade, this);
            }

            if (autoCreateMiniMap && miniMapController == null)
            {
                miniMapController = FindObjectOfType<MiniMapController>(true);
            }

            if (miniMapController != null)
            {
                miniMapController.Initialize(characterFacade);
            }

            OpenMainMenu();
        }

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

        private void ResolveDependencies()
        {
            // 统一收集服务层和角色层依赖，减少场景手动拖线要求
            ResolveGameTimeService();

            if (audioService == null)
            {
                audioService = FindObjectOfType<AudioService>(true);
            }

            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>(true);
            }

            if (characterFacade == null && playerController != null)
            {
                characterFacade = playerController.CurrentCharacterFacade;
            }

            if (characterFacade == null)
            {
                characterFacade = FindObjectOfType<CharacterFacade>(true);
            }

            if (legacyCore == null)
            {
                legacyCore = characterFacade != null ? characterFacade.LegacyCore : null;
            }

            if (legacyCore == null)
            {
                legacyCore = FindObjectOfType<CharacterCore>(true);
                if (characterFacade == null && legacyCore != null)
                {
                    characterFacade = legacyCore.Facade != null ? legacyCore.Facade : legacyCore.GetComponent<CharacterFacade>();
                }
            }
        }

        private void ResolveControllers()
        {
            // 优先使用Inspector引用，缺失时自动从子节点或当前对象补齐
            if (mainMenuController == null)
            {
                mainMenuController = GetComponentInChildren<MainMenuController>(true);
            }

            if (mainMenuController == null)
            {
                mainMenuController = GetOrAddComponent<MainMenuController>(gameObject);
            }

            if (settingsMenuController == null)
            {
                settingsMenuController = GetComponentInChildren<SettingsMenuController>(true);
            }

            if (settingsMenuController == null)
            {
                settingsMenuController = GetOrAddComponent<SettingsMenuController>(gameObject);
            }

            if (characterHUDController == null)
            {
                characterHUDController = GetComponentInChildren<CharacterHUDController>(true);
            }

            if (characterHUDController == null)
            {
                characterHUDController = GetOrAddComponent<CharacterHUDController>(gameObject);
            }

            if (miniMapController == null && autoCreateMiniMap)
            {
                miniMapController = FindObjectOfType<MiniMapController>(true);
            }
        }

        private void WireLegacyReferencesIfNeeded()
        {
            // 将旧版场景里的面板和控件引用补接到新控制器，方便平滑过渡
            if (mainMenuController != null)
            {
                mainMenuController.ConfigureIfMissing(
                    MainPanel,
                    mainStartButton,
                    mainVolumeButton,
                    mainQuitButton);
            }

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

        private void SetCharacterInputEnabled(bool enabled)
        {
            // 菜单开关统一控制角色输入，避免暂停时仍能操作角色
            if (characterFacade == null)
            {
                if (playerController == null)
                {
                    playerController = FindObjectOfType<PlayerController>(true);
                }

                if (playerController == null)
                {
                    return;
                }

                if (enabled)
                {
                    playerController.EnableCurrentCharacterInput();
                }
                else
                {
                    playerController.DisableCurrentCharacterInput();
                }

                return;
            }

            if (enabled)
            {
                characterFacade.EnablePlayerInput();
            }
            else
            {
                characterFacade.DisablePlayerInput();
            }
        }

        private void SetPauseAndCursor(bool pause)
        {
            // 优先走时间服务统一暂停；若服务不可用，则回退到直接操作Time和Cursor
            GameTimeService timeService = ResolveGameTimeService();
            if (timeService != null)
            {
                timeService.SetPauseAndCursor(pause);
                return;
            }

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

        private GameTimeService ResolveGameTimeService()
        {
            // 时间服务按“显式引用 -> 单例 -> Bootstrap -> 场景查找”的顺序兜底解析
            if (gameTimeService != null)
            {
                return gameTimeService;
            }

            gameTimeService = GameTimeService.Instance;
            if (gameTimeService != null)
            {
                return gameTimeService;
            }

            if (GameBootstrap.Instance != null)
            {
                gameTimeService = GameBootstrap.Instance.TimeService;
            }

            if (gameTimeService == null)
            {
                gameTimeService = FindObjectOfType<GameTimeService>(true);
            }

            return gameTimeService;
        }

        private static T GetOrAddComponent<T>(GameObject host) where T : Component
        {
            // 某些控制器缺失时自动补组件，减少基础UI对象的配置成本
            if (host == null)
            {
                return null;
            }

            T component = host.GetComponent<T>();
            if (component == null)
            {
                component = host.AddComponent<T>();
            }

            return component;
        }
    }
}

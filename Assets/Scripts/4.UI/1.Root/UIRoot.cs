using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // UI根控制器：统一调度主菜单、登录菜单、系统菜单、设置菜单、HUD和小地图的生命周期
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; } // UI全局访问入口

        private enum SettingsMenuSource
        {
            None,
            MainMenu,
            Gameplay,
            SystemMenu
        }

        [Header("=== UI控制器引用（手动拖入） ===")]
        [Tooltip("主菜单控制器")]
        [SerializeField] private MainMenuController mainMenuController;
        [Tooltip("登录菜单控制器")]
        [SerializeField] private LoginMenuController loginMenuController;
        [Tooltip("存档菜单控制器")]
        [SerializeField] private SavedGameMenuController savedGameMenuController;
        [Tooltip("系统菜单控制器")]
        [SerializeField] private SystemMenuController systemMenuController;
        [Tooltip("设置菜单控制器")]
        [SerializeField] private SettingsMenuController settingsMenuController;
        [Tooltip("音量菜单控制器")]
        [SerializeField] private VolumeMenuController volumeMenuController;
        [Tooltip("角色HUD控制器")]
        [SerializeField] private CharacterHUDController characterHUDController;
        [Tooltip("小地图控制器")]
        [SerializeField] private MiniMapController miniMapController;

        [Header("=== 运行时注入依赖 ===")]
        [Tooltip("当前玩家控制器")]
        [SerializeField] private PlayerController playerController;
        [Tooltip("当前绑定的角色上下文")]
        [SerializeField] private CharacterContext context;

        private bool IsInitialized;
        public CharacterContext Context => context; // 当前角色上下文
        private bool isSettingsMenuOpen; // 设置菜单是否打开
        private SettingsMenuSource settingsMenuSource = SettingsMenuSource.None; // 记录设置菜单来源，用于返回到正确界面

        #region 生命周期
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

            // 3.UIRoot作为全局UI根节点，切换场景时不销毁
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // 按下Esc时切换游戏内系统菜单
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleSystemMenu();
            }
        }
        #endregion

        #region 绑定玩家依赖
        // 绑定玩家控制器：进入游戏或切换角色后，由PlayerController主动传入最新玩家引用
        public void Bind(PlayerController controller)
        {
            // 1.空值检查
            if (controller == null)
            {
                return;
            }

            // 2.缓存玩家控制器
            playerController = controller;

            // 3.绑定当前角色上下文
            BindCharacterContext(controller.CurrentCharacterContext);
        }

        // 绑定当前角色上下文：作为HUD和小地图的统一角色入口
        public void BindCharacterContext(CharacterContext context)
        {
            // 1.空值检查
            if (context == null)
            {
                return;
            }

            // 2.缓存当前角色上下文
            this.context = context;

            // 3.把当前角色上下文同步给HUD和小地图
            characterHUDController?.Bind(this.context);

            miniMapController?.Bind(this.context);
        }
        #endregion

        #region 初始化
        // 初始化UIRoot：解析子控制器，绑定页面流程，并显示初始主菜单
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            // 1.解析所有UI控制器
            ResolveControllers();

            // 2.初始化UI流程
            InitializeUIFlow();

            // 3.启动时先显示主菜单，不直接弹出登录面板
            ShowMainMenu();

            IsInitialized = true;
        }
        #endregion

        #region 从场景中获取所有UI控制器
        private void ResolveControllers()
        {
            // 1.登录菜单控制器：优先Inspector手动绑定，其次从UIRoot子节点中自动查找
            if (loginMenuController == null)
            {
                loginMenuController = GetComponentInChildren<LoginMenuController>(true);
            }

            // 2.主菜单控制器：优先Inspector，其次子节点
            if (mainMenuController == null)
            {
                mainMenuController = GetComponentInChildren<MainMenuController>(true);
            }

            // 3.存档菜单控制器：优先Inspector，其次子节点
            if (savedGameMenuController == null)
            {
                savedGameMenuController = GetComponentInChildren<SavedGameMenuController>(true);
            }

            // 4.系统菜单控制器：优先Inspector，其次子节点
            if (systemMenuController == null)
            {
                systemMenuController = GetComponentInChildren<SystemMenuController>(true);
            }

            // 5.设置菜单控制器：优先Inspector，其次子节点
            if (settingsMenuController == null)
            {
                settingsMenuController = GetComponentInChildren<SettingsMenuController>(true);
            }

            // 6.音量菜单控制器：优先Inspector，其次子节点
            if (volumeMenuController == null)
            {
                volumeMenuController = GetComponentInChildren<VolumeMenuController>(true);
            }

            // 7.HUD控制器：优先Inspector，其次子节点
            if (characterHUDController == null)
            {
                characterHUDController = GetComponentInChildren<CharacterHUDController>(true);
            }

            // 8.小地图可能不在UIRoot子节点下，所以用场景查找兜底
            if (miniMapController == null)
            {
                miniMapController = FindObjectOfType<MiniMapController>(true);
            }
        }
        #endregion

        #region 初始化UI流程
        // 初始化UI流程
        private void InitializeUIFlow()
        {
            // 登录菜单：登录成功后刷新主菜单；关闭按钮只关闭登录面板并留在主界面
            loginMenuController?.Initialize(HandleLoginSuccessRequested, HandleLoginCloseRequested);

            // 主菜单：设置按钮打开覆盖式设置面板，不关闭主界面
            mainMenuController?.Initialize(
            HandleStartGameRequested,
            HandleLoginRequested,
            HandleSwitchAccountRequested,
            ShowSettingsMenuFromMainMenu,
            QuitGame
            );

            // 系统菜单：游戏中按Esc打开，负责继续游戏、打开设置、退出到主菜单
            systemMenuController?.Initialize(
            HandleSystemMenuExitGameRequested,
            HandleSystemMenuCloseRequested,
            HandleSystemMenuSettingsRequested
            );

            // 设置菜单：新版设置界面内部切换声音页和画质页，返回时由UIRoot判断来源
            settingsMenuController?.Initialize(HandleSettingsMenuReturnRequested);

            // 音量菜单：旧独立音量菜单暂时保留，新版设置界面不再主动进入它
            volumeMenuController?.Initialize(ShowSettingsMenuFromGameplay);

            // HUD和小地图先初始化，但启动时隐藏
            characterHUDController?.Initialize(this);
            miniMapController?.Initialize();
        }
        #endregion

        #region 页面切换
        public void ShowMainMenu()
        {
            // 1.回到主菜单时，设置来源清空
            isSettingsMenuOpen = false;
            settingsMenuSource = SettingsMenuSource.None;

            // 2.显示主菜单
            mainMenuController?.SetVisible(true);

            // 3.主菜单默认不弹出登录面板，登录面板只由登入账号或切换账号按钮打开
            loginMenuController?.SetVisible(false);

            // 4.隐藏其他菜单和玩法UI
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 5.刷新主菜单按钮状态
            mainMenuController?.RefreshLoginState();

            // 6.菜单界面停止背景音乐
            AudioService.Instance?.PauseBackgroundMusic();

            // 7.菜单界面禁用玩家输入
            SetPlayerInputEnabled(false);

            // 8.菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);
        }

        // 显示登录菜单：游戏刚启动、账号登出后调用
        public void ShowLoginMenu()
        {
            // 1.显示登录菜单，作为当前一账号一存档流程的入口
            loginMenuController?.SetVisible(true);

            // 2.隐藏其他菜单和玩法UI，避免登录界面和游戏界面重叠
            mainMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 3.登录界面停止背景音乐
            AudioService.Instance?.PauseBackgroundMusic();

            // 4.登录界面禁用玩家输入
            SetPlayerInputEnabled(false);

            // 5.登录界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);
        }

        // 显示存档菜单：旧三槽位流程暂时保留
        public void ShowSavedGameMenu()
        {
            // 1.隐藏主菜单
            mainMenuController?.SetVisible(false);

            // 2.设置为选档模式：主菜单进入时不显示主动存档按钮
            savedGameMenuController?.SetCanManualSave(false);

            // 3.显示存档菜单
            savedGameMenuController?.SetVisible(true);

            // 4.隐藏其他UI
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 5.记录设置菜单关闭
            isSettingsMenuOpen = false;
            settingsMenuSource = SettingsMenuSource.None;

            // 6.存档菜单界面停止背景音乐
            AudioService.Instance?.PauseBackgroundMusic();

            // 7.存档菜单界面禁用玩家输入
            SetPlayerInputEnabled(false);

            // 8.存档菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);
        }

        // 从设置菜单进入存档菜单：旧流程暂时保留
        private void ShowSavedGameMenuFromSettings()
        {
            // 1.记录设置菜单已关闭
            isSettingsMenuOpen = false;
            settingsMenuSource = SettingsMenuSource.None;

            // 2.隐藏主菜单和设置菜单
            mainMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);

            // 3.设置为主动存档模式：游戏中进入时允许保存到任意槽位
            savedGameMenuController?.SetCanManualSave(true);

            // 4.显示存档菜单
            savedGameMenuController?.SetVisible(true);

            // 5.隐藏玩法UI
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 6.存档菜单界面停止背景音乐
            AudioService.Instance?.PauseBackgroundMusic();

            // 7.存档菜单界面禁用玩家输入
            SetPlayerInputEnabled(false);

            // 8.存档菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);
        }

        // 显示设置菜单：保留公开入口，默认按游戏内设置处理
        public void ShowSettingsMenu()
        {
            ShowSettingsMenuFromGameplay();
        }

        // 从主菜单打开设置菜单：主界面保留在后面，设置界面作为覆盖层显示
        private void ShowSettingsMenuFromMainMenu()
        {
            // 1.记录设置菜单来源，关闭设置时回到主菜单
            isSettingsMenuOpen = true;
            settingsMenuSource = SettingsMenuSource.MainMenu;

            // 2.主菜单不要关闭，避免返回设置时主界面消失
            mainMenuController?.SetVisible(true);

            // 3.隐藏和主菜单无关的界面
            loginMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 4.显示设置菜单
            settingsMenuController?.SetVisible(true);

            // 5.主菜单设置界面保持菜单输入状态
            SetPlayerInputEnabled(false);
            SetPauseAndCursor(true);
        }

        // 从游戏内直接打开设置菜单：关闭后回到玩法UI
        private void ShowSettingsMenuFromGameplay()
        {
            // 1.记录设置菜单来源，关闭设置时回到游戏
            isSettingsMenuOpen = true;
            settingsMenuSource = SettingsMenuSource.Gameplay;

            // 2.隐藏菜单和玩法UI
            mainMenuController?.SetVisible(false);
            loginMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 3.显示设置菜单
            settingsMenuController?.SetVisible(true);

            // 4.设置菜单界面停止背景音乐
            AudioService.Instance?.PauseBackgroundMusic();

            // 5.设置菜单界面禁用玩家输入
            SetPlayerInputEnabled(false);

            // 6.设置菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);
        }

        // 从系统菜单打开设置菜单：关闭后回到SystemMenuPanel
        private void ShowSettingsMenuFromSystemMenu()
        {
            // 1.记录设置菜单来源，关闭设置时回到系统菜单
            isSettingsMenuOpen = true;
            settingsMenuSource = SettingsMenuSource.SystemMenu;

            // 2.隐藏系统菜单，避免和设置菜单重叠
            systemMenuController?.SetVisible(false);

            // 3.隐藏其他界面
            mainMenuController?.SetVisible(false);
            loginMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 4.显示设置菜单
            settingsMenuController?.SetVisible(true);

            // 5.设置菜单属于暂停菜单状态
            SetPlayerInputEnabled(false);
            SetPauseAndCursor(true);
        }

        // 显示音量菜单：旧独立音量菜单入口暂时保留
        public void ShowVolumeMenu()
        {
            // 1.记录设置菜单仍处于打开状态
            isSettingsMenuOpen = true;

            // 2.隐藏主菜单、存档菜单、设置菜单
            mainMenuController?.SetVisible(false);
            loginMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);

            // 3.显示音量菜单
            volumeMenuController?.SetVisible(true);

            // 4.隐藏玩法UI
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 5.音量菜单界面禁用玩家输入
            SetPlayerInputEnabled(false);

            // 6.音量菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);
        }

        // 显示玩法UI：新建存档或读档成功、真正进入游戏后调用
        public void ShowGameplayUI()
        {
            // 1.记录设置菜单已关闭
            isSettingsMenuOpen = false;
            settingsMenuSource = SettingsMenuSource.None;

            // 2.隐藏所有菜单
            mainMenuController?.SetVisible(false);
            loginMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);

            // 3.显示玩法UI
            characterHUDController?.SetVisible(true);
            miniMapController?.SetVisible(true);

            // 4.非菜单界面播放背景音乐
            AudioService.Instance?.ResumeBackgroundMusic();

            // 5.恢复玩家输入
            SetPlayerInputEnabled(true);

            // 6.恢复游戏时间，并隐藏锁定鼠标
            SetPauseAndCursor(false);
        }

        // 隐藏全部UI：用于过场、加载界面或特殊状态
        public void HideAllUI()
        {
            // 1.隐藏菜单UI
            mainMenuController?.SetVisible(false);
            loginMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);

            // 2.隐藏玩法UI
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 3.清空菜单状态
            isSettingsMenuOpen = false;
            settingsMenuSource = SettingsMenuSource.None;
        }

        // 切换系统菜单：Esc打开或关闭游戏中的系统菜单
        private void ToggleSystemMenu()
        {
            // 1.没有进入游戏时不响应Esc，避免主菜单和登录界面误打开系统菜单
            if (GameSessionService.Instance == null || !GameSessionService.Instance.IsInGame)
            {
                return;
            }

            // 2.如果设置菜单或音量菜单正在打开，Esc先回到系统菜单
            if (isSettingsMenuOpen)
            {
                isSettingsMenuOpen = false;
                settingsMenuSource = SettingsMenuSource.None;

                settingsMenuController?.SetVisible(false);
                volumeMenuController?.SetVisible(false);
                systemMenuController?.SetVisible(true);

                characterHUDController?.SetVisible(false);
                miniMapController?.SetVisible(false);

                SetPlayerInputEnabled(false);
                SetPauseAndCursor(true);
                return;
            }

            // 3.判断系统菜单是否显示时，必须看SystemMenuPanel本身，而不是SystemMenuController物体
            if (systemMenuController != null && systemMenuController.IsVisible)
            {
                HandleSystemMenuCloseRequested();
                return;
            }

            // 4.系统菜单未显示时，打开系统菜单并暂停游戏
            systemMenuController?.SetVisible(true);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            SetPlayerInputEnabled(false);
            SetPauseAndCursor(true);
        }
        #endregion

        #region 主菜单账号入口流程
        // 开始游戏：由主菜单“开始游戏”按钮调用，直接读取或创建当前账号存档
        private void HandleStartGameRequested()
        {
            // 1.游戏会话服务缺失时不进入游戏
            if (GameSessionService.Instance == null)
            {
                Debug.LogError("[UIRoot] 开始游戏失败：GameSessionService为空。", this);
                return;
            }

            // 2.当前没有登录账号时，不允许进入游戏，避免创建无账号存档
            if (AccountManager.Instance == null || !AccountManager.Instance.IsLoggedIn || AccountManager.Instance.CurrentAccount == null)
            {
                Debug.LogWarning("[UIRoot] 开始游戏失败：当前没有登录账号。", this);
                return;
            }

            // 3.一账号一存档流程：有存档就读取，没有存档就创建默认存档
            GameSessionService.Instance.StartGameWithCurrentAccount();
        }

        // 打开登录面板：由主菜单“登入账号”按钮调用
        private void HandleLoginRequested()
        {
            // 1.主界面保持显示，只弹出登录面板作为覆盖层
            mainMenuController?.SetVisible(true);
            loginMenuController?.SetVisible(true);

            // 2.打开登录面板时关闭设置面板，避免两个覆盖层叠在一起
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);

            // 3.清空设置菜单状态
            isSettingsMenuOpen = false;
            settingsMenuSource = SettingsMenuSource.None;

            // 4.打开登录面板时继续禁用玩家输入，并显示鼠标
            SetPlayerInputEnabled(false);
            SetPauseAndCursor(true);
        }

        // 登录成功：由LoginMenuController在账号登录成功后回调
        private void HandleLoginSuccessRequested()
        {
            // 1.登录成功后关闭登录面板
            loginMenuController?.SetVisible(false);

            // 2.刷新主菜单按钮状态
            mainMenuController?.RefreshLoginState();

            // 3.保持在主菜单界面，等待玩家点击开始游戏
            ShowMainMenu();
        }

        // 切换账号：由主菜单“切换账号”按钮调用
        private void HandleSwitchAccountRequested()
        {
            // 1.切换账号前，如果当前还在游戏中，则先尝试自动保存当前账号存档
            if (GameSessionService.Instance != null
                && GameSessionService.Instance.IsInGame
                && SaveService.Instance != null
                && SaveService.Instance.CurrentData != null
                && !string.IsNullOrWhiteSpace(SaveService.Instance.CurrentUsername))
            {
                GameSessionService.Instance.SaveCurrentGameOnExit();
            }

            // 2.登出当前账号，清理当前账号和当前存档绑定
            if (AccountManager.Instance != null)
            {
                AccountManager.Instance.Logout();
            }

            // 3.保持主界面显示，只刷新按钮状态
            mainMenuController?.SetVisible(true);
            mainMenuController?.RefreshLoginState();

            // 4.关闭其他覆盖层
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            systemMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);

            // 5.在主界面上弹出登录面板，让玩家登录新账号
            loginMenuController?.SetVisible(true);

            // 6.清空设置菜单状态
            isSettingsMenuOpen = false;
            settingsMenuSource = SettingsMenuSource.None;

            // 7.切换账号期间保持菜单输入状态
            SetPlayerInputEnabled(false);
            SetPauseAndCursor(true);
        }

        // 关闭登录面板：由LoginMenuController关闭按钮回调
        private void HandleLoginCloseRequested()
        {
            // 1.关闭登录面板
            loginMenuController?.SetVisible(false);

            // 2.回到主菜单，并根据当前账号状态刷新按钮
            ShowMainMenu();
        }
        #endregion

        #region 系统菜单流程
        // 系统菜单退出游戏：自动保存当前账号存档，并返回主菜单
        private void HandleSystemMenuExitGameRequested()
        {
            // 1.结束当前游戏会话，内部会尝试自动保存并清理玩家输入和背包绑定
            if (GameSessionService.Instance != null)
            {
                GameSessionService.Instance.EndCurrentGameSession();
            }

            // 2.回到主菜单，保留当前账号登录状态，方便玩家再次开始游戏
            ShowMainMenu();
        }

        // 系统菜单关闭：关闭系统菜单并继续游戏
        private void HandleSystemMenuCloseRequested()
        {
            // 1.隐藏系统菜单
            systemMenuController?.SetVisible(false);

            // 2.恢复玩法UI、玩家输入、游戏时间和鼠标锁定
            ShowGameplayUI();
        }

        // 系统菜单打开设置：隐藏系统菜单，显示设置菜单
        private void HandleSystemMenuSettingsRequested()
        {
            // 1.从系统菜单进入设置，关闭设置后需要回到系统菜单
            ShowSettingsMenuFromSystemMenu();
        }
        #endregion

        #region 设置菜单返回流程
        // 设置菜单返回：根据打开设置菜单的来源回到正确界面
        private void HandleSettingsMenuReturnRequested()
        {
            // 1.先关闭设置菜单和旧音量菜单
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);

            // 2.记录设置菜单已经关闭
            isSettingsMenuOpen = false;

            // 3.从主菜单打开设置：关闭设置后保留主菜单
            if (settingsMenuSource == SettingsMenuSource.MainMenu)
            {
                settingsMenuSource = SettingsMenuSource.None;

                mainMenuController?.SetVisible(true);
                mainMenuController?.RefreshLoginState();

                loginMenuController?.SetVisible(false);
                savedGameMenuController?.SetVisible(false);
                systemMenuController?.SetVisible(false);
                characterHUDController?.SetVisible(false);
                miniMapController?.SetVisible(false);

                AudioService.Instance?.PauseBackgroundMusic();

                SetPlayerInputEnabled(false);
                SetPauseAndCursor(true);
                return;
            }

            // 4.从系统菜单打开设置：关闭设置后重新显示系统菜单
            if (settingsMenuSource == SettingsMenuSource.SystemMenu)
            {
                settingsMenuSource = SettingsMenuSource.None;

                mainMenuController?.SetVisible(false);
                loginMenuController?.SetVisible(false);
                savedGameMenuController?.SetVisible(false);

                systemMenuController?.SetVisible(true);
                characterHUDController?.SetVisible(false);
                miniMapController?.SetVisible(false);

                SetPlayerInputEnabled(false);
                SetPauseAndCursor(true);
                return;
            }

            // 5.从游戏内直接打开设置：关闭设置后回到游戏
            settingsMenuSource = SettingsMenuSource.None;
            ShowGameplayUI();
        }
        #endregion

        #region 工具方法
        private void SetPauseAndCursor(bool pause)
        {
            // 1.优先通过时间服务统一控制暂停与恢复
            if (GameTimeService.Instance != null)
            {
                if (pause)
                {
                    GameTimeService.Instance.Pause();
                }
                else
                {
                    GameTimeService.Instance.Resume();
                }
            }
            else
            {
                // 2.兜底直接操作Time，避免时间服务缺失时UI完全失效
                Time.timeScale = pause ? 0f : 1f;
            }

            // 3.优先通过输入服务统一控制鼠标显示和锁定状态
            if (InputService.Instance != null)
            {
                InputService.Instance.SetCursorByPauseState(pause);
                return;
            }

            // 4.兜底直接操作Cursor，避免输入服务缺失时鼠标状态错误
            Cursor.visible = pause;
            Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // 设置玩家输入是否启用
        private void SetPlayerInputEnabled(bool enabled)
        {
            // 1.优先通过输入服务统一控制玩家输入，避免UIRoot直接依赖PlayerInputReader
            if (InputService.Instance != null)
            {
                if (enabled)
                {
                    InputService.Instance.EnablePlayerInput();
                }
                else
                {
                    InputService.Instance.DisablePlayerInput();
                }

                return;
            }

            // 2.兜底：输入服务缺失时，临时直接控制当前玩家输入读取器
            PlayerInputReader inputReader = playerController != null
                ? playerController.CurrentPlayerInputReader
                : null;

            if (inputReader == null)
            {
                return;
            }

            if (enabled)
            {
                inputReader.EnablePlayerInput();
            }
            else
            {
                inputReader.DisablePlayerInput();
            }
        }
        #endregion

        #region 新建 读取 储存 删除 存档 （已废弃）
        //新建
        private void HandleCreateSaveRequested(int slotIndex)
        {
            // 1.游戏会话服务缺失时不进入游戏
            if (GameSessionService.Instance == null)
            {
                Debug.LogError("[UIRoot] 开始游戏失败：GameSessionService为空。", this);
                return;
            }

            // 2.一账号一存档阶段：点击开始游戏后，由会话服务自动读取或创建当前账号存档
            GameSessionService.Instance.StartGameWithCurrentAccount();
        }

        //读取
        private void HandleLoadSaveRequested(int slotIndex)
        {
            // 1.游戏会话服务缺失时不进入游戏
            if (GameSessionService.Instance == null)
            {
                Debug.LogError("[UIRoot] 读取当前账号存档失败：GameSessionService为空。", this);
                return;
            }

            // 2.一账号一存档阶段：读取和开始游戏合并，由会话服务统一处理
            GameSessionService.Instance.StartGameWithCurrentAccount();
        }

        //存档
        private void HandleSaveRequested(int slotIndex)
        {
            // 1.游戏会话服务缺失时不保存
            if (GameSessionService.Instance == null)
            {
                Debug.LogError("[UIRoot] 自动保存失败：GameSessionService为空。", this);
                return;
            }

            // 2.一账号一存档阶段：主动存档临时复用退出自动保存逻辑
            bool ok = GameSessionService.Instance.SaveCurrentGameOnExit();

            // 3.保存后留在当前菜单，只刷新账号存档显示
            if (ok)
            {
                savedGameMenuController?.RefreshSlots();
                Debug.Log("[UIRoot] 当前账号存档保存完成。");
            }
        }

        //删除
        private void HandleDeleteSaveRequested(int slotIndex)
        {
            // 1.存档服务缺失时不删除
            if (SaveService.Instance == null)
            {
                Debug.LogError("[UIRoot] 删除当前账号存档失败：SaveService为空。", this);
                return;
            }

            // 2.账号服务缺失或未登录时，不允许删除当前账号存档
            if (AccountManager.Instance == null || !AccountManager.Instance.IsLoggedIn || AccountManager.Instance.CurrentAccount == null)
            {
                Debug.LogWarning("[UIRoot] 删除当前账号存档失败：当前没有登录账号。", this);
                return;
            }

            // 3.删除当前登录账号的账号存档
            SaveService.Instance.DeleteSave(AccountManager.Instance.CurrentAccount.username);

            // 4.删除后刷新账号存档显示
            savedGameMenuController?.RefreshSlots();
        }
        #endregion
    }
}
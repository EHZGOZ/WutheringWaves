using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // UI根控制器：统一调度主菜单、设置菜单、HUD和小地图的生命周期
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; } // UI全局访问入口

        [Header("=== UI控制器引用（手动拖入） ===")]
        [Tooltip("主菜单控制器")]
        [SerializeField] private MainMenuController mainMenuController;
        [Tooltip("登录菜单控制器")]
        [SerializeField] private LoginMenuController loginMenuController;
        [Tooltip("存档菜单控制器")]
        [SerializeField] private SavedGameMenuController savedGameMenuController;
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
            // 按下Esc时切换设置菜单
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleSettingsMenu();
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

            // 3.启动时显示主菜单
            ShowMainMenu();

            IsInitialized = true;
        }
        #endregion

        #region 解析所有UI控制器
        private void ResolveControllers()
        {
            // 1.主菜单控制器：优先Inspector，其次子节点，最后自动补组件
            if (mainMenuController == null)
            {
                mainMenuController = GetComponentInChildren<MainMenuController>(true);
            }
            // 2.存档菜单控制器：优先Inspector，其次子节点
            if (savedGameMenuController == null)
            {
                savedGameMenuController = GetComponentInChildren<SavedGameMenuController>(true);
            }

            // 3.设置菜单控制器：优先Inspector，其次子节点，最后自动补组件
            if (settingsMenuController == null)
            {
                settingsMenuController = GetComponentInChildren<SettingsMenuController>(true);
            }
            // 4.音量菜单控制器：优先Inspector，其次子节点
            if (volumeMenuController == null)
            {
                volumeMenuController = GetComponentInChildren<VolumeMenuController>(true);
            }

            // 5.HUD控制器：
            if (characterHUDController == null)
            {
                characterHUDController = GetComponentInChildren<CharacterHUDController>(true);
            }

            // 6.按配置自动查找小地图控制器
            if (miniMapController == null)
            {
                miniMapController = FindObjectOfType<MiniMapController>(true);
            }
        }
        #endregion

        #region 初始化UI流程
        //初始化UI流程
        private void InitializeUIFlow()
        {
            // 主菜单：开始游戏进入存档菜单，退出游戏直接退出程序
            mainMenuController?.Initialize(ShowSavedGameMenu, QuitGame);

            // 存档菜单：先只初始化，具体三槽位逻辑下一步再做
            savedGameMenuController?.Initialize(
            HandleCreateSaveRequested,
            HandleSaveRequested,
            HandleLoadSaveRequested,
            HandleDeleteSaveRequested,
            ShowMainMenu
            );

            // 设置菜单：进入存档菜单，或者返回游戏
            settingsMenuController?.Initialize(ShowSavedGameMenuFromSettings, ShowGameplayUI, ShowVolumeMenu);

            volumeMenuController?.Initialize(ShowSettingsMenu);


            // HUD和小地图先初始化，但启动时隐藏
            characterHUDController?.Initialize(this);
            miniMapController?.Initialize();
        }
        #endregion

        #region 页面切换
        // 显示主菜单：游戏刚启动、从存档菜单返回主菜单时调用
        public void ShowMainMenu()
        {
            // 1.显示主菜单
            mainMenuController?.SetVisible(true);

            // 2.隐藏其他菜单和玩法UI
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 3.菜单界面停止背景音乐
            AudioService.Instance?.PauseBackgroundMusic();

            // 4.菜单界面禁用玩家输入
            SetPlayerInputEnabled(false);

            // 5.菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);

        }

        // 显示存档菜单：点击“开始游戏”后调用
        public void ShowSavedGameMenu()
        {
            // 1.隐藏主菜单
            mainMenuController?.SetVisible(false);

            // 2.设置为选档模式：主菜单进入时不显示主动存档按钮
            savedGameMenuController?.SetCanManualSave(false);

            // 3.显示存档菜单
            savedGameMenuController?.SetVisible(true);

            // 4.隐藏设置菜单和玩法UI
            settingsMenuController?.SetVisible(false);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);

            // 5.存档菜单界面停止背景音乐
            AudioService.Instance?.PauseBackgroundMusic();

            // 6.存档菜单界面禁用玩家输入
            SetPlayerInputEnabled(false);

            // 7.存档菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);

        }

        // 从设置菜单进入存档菜单：用于游戏中读档和主动存档
        private void ShowSavedGameMenuFromSettings()
        {
            // 1.记录设置菜单已关闭
            isSettingsMenuOpen = false;

            // 2.隐藏主菜单和设置菜单
            mainMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);

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

        // 显示设置菜单：当前设置功能还没做，先保留页面切换入口
        public void ShowSettingsMenu()
        {
            // 1.记录设置菜单已打开
            isSettingsMenuOpen = true;

            // 2.隐藏主菜单和存档菜单
            mainMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            // 3.显示设置菜单
            settingsMenuController?.SetVisible(true);

            // 4.隐藏玩法UI
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 5.设置菜单界面停止背景音乐
            AudioService.Instance?.PauseBackgroundMusic();

            // 6.设置菜单界面禁用玩家输入
            SetPlayerInputEnabled(false);

            // 7.设置菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);

        }
        // 显示音量菜单：从设置菜单进入
        public void ShowVolumeMenu()
        {
            // 1.记录设置菜单仍处于打开状态
            isSettingsMenuOpen = true;

            // 2.隐藏主菜单、存档菜单、设置菜单
            mainMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);

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

            // 2.隐藏所有菜单
            mainMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);

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
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);
            volumeMenuController?.SetVisible(false);
            // 2.隐藏玩法UI
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);
        }

        // 切换设置菜单：Esc打开设置菜单，再按一次Esc返回玩法UI
        private void ToggleSettingsMenu()
        {
            if (isSettingsMenuOpen)
            {
                ShowGameplayUI();
            }
            else
            {
                ShowSettingsMenu();
            }
        }

        #endregion

        #region 新建 读取 储存 删除 存档 
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

    }
}

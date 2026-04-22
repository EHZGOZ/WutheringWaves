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
        [Tooltip("存档菜单控制器")]
        [SerializeField] private SavedGameMenuController savedGameMenuController;
        [Tooltip("设置菜单控制器")]
        [SerializeField] private SettingsMenuController settingsMenuController;
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
            characterHUDController?.SetCharacterContext(this.context);
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

            // 4.HUD控制器：
            if (characterHUDController == null)
            {
                characterHUDController = GetComponentInChildren<CharacterHUDController>(true);
            }

            // 5.按配置自动查找小地图控制器
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
            settingsMenuController?.Initialize(ShowSavedGameMenuFromSettings, ShowGameplayUI);


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
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 3.菜单界面暂停游戏，并显示鼠标
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

            // 5.存档菜单界面暂停游戏，并显示鼠标
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

            // 3.设置为主动存档模式：游戏中进入时允许保存到任意槽位
            savedGameMenuController?.SetCanManualSave(true);

            // 4.显示存档菜单
            savedGameMenuController?.SetVisible(true);

            // 5.隐藏玩法UI
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 6.存档菜单界面暂停游戏，并显示鼠标
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

            // 3.显示设置菜单
            settingsMenuController?.SetVisible(true);

            // 4.隐藏玩法UI
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 5.设置菜单界面暂停游戏，并显示鼠标
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

            // 3.显示玩法UI
            characterHUDController?.SetVisible(true);
            miniMapController?.SetVisible(true);

            // 4.恢复游戏时间，并隐藏锁定鼠标
            SetPauseAndCursor(false);
        }


        // 隐藏全部UI：用于过场、加载界面或特殊状态
        public void HideAllUI()
        {
            // 1.隐藏菜单UI
            mainMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);

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
            //Debug.Log($"[UIRoot] 请求新建存档：槽位 {slotIndex + 1}");

            // 1.GameBootstrap缺失时不进入游戏
            if (GameBootstrap.Instance == null)
            {
                Debug.LogError("[UIRoot] 新建存档失败：GameBootstrap为空。", this);
                return;
            }

            // 2.交给GameBootstrap处理新建存档和进入游戏流程
            GameBootstrap.Instance.StartNewGame(slotIndex);
        }
        //读取
        private void HandleLoadSaveRequested(int slotIndex)
        {
            //Debug.Log($"[UIRoot] 请求读取存档：槽位 {slotIndex + 1}");

            // 1.GameBootstrap缺失时不进入游戏
            if (GameBootstrap.Instance == null)
            {
                Debug.LogError("[UIRoot] 读取存档失败：GameBootstrap为空。", this);
                return;
            }

            // 2.交给GameBootstrap处理读档和进入游戏流程
            GameBootstrap.Instance.LoadGame(slotIndex);
        }
        //存档
        private void HandleSaveRequested(int slotIndex)
        {
            //Debug.Log($"[UIRoot] 请求主动存档：槽位 {slotIndex + 1}");

            // 1.GameBootstrap缺失时不保存
            if (GameBootstrap.Instance == null)
            {
                Debug.LogError("[UIRoot] 主动存档失败：GameBootstrap为空。", this);
                return;
            }

            // 2.交给GameBootstrap处理场景数据收集和指定槽位保存
            bool ok = GameBootstrap.Instance.SaveCurrentGameToSlot(slotIndex);

            // 3.保存后留在存档菜单，只刷新槽位显示
            if (ok)
            {
                savedGameMenuController?.RefreshSlots();
                Debug.Log($"[UIRoot] 主动存档完成，已留在存档菜单。槽位：{slotIndex + 1}");
            }
        }
        //删除
        private void HandleDeleteSaveRequested(int slotIndex)
        {
            //Debug.Log($"[UIRoot] 请求删除存档：槽位 {slotIndex + 1}");

            // 1.GameBootstrap缺失时不删除
            if (GameBootstrap.Instance == null)
            {
                Debug.LogError("[UIRoot] 删除存档失败：GameBootstrap为空。", this);
                return;
            }

            // 2.交给GameBootstrap处理删除存档流程
            GameBootstrap.Instance.DeleteGameSave(slotIndex);

            // 3.删除后刷新存档槽显示
            savedGameMenuController?.RefreshSlots();
        }

        #endregion

        #region 工具方法
        private void SetPauseAndCursor(bool pause)
        {
            // 1.优先通过时间服务统一控制暂停与鼠标状态

            if (GameTimeService.Instance != null)
            {
                GameTimeService.Instance.SetPauseAndCursor(pause);
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
        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }
        #endregion

    }
}

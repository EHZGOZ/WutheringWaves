using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

        public CharacterContext Context => context; // 当前角色上下文


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

            // 3.如果UIRoot需要跨场景保留，就开启这一句
            DontDestroyOnLoad(gameObject);

            // 4.解析所有UI控制器
            ResolveControllers();

            // 5.初始化UI流程
            InitializeUIFlow();

            // 6.启动时显示主菜单
            ShowMainMenu();
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
            HandleLoadSaveRequested,
             HandleDeleteSaveRequested,
            ShowMainMenu
            );

            // 设置菜单：功能还没做，先保留初始化入口
            settingsMenuController?.Initialize();

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

            // 2.显示存档菜单
            savedGameMenuController?.SetVisible(true);

            // 3.隐藏设置菜单和玩法UI
            settingsMenuController?.SetVisible(false);
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 4.存档菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);
        }

        // 显示设置菜单：当前设置功能还没做，先保留页面切换入口
        public void ShowSettingsMenu()
        {
            // 1.隐藏主菜单和存档菜单
            mainMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);

            // 2.显示设置菜单
            settingsMenuController?.SetVisible(true);

            // 3.隐藏玩法UI
            characterHUDController?.SetVisible(false);
            miniMapController?.SetVisible(false);

            // 4.设置菜单界面暂停游戏，并显示鼠标
            SetPauseAndCursor(true);
        }

        // 显示玩法UI：新建存档或读档成功、真正进入游戏后调用
        public void ShowGameplayUI()
        {
            // 1.隐藏所有菜单
            mainMenuController?.SetVisible(false);
            savedGameMenuController?.SetVisible(false);
            settingsMenuController?.SetVisible(false);

            // 2.显示玩法UI
            characterHUDController?.SetVisible(true);
            miniMapController?.SetVisible(true);

            // 3.恢复游戏时间，并隐藏锁定鼠标
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
        #endregion

        #region 存档菜单请求
        private void HandleCreateSaveRequested(int slotIndex)
        {
            Debug.Log($"[UIRoot] 请求新建存档：槽位 {slotIndex + 1}");

            // 1.GameBootstrap缺失时不进入游戏
            if (GameBootstrap.Instance == null)
            {
                Debug.LogError("[UIRoot] 新建存档失败：GameBootstrap为空。", this);
                return;
            }

            // 2.交给GameBootstrap处理新建存档和进入游戏流程
            GameBootstrap.Instance.StartNewGame(slotIndex);
        }

        private void HandleLoadSaveRequested(int slotIndex)
        {
            Debug.Log($"[UIRoot] 请求读取存档：槽位 {slotIndex + 1}");

            // 1.GameBootstrap缺失时不进入游戏
            if (GameBootstrap.Instance == null)
            {
                Debug.LogError("[UIRoot] 读取存档失败：GameBootstrap为空。", this);
                return;
            }

            // 2.交给GameBootstrap处理读档和进入游戏流程
            GameBootstrap.Instance.LoadGame(slotIndex);
        }

        private void HandleDeleteSaveRequested(int slotIndex)
        {
            Debug.Log($"[UIRoot] 请求删除存档：槽位 {slotIndex + 1}");

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



        public void RefreshUIRoot(PlayerController controller)
        {
            // 1.注入玩家控制器
            InjectPlayerController(controller);

            // 2.进入游戏后显示玩法UI
            ShowGameplayUI();
        }

        // 注入玩家控制器：由PlayerController在角色绑定完成后主动回写
        public void InjectPlayerController(PlayerController controller)
        {
            if (controller == null)
            {
                return;
            }

            playerController = controller;
            InjectCharacterContext(controller.CurrentCharacterContext);
        }
        // 注入当前角色上下文：作为UI层统一的角色入口
        public void InjectCharacterContext(CharacterContext context)
        {
            if (context == null)
            {
                return;
            }

            this.context = context;
            characterHUDController?.SetCharacterContext(this.context);
            miniMapController?.InjectDependencies(this.context);
        }
    

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

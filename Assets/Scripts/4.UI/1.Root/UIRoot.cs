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

            // 3.先解析依赖和子控制器，保证Start阶段可以直接初始化

            ResolveControllers();

        }


        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region 注入入口
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
        #endregion


        #region 依赖解析


        private void ResolveControllers()
        {
            // 1.主菜单控制器：优先Inspector，其次子节点，最后自动补组件
            if (mainMenuController == null)
            {
                mainMenuController = GetComponentInChildren<MainMenuController>(true);
            }

            // 2.设置菜单控制器：优先Inspector，其次子节点，最后自动补组件
            if (settingsMenuController == null)
            {
                settingsMenuController = GetComponentInChildren<SettingsMenuController>(true);
            }

            // 3.HUD控制器：
            if (characterHUDController == null)
            {
                characterHUDController = GetComponentInChildren<CharacterHUDController>(true);
            }


            // 4.按配置自动查找小地图控制器
            if (miniMapController == null)
            {
                miniMapController = FindObjectOfType<MiniMapController>(true);
            }
        }
        #endregion



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



    }
}

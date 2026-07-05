using System;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    // 系统菜单控制器：负责游戏中Esc呼出的系统菜单按钮事件转发
    public class SystemMenuController : MonoBehaviour
    {
        [Header("系统菜单面板")]
        [SerializeField] private GameObject systemMenuPanel; // 系统菜单主面板

        [Header("退出游戏按钮")]
        [SerializeField] private Button exitGameButton; // 退出当前游戏并返回主界面按钮

        [Header("关闭按钮")]
        [SerializeField] private Button closeButton; // 关闭系统菜单并继续游戏按钮

        [Header("设置按钮")]
        [SerializeField] private Button settingsButton; // 打开设置菜单按钮

        private Action onExitGameRequested; // 退出当前游戏请求
        private Action onCloseRequested; // 关闭系统菜单请求
        private Action onSettingsRequested; // 打开设置菜单请求

        private bool initialized; // 是否已初始化
        private bool listenersBound; // 是否已绑定按钮事件

        #region 生命周期
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

        private void OnDestroy()
        {
            UnbindListeners();
        }
        #endregion

        #region 初始化
        // 初始化系统菜单：由UIRoot传入系统菜单各按钮对应的流程回调
        public void Initialize(
            Action exitGameRequested,
            Action closeRequested,
            Action settingsRequested)
        {
            // 1.缓存外部流程回调，系统菜单只转发按钮事件，不直接处理具体逻辑
            onExitGameRequested = exitGameRequested;
            onCloseRequested = closeRequested;
            onSettingsRequested = settingsRequested;

            // 2.绑定按钮事件
            BindListeners();

            // 3.启动时默认隐藏系统菜单，只有游戏中按Esc才显示
            SetVisible(false);

            // 4.标记初始化完成，后续OnEnable时可以自动重新绑定按钮事件
            initialized = true;
        }
        #endregion

        #region 按钮绑定与解绑
        // 绑定按钮事件
        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (exitGameButton != null)
            {
                exitGameButton.onClick.AddListener(HandleExitGameClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(HandleSettingsClicked);
            }

            listenersBound = true;
        }

        // 解绑按钮事件
        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            if (exitGameButton != null)
            {
                exitGameButton.onClick.RemoveListener(HandleExitGameClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettingsClicked);
            }

            listenersBound = false;
        }
        #endregion

        #region 按钮事件
        // 点击退出游戏按钮：退出当前游戏会话并返回主界面，具体保存和切界面逻辑交给UIRoot
        private void HandleExitGameClicked()
        {
            onExitGameRequested?.Invoke();
        }

        // 点击关闭按钮：关闭系统菜单并继续游戏
        private void HandleCloseClicked()
        {
            onCloseRequested?.Invoke();
        }

        // 点击设置按钮：隐藏系统菜单并打开设置菜单
        private void HandleSettingsClicked()
        {
            onSettingsRequested?.Invoke();
        }
        #endregion

        #region 外部调用
        // 外部只读访问：判断系统菜单面板当前是否正在显示
        public bool IsVisible => systemMenuPanel != null && systemMenuPanel.activeSelf;

        // 设置系统菜单显隐
        public void SetVisible(bool visible)
        {
            if (systemMenuPanel != null)
            {
                systemMenuPanel.SetActive(visible);
            }
        }
        #endregion
    }
}
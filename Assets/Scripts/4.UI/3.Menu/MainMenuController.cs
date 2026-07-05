using System;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 主菜单控制器：负责主界面按钮事件转发，并根据账号登录状态刷新按钮显隐
    public class MainMenuController : MonoBehaviour
    {
        [Header("主菜单面板 Menu")]
        [SerializeField] private GameObject mainPanel; // 主菜单面板

        [Header("账号相关按钮")]
        [SerializeField] private Button loginButton; // 登入账号按钮：未登录时显示，点击后打开登录面板
        [SerializeField] private Button switchAccountButton; // 切换账号按钮：已登录时显示，点击后自动保存并切换账号

        [Header("主菜单功能按钮")]
        [SerializeField] private Button startButton; // 开始游戏按钮：已登录后才显示
        [SerializeField] private Button settingsButton; // 设置按钮：未登录和已登录都可以使用
        [SerializeField] private Button quitButton; // 退出按钮：未登录和已登录都可以使用

        private Action onStartRequested; // 开始游戏回调
        private Action onLoginRequested; // 打开登录面板回调
        private Action onSwitchAccountRequested; // 切换账号回调
        private Action onSettingsRequested; // 打开设置菜单回调
        private Action onQuitRequested; // 退出游戏回调

        private bool initialized; // 是否已完成初始化
        private bool listenersBound; // 是否已完成事件绑定

        #region 生命周期
        private void OnEnable()
        {
            if (initialized)
            {
                BindListeners();
                RefreshLoginState();
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
        public void Initialize(Action startRequested, Action quitRequested)
        {
            // 兼容旧版UIRoot调用：后续UIRoot改完后，会改用完整初始化方法
            Initialize(startRequested, null, null, null, quitRequested);
        }

        public void Initialize(
        Action startRequested,
        Action loginRequested,
        Action switchAccountRequested,
        Action settingsRequested,
        Action quitRequested)
        {
            // 1.缓存主菜单各按钮回调
            onStartRequested = startRequested;
            onLoginRequested = loginRequested;
            onSwitchAccountRequested = switchAccountRequested;
            onSettingsRequested = settingsRequested;
            onQuitRequested = quitRequested;

            // 2.标记初始化完成
            initialized = true;

            // 3.绑定按钮事件
            BindListeners();

            // 4.初始化后立刻刷新按钮显隐，确保未登录时隐藏开始游戏和切换账号
            RefreshLoginState();
        }
        #endregion

        #region 按钮事件绑定
        private void BindListeners()
        {
            // 1.避免重复绑定，防止按钮点击被多次触发
            if (listenersBound)
            {
                return;
            }

            if (loginButton != null)
            {
                loginButton.onClick.AddListener(HandleLoginClicked);
            }

            if (switchAccountButton != null)
            {
                switchAccountButton.onClick.AddListener(HandleSwitchAccountClicked);
            }

            if (startButton != null)
            {
                startButton.onClick.AddListener(HandleStartClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(HandleSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(HandleQuitClicked);
            }

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            // 1.没有绑定时不重复解绑
            if (!listenersBound)
            {
                return;
            }

            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(HandleLoginClicked);
            }

            if (switchAccountButton != null)
            {
                switchAccountButton.onClick.RemoveListener(HandleSwitchAccountClicked);
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(HandleQuitClicked);
            }

            listenersBound = false;
        }
        #endregion

        #region 按钮点击事件
        private void HandleLoginClicked()
        {
            // 1.登入账号按钮只负责发起打开登录面板请求
            onLoginRequested?.Invoke();
        }

        private void HandleSwitchAccountClicked()
        {
            // 1.切换账号按钮只负责发起切换账号请求，自动保存和登出由UIRoot统一处理
            onSwitchAccountRequested?.Invoke();
        }

        private void HandleStartClicked()
        {
            // 1.开始游戏按钮只负责发起开始游戏请求
            onStartRequested?.Invoke();
        }

        private void HandleSettingsClicked()
        {
            // 1.设置按钮只负责发起打开设置菜单请求
            onSettingsRequested?.Invoke();
        }

        private void HandleQuitClicked()
        {
            // 1.退出按钮只负责发起退出游戏请求
            onQuitRequested?.Invoke();
        }
        #endregion

        #region 外部调用
        public void SetVisible(bool visible)
        {
            // 1.主菜单显隐统一通过面板根节点控制
            if (mainPanel != null)
            {
                mainPanel.SetActive(visible);
            }

            // 2.每次显示主菜单时刷新账号按钮状态，避免登录/登出后按钮状态残留
            if (visible)
            {
                RefreshLoginState();
            }
        }

        public void RefreshLoginState()
        {
            // 1.根据账号服务判断当前是否已经登录账号
            bool isLoggedIn = AccountManager.Instance != null
            && AccountManager.Instance.IsLoggedIn
            && AccountManager.Instance.CurrentAccount != null;

            // 2.未登录时显示登入账号按钮，已登录后隐藏
            if (loginButton != null)
            {
                loginButton.gameObject.SetActive(!isLoggedIn);
            }

            // 3.已登录后才显示开始游戏按钮
            if (startButton != null)
            {
                startButton.gameObject.SetActive(isLoggedIn);
            }

            // 4.已登录后才显示切换账号按钮
            if (switchAccountButton != null)
            {
                switchAccountButton.gameObject.SetActive(isLoggedIn);
            }

            // 5.设置按钮不依赖账号，未登录也允许打开
            if (settingsButton != null)
            {
                settingsButton.gameObject.SetActive(true);
            }

            // 6.退出按钮不依赖账号，始终显示
            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(true);
            }
        }
        #endregion
    }
}
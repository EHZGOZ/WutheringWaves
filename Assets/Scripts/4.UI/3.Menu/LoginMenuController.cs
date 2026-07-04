using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    // 登录菜单控制器：负责账号登录、注册请求和登录结果显示
    public class LoginMenuController : MonoBehaviour
    {
        [Header("登录菜单面板")]
        [SerializeField] private GameObject loginPanel; // 登录菜单面板

        [Header("账号输入框")]
        [SerializeField] private TMP_InputField usernameInputField; // 用户名输入框

        [Header("密码输入框")]
        [SerializeField] private TMP_InputField passwordInputField; // 密码输入框

        [Header("提示文本")]
        [SerializeField] private TextMeshProUGUI messageText; // 登录或注册结果提示

        [Header("登录按钮")]
        [SerializeField] private Button loginButton; // 登录按钮

        [Header("注册按钮")]
        [SerializeField] private Button registerButton; // 注册按钮

        private Action onLoginSuccessRequested; // 登录成功后通知UIRoot切换页面

        private bool initialized; // 是否已初始化
        private bool listenersBound; // 是否已绑定按钮事件

        #region 初始化
        // 初始化登录菜单
        public void Initialize(Action loginSuccessRequested)
        {
            // 1.缓存登录成功回调
            onLoginSuccessRequested = loginSuccessRequested;

            // 2.绑定按钮事件
            BindListeners();

            // 3.标记初始化完成
            initialized = true;
        }
        #endregion

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

            if (registerButton != null)
            {
                registerButton.onClick.AddListener(HandleRegisterClicked);
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

            if (registerButton != null)
            {
                registerButton.onClick.RemoveListener(HandleRegisterClicked);
            }

            listenersBound = false;
        }
        #endregion

        #region 登录注册
        // 点击登录按钮
        private void HandleLoginClicked()
        {
            // 1.账号服务缺失时不能登录
            if (AccountManager.Instance == null)
            {
                SetMessage("账号服务未初始化。");
                return;
            }

            // 2.读取输入框内容
            string username = usernameInputField != null ? usernameInputField.text : string.Empty;
            string password = passwordInputField != null ? passwordInputField.text : string.Empty;

            // 3.调用账号服务登录
            AccountManager.AccountLoginResult result = AccountManager.Instance.Login(username, password);

            // 4.失败时显示提示
            if (!result.success)
            {
                SetMessage(result.message);
                return;
            }

            // 5.登录成功后清理提示并通知UIRoot进入主菜单
            SetMessage(result.message);
            onLoginSuccessRequested?.Invoke();
        }

        // 点击注册按钮
        private void HandleRegisterClicked()
        {
            // 1.账号服务缺失时不能注册
            if (AccountManager.Instance == null)
            {
                SetMessage("账号服务未初始化。");
                return;
            }

            // 2.读取输入框内容
            string username = usernameInputField != null ? usernameInputField.text : string.Empty;
            string password = passwordInputField != null ? passwordInputField.text : string.Empty;

            // 3.调用账号服务注册
            AccountManager.AccountRegisterResult result = AccountManager.Instance.Register(username, password);

            // 4.显示注册结果
            SetMessage(result.message);
        }
        #endregion

        #region 外部调用
        // 设置登录菜单显隐
        public void SetVisible(bool visible)
        {
            if (loginPanel != null)
            {
                loginPanel.SetActive(visible);
            }
        }
        #endregion

        #region 工具方法
        // 设置提示文本
        private void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }
        #endregion
    }
}
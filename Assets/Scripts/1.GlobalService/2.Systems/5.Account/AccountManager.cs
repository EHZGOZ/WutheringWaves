using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace WutheringWaves
{
    // 账号管理器：负责注册、登录、切换、注销、密码加密
    public class AccountManager : MonoBehaviour
    {
        public static AccountManager Instance { get; private set; }

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true; // 是否输出详细日志

        [Header(" 账号索引文件名")]
        [SerializeField] private string indexFileName = "index.json"; // 存到 accounts/index.json

        public bool IsInitialized { get; private set; } // 是否已初始化
        public bool IsLoggedIn { get; private set; } // 当前是否已登录
        public AccountInfo CurrentAccount { get; private set; } // 当前登录的账号信息

        private string IndexFilePath => Path.Combine(Application.persistentDataPath, "accounts", indexFileName);

        #region 生命周期

        private void Awake()
        {
            // 1.单例
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region 初始化

        // 初始化账号管理器
        public void Initialize()
        {
            // 1.防止重复初始化
            if (IsInitialized)
            {
                return;
            }

            // 2.确保 accounts 目录存在
            string accountsDir = Path.Combine(Application.persistentDataPath, "accounts");
            if (!Directory.Exists(accountsDir))
            {
                Directory.CreateDirectory(accountsDir);
            }

            // 3.如果 index.json 不存在，创建空索引
            if (!File.Exists(IndexFilePath))
            {
                SaveIndex(new AccountIndexData());
            }

            // 4.标记初始化完成
            IsInitialized = true;
            if (verboseLog)
            {
                Debug.Log("[账号管理器] 初始化完成。");
            }

            Debug.Log(Application.persistentDataPath);
        }

        #endregion

        #region 注册 & 登录

        // 注册新账号
        public AccountRegisterResult Register(string username, string password)
        {
            // 1.用户名不能为空
            if (string.IsNullOrWhiteSpace(username))
            {
                return new AccountRegisterResult(false, "用户名为空。");
            }

            // 2.密码不能为空
            if (string.IsNullOrWhiteSpace(password))
            {
                return new AccountRegisterResult(false, "密码为空。");
            }

            // 3.加载已有账号列表
            AccountIndexData index = LoadIndex();

            // 4.检查用户名是否已存在
            if (index.accounts.Any(a => a.username == username))
            {
                return new AccountRegisterResult(false, "用户名已存在。");
            }

            // 5.生成加盐哈希
            string salt = GenerateSalt();
            string passwordHash = HashPassword(password, salt);

            // 6.创建账号信息
            AccountInfo newAccount = new AccountInfo
            {
                username = username,
                passwordHash = passwordHash,
                salt = salt,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // 7.追加到列表并保存
            AccountInfo[] newAccounts = new AccountInfo[index.accounts.Length + 1];
            Array.Copy(index.accounts, newAccounts, index.accounts.Length);
            newAccounts[index.accounts.Length] = newAccount;
            index.accounts = newAccounts;
            SaveIndex(index);

            // 8.创建该账号的存档目录（预留，SaveService会在这里写入save.json）
            string accountDir = Path.Combine(Application.persistentDataPath, "accounts", username);
            if (!Directory.Exists(accountDir))
            {
                Directory.CreateDirectory(accountDir);
            }

            if (verboseLog)
            {
                Debug.Log($"[账号管理器] 账号 '{username}' 注册成功。");
            }

            return new AccountRegisterResult(true, "注册成功。");
        }

        // 登录账号
        public AccountLoginResult Login(string username, string password)
        {
            // 1.加载账号列表
            AccountIndexData index = LoadIndex();

            // 2.查找用户
            AccountInfo account = index.accounts.FirstOrDefault(a => a.username == username);

            if (account == null)
            {
                return new AccountLoginResult(false, "账号不存在。", null);
            }

            // 3.验证密码
            string expectedHash = HashPassword(password, account.salt);

            if (account.passwordHash != expectedHash)
            {
                return new AccountLoginResult(false, "密码错误。", null);
            }

            // 4.更新最近登录时间
            account.lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SaveIndex(index);

            // 5.设置当前登录状态
            CurrentAccount = account;
            IsLoggedIn = true;

            if (verboseLog)
            {
                Debug.Log($"[账号管理器] 账号 '{username}' 登录成功。");
            }

            return new AccountLoginResult(true, "登录成功。", account);
        }

        // 注销当前账号
        public void Logout()
        {
            // 1.清除当前登录状态
            CurrentAccount = null;
            IsLoggedIn = false;

            // 2.通知存档服务清除内存数据
            if (SaveService.Instance != null)
            {
                SaveService.Instance.ClearCurrent();
            }

            if (verboseLog)
            {
                Debug.Log("[账号管理器] 已注销当前账号。");
            }
        }

        #endregion

        #region 账号列表查询

        // 获取所有已注册的账号名
        public string[] GetAllUsernames()
        {
            AccountIndexData index = LoadIndex();
            return index.accounts.Select(a => a.username).ToArray();
        }

        // 判断账号是否存在
        public bool AccountExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            AccountIndexData index = LoadIndex();
            return index.accounts.Any(a => a.username == username);
        }

        #endregion

        #region 文件读写
        // 从硬盘加载账号索引
        private AccountIndexData LoadIndex()
        {
            if (!File.Exists(IndexFilePath))
            {
                return new AccountIndexData();
            }

            try
            {
                string json = File.ReadAllText(IndexFilePath);
                return JsonUtility.FromJson<AccountIndexData>(json) ?? new AccountIndexData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AccountManager] 读取账号索引失败：{e.Message}");
                return new AccountIndexData();
            }
        }

        // 保存账号索引到硬盘
        private void SaveIndex(AccountIndexData index)
        {
            if (index == null)
            {
                return;
            }

            try
            {
                // 确保 accounts 目录存在
                string accountsDir = Path.Combine(Application.persistentDataPath, "accounts");
                if (!Directory.Exists(accountsDir))
                {
                    Directory.CreateDirectory(accountsDir);
                }

                string json = JsonUtility.ToJson(index, true);
                File.WriteAllText(IndexFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AccountManager] 保存账号索引失败：{e.Message}");
            }
        }

        #endregion

        #region 加密工具

        // 生成随机盐值
        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            return Convert.ToBase64String(saltBytes);
        }

        // 密码加盐哈希（SHA256）
        private string HashPassword(string password, string salt)
        {
            string saltedPassword = salt + password;
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(bytes);
        }

        #endregion

        #region 结果数据结构

        // 注册结果
        public struct AccountRegisterResult
        {
            public bool success;
            public string message;

            public AccountRegisterResult(bool success, string message)
            {
                this.success = success;
                this.message = message;
            }
        }

        // 登录结果
        public struct AccountLoginResult
        {
            public bool success;
            public string message;
            public AccountInfo account;

            public AccountLoginResult(bool success, string message, AccountInfo account)
            {
                this.success = success;
                this.message = message;
                this.account = account;
            }
        }

        #endregion
    }


}
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WutheringWaves
{
    // 账号管理器：负责注册、登录、注销，通过仓储接口存取账号数据
    public class AccountManager : MonoBehaviour
    {
        public static AccountManager Instance { get; private set; }

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;

        [Header(" 账号索引文件名")]
        [SerializeField] private string indexFileName = "index.json";

        public bool IsInitialized { get; private set; }
        public bool IsLoggedIn { get; private set; }
        public AccountInfo CurrentAccount { get; private set; }

        // 抽象仓储接口，不关心底层是 JSON 还是 SQLite
        private IAccountRepository _repository;

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
        }
        private void OnDestroy()
        {
            // 1.如果销毁的是当前单例，清空单例引用
            if (Instance == this)
            {
                Instance = null;
            }
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

            // 2.创建 JSON 仓储（将来切 SQLite 只改这一行）
            _repository = new JsonAccountRepository(Application.persistentDataPath, indexFileName);

            // 3.如果数据文件不存在，写一个空索引
            if (!_repository.Exists())
            {
                _repository.Save(new AccountIndexData());
            }

            // 4.标记初始化完成
            IsInitialized = true;

            if (verboseLog)
            {
                Debug.Log("[账号管理器] 初始化完成。"+ "持久化数据路径" + Application.persistentDataPath);
            }
      
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
            AccountIndexData index = _repository.Load();

            // 4.检查用户名是否已存在
            if (index.accounts.Any(account => account.username == username))
            {
                return new AccountRegisterResult(false, "用户名已存在。");
            }

            // 5.用加密工具生成盐和哈希
            string salt = AccountCrypto.GenerateSalt();
            string passwordHash = AccountCrypto.HashPassword(password, salt);

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
            _repository.Save(index);

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
            AccountIndexData index = _repository.Load();

            // 2.查找用户
            AccountInfo account = index.accounts.FirstOrDefault(a => a.username == username);

            if (account == null)
            {
                return new AccountLoginResult(false, "账号不存在。", null);
            }

            // 3.用加密工具验证密码
            if (!AccountCrypto.VerifyPassword(password, account.salt, account.passwordHash))
            {
                return new AccountLoginResult(false, "密码错误。", null);
            }

            // 4.更新最近登录时间
            account.lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _repository.Save(index);

            // 5.设置当前登录状态
            CurrentAccount = account;
            IsLoggedIn = true;

            if (verboseLog)
            {
                Debug.Log($"[账号管理器] 账号 '{username}' 登录成功。");
            }

            return new AccountLoginResult(true, "登录成功。", account);
        }

        // 退出当前账号登录
        public void Logout()
        {
            // 1.清除当前登录状态，但不删除账号数据
            CurrentAccount = null;
            IsLoggedIn = false;

            // 2.通知存档服务清除当前内存存档，避免切换账号时误用旧账号数据
            if (SaveService.Instance != null)
            {
                SaveService.Instance.ClearCurrent();
            }

            if (verboseLog)
            {
                Debug.Log("[账号管理器] 已退出当前账号登录。");
            }
        }

        #endregion

        #region 账号列表查询

        // 获取所有已注册的账号名
        public string[] GetAllUsernames()
        {
            AccountIndexData index = _repository.Load();
            return index.accounts.Select(a => a.username).ToArray();
        }

        // 判断账号是否存在
        public bool AccountExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            AccountIndexData index = _repository.Load();
            return index.accounts.Any(a => a.username == username);
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
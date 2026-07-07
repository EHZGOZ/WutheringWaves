using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    // 存档服务：负责按账号路径创建、读取、保存和删除存档
    public class SaveService : MonoBehaviour
    {
        public static SaveService Instance { get; private set; }

        [Header("=== 存档路径配置 ===")]
        [SerializeField] private string saveFileName = "save.json"; // 存档文件名（存到 accounts/用户名/ 下）

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;  // 是否输出详细日志

        public bool IsInitialized { get; private set; } // 是否已初始化
        public SaveData CurrentData { get; private set; } // 当前内存中的存档数据
        public string CurrentUsername { get; private set; } = string.Empty; // 当前存档属于哪个账号

        private JsonSaveRepository _jsonRepository; // 当前槽位对应的JSON存档仓储对象
        public PlayerPrefsSettingsRepository SettingsRepository { get; private set; } // 轻量级设置仓储

        #region 生命周期
        private void Awake()
        {
            // 1.保持单例，避免多个SaveService争抢存档状态
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 2.缓存单例引用
            Instance = this;

        }
        #endregion

        #region 初始化
        // 初始化存档服务
        public void Initialize()
        {
            // 1.已经初始化过时直接返回，避免重复初始化清空当前存档状态
            if (IsInitialized)
            {
                return;
            }

            // 2.初始化轻量级设置仓储
            SettingsRepository = new PlayerPrefsSettingsRepository();

            // 3.清空当前存档状态，等待账号登录后绑定
            _jsonRepository = null;
            CurrentData = null;
            CurrentUsername = string.Empty;

            // 4.标记初始化完成
            IsInitialized = true;

            // 5.打印初始化日志
            if (verboseLog)
            {
                Debug.Log("[存档服务] 初始化完成，等待账号登录。");
            }
        }
        #endregion

        #region 新建存档 保存存档  读取存档 删除存档
        // 为指定账号新建存档
        public SaveData CreateSave(string username, SaveData saveData)
        {
            // 1.用户名为空时不创建
            if (string.IsNullOrWhiteSpace(username))
            {
                Debug.Log("[存档服务] 新建存档失败：用户名为空。");
                return null;
            }

            // 2.传入存档数据为空时不创建，避免保存无效账号存档
            if (saveData == null)
            {
                Debug.Log("[存档服务] 新建存档失败：存档数据为空。");
                return null;
            }

            // 3.记录当前账号
            CurrentUsername = username;

            // 4.根据用户名创建对应JSON仓储
            _jsonRepository = CreateRepository(username);

            // 5.缓存外部创建好的默认存档数据，SaveService只负责保存和维护当前存档状态
            CurrentData = saveData;

            // 6.保存默认存档到本地
            if (_jsonRepository.Save(CurrentData))
            {
                if (verboseLog)
                {
                    Debug.Log($"[存档服务] 账号 '{username}' 新建存档成功。");
                }

                return CurrentData;
            }

            // 7.保存失败时清空当前数据
            CurrentData = null;
            CurrentUsername = string.Empty;

            if (verboseLog)
            {
                Debug.Log($"[存档服务] 账号 '{username}' 新建存档失败。");
            }

            return null;
        }

        // 保存当前正在使用的存档：用于游戏内主动保存、退出前兜底保存
        public bool SaveCurrentSave()
        {
            // 1.当前没有存档数据时不保存
            if (CurrentData == null)
            {
                Debug.Log("[存档服务] 保存失败：当前存档数据为空。");
                return false;
            }

            // 2.没有登录账号时不保存
            if (string.IsNullOrWhiteSpace(CurrentUsername))
            {
                Debug.Log("[存档服务] 保存失败：未绑定账号。");
                return false;
            }

            // 3.兜底创建当前账号的仓储
            if (_jsonRepository == null)
            {
                _jsonRepository = CreateRepository(CurrentUsername);
            }

            // 4.保存当前内存中的存档数据
            bool ok = _jsonRepository.Save(CurrentData);
            if (verboseLog)
            {
                if (ok)
                {
                    Debug.Log($"[存档服务] 账号 '{CurrentUsername}' 当前存档保存成功。");
                }
                else
                {
                    Debug.Log($"[存档服务] 账号 '{CurrentUsername}' 当前存档保存失败。");
                }
            }

            return ok;
        }

        // 读取指定账号存档：用于已有账号进入游戏
        public SaveData LoadSave(string username)
        {
            // 1.用户名为空时取消读取
            if (string.IsNullOrWhiteSpace(username))
            {
                Debug.Log("[存档服务] 读取存档失败：用户名为空。");
                return null;
            }

            // 2.记录当前账号
            CurrentUsername = username;

            // 3.根据用户名创建对应JSON仓储
            _jsonRepository = CreateRepository(username);

            // 4.尝试加载本地存档
            if (_jsonRepository.TryLoad(out SaveData data))
            {
                CurrentData = data;

                if (verboseLog)
                {
                    Debug.Log($"[存档服务] 账号 '{username}' 存档读取成功。");
                }

                return CurrentData;
            }

            // 5.没有存档时不自动创建，返回null让上层决定是新建还是提示
            CurrentData = null;
            CurrentUsername = string.Empty;

            if (verboseLog)
            {
                Debug.Log($"[存档服务] 账号 '{username}' 没有存档，将创建新存档。");
            }

            return null;
        }

        // 删除指定账号的存档
        public bool DeleteSave(string username)
        {
            // 1.用户名为空时不删除
            if (string.IsNullOrWhiteSpace(username))
            {
                Debug.Log("[存档服务] 删除存档失败：用户名为空。");
                return false;
            }

            // 2.根据用户名创建对应JSON仓储
            JsonSaveRepository repository = CreateRepository(username);

            // 3.删除本地JSON存档文件
            if (repository.Delete())
            {
                // 4.如果删除的是当前账号的存档，清空当前内存数据
                if (CurrentUsername == username)
                {
                    CurrentData = null;
                    CurrentUsername = string.Empty;
                    _jsonRepository = null;
                }

                if (verboseLog)
                {
                    Debug.Log($"[存档服务] 账号 '{username}' 存档删除成功。");
                }

                return true;
            }

            if (verboseLog)
            {
                Debug.Log($"[存档服务] 账号 '{username}' 没有找到可删除的存档。");
            }

            return false;
        }
        #endregion

        #region 存档查询
        // 判断指定账号是否有存档
        public bool HasSave(string username)
        {
            // 1.用户名为空时视为没有存档
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            // 2.通过仓储判断存档文件是否存在
            JsonSaveRepository repo = CreateRepository(username);
            return repo.Exists();
        }
        // 清除当前内存中的存档数据（切换账号时使用）
        public void ClearCurrent()
        {
            CurrentData = null;
            CurrentUsername = string.Empty;
            _jsonRepository = null;
        }

        #endregion

        #region 工具方法
        // 根据用户名创建对应仓储，路径：{persistentDataPath}/accounts/{username}/save.json
        private JsonSaveRepository CreateRepository(string username)
        {
            string accountDir = Path.Combine(Application.persistentDataPath, "accounts", username);
            return new JsonSaveRepository(Path.Combine(accountDir, saveFileName));
        }
        #endregion

    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    // 存档服务：负责初始化仓库、加载默认存档和统一保存入口
    public class SaveService : MonoBehaviour
    {
        // 存档文件名
        [SerializeField] private string saveFileName = "save.json";
        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;  // 是否输出详细日志
        public bool IsInitialized { get; private set; }// 是否已初始化  
        public SaveData CurrentData { get; private set; }  // 当前内存中的存档数据

        private JsonSaveRepository _jsonRepository;// JSON存档仓储对象
        public PlayerPrefsSettingsRepository SettingsRepository { get; private set; } // 轻量级设置仓储

        // 初始化服务
        public void Initialize()
        {
            // 防止重复初始化
            if (IsInitialized)
            {
                return;
            }

            // 创建存档仓储实例
            _jsonRepository = new JsonSaveRepository(saveFileName);
            SettingsRepository = new PlayerPrefsSettingsRepository();
            IsInitialized = true;

            // 打印初始化日志
            if (verboseLog)
            {
                Debug.Log($"[存档服务] 初始化完成。路径：{_jsonRepository.SavePath}");
            }
        }

        // 加载存档，无存档则创建默认存档
        public SaveData LoadOrCreate()
        {
            // 确保服务已初始化
            EnsureInitialized();

            // 尝试加载本地存档
            if (_jsonRepository.TryLoad(out SaveData data))
            {
                CurrentData = data;
                if (verboseLog)
                {
                    Debug.Log("[存档服务] 存档加载成功。");
                }
            }
            else
            {
                // 无存档时创建默认数据
                CurrentData = SaveData.CreateDefault();
                // 记录当前场景名
                CurrentData.sceneName = SceneManager.GetActiveScene().name;
                // 保存默认存档
                _jsonRepository.Save(CurrentData);
                if (verboseLog)
                {
                    Debug.Log("[存档服务] 未找到存档，已创建默认存档。");
                }
            }

            return CurrentData;
        }

        // 保存指定的存档数据
        public bool Save(SaveData data)
        {
            EnsureInitialized();

            // 空数据不保存
            if (data == null)
            {
                return false;
            }

            // 同步当前场景名称
            data.sceneName = SceneManager.GetActiveScene().name;
            bool ok = _jsonRepository.Save(data);

            // 保存成功后更新当前数据
            if (ok)
            {
                CurrentData = data;
                if (verboseLog)
                {
                    Debug.Log("[存档服务] 存档保存成功。");
                }
            }

            return ok;
        }

        // 快速保存当前内存中的数据
        public bool SaveCurrent()
        {
            // 当前数据为空则创建默认数据
            if (CurrentData == null)
            {
                CurrentData = SaveData.CreateDefault();
            }

            return Save(CurrentData);
        }

        // 校验初始化状态，未初始化则自动初始化
        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
        }
    }
}
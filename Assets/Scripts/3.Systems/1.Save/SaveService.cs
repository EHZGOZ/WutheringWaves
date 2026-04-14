using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    // 存档服务：负责初始化仓库、加载默认存档和统一保存入口
    public class SaveService : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "save.json"; // 存档文件名
        [SerializeField] private bool verboseLog = true; // 是否输出详细日志

        private JsonSaveRepository _jsonRepository; // Json存档仓库

        public bool IsInitialized { get; private set; } // 是否已完成初始化
        public SaveData CurrentData { get; private set; } // 当前运行中的存档数据
        public PlayerPrefsSettingsRepository SettingsRepository { get; private set; } // 轻量设置仓库

        public void Initialize()
        {
            // 只初始化一次，避免重复创建仓库对象。
            if (IsInitialized)
            {
                return;
            }

            _jsonRepository = new JsonSaveRepository(saveFileName);
            SettingsRepository = new PlayerPrefsSettingsRepository();
            IsInitialized = true;

            if (verboseLog)
            {
                Debug.Log($"[SaveService] Initialized. Path: {_jsonRepository.SavePath}");
            }
        }

        public SaveData LoadOrCreate()
        {
            // 加载前确保仓库已经准备完成。
            EnsureInitialized();

            if (_jsonRepository.TryLoad(out SaveData data))
            {
                CurrentData = data;
                if (verboseLog)
                {
                    Debug.Log("[SaveService] Save loaded.");
                }
            }
            else
            {
                // 首次启动没有存档时，自动创建一份默认存档。
                CurrentData = SaveData.CreateDefault();
                CurrentData.sceneName = SceneManager.GetActiveScene().name;
                _jsonRepository.Save(CurrentData);
                if (verboseLog)
                {
                    Debug.Log("[SaveService] No save found. Created default save.");
                }
            }

            return CurrentData;
        }

        public bool Save(SaveData data)
        {
            EnsureInitialized();
            // 空数据不参与保存，避免覆盖掉已有有效内容。
            if (data == null)
            {
                return false;
            }

            // 保存前同步当前场景名，保证场景状态一致。
            data.sceneName = SceneManager.GetActiveScene().name;
            bool ok = _jsonRepository.Save(data);
            if (ok)
            {
                CurrentData = data;
                if (verboseLog)
                {
                    Debug.Log("[SaveService] Save successful.");
                }
            }

            return ok;
        }

        public bool SaveCurrent()
        {
            // 当前无缓存存档时，先补一份默认对象再保存。
            if (CurrentData == null)
            {
                CurrentData = SaveData.CreateDefault();
            }

            return Save(CurrentData);
        }

        private void EnsureInitialized()
        {
            // 对外接口统一支持懒初始化，减少场景初始化耦合。
            if (!IsInitialized)
            {
                Initialize();
            }
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    // 存档服务：负责初始化仓库、加载默认存档和统一保存入口
    public class SaveService : MonoBehaviour
    {
        public static SaveService Instance { get; private set; }
        // 存档文件名
        [SerializeField] private string saveFileName = "save.json";
        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;  // 是否输出详细日志

        public bool IsInitialized { get; private set; }// 是否已初始化  
        public SaveData CurrentData { get; private set; }  // 当前内存中的存档数据


        private JsonSaveRepository _jsonRepository;// JSON存档仓储对象
        public PlayerPrefsSettingsRepository SettingsRepository { get; private set; } // 轻量级设置仓储

        #region 初始化
        //初始化
        public void Initialize()
        {
            // 单例模式核心：如果已存在实例，销毁当前重复对象
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this; // 赋值单例实例
            DontDestroyOnLoad(gameObject); // 场景切换时，不销毁该引导对象

            //2创建存档仓储实例
            _jsonRepository = new JsonSaveRepository(saveFileName);
            SettingsRepository = new PlayerPrefsSettingsRepository();
            IsInitialized = true;

            //3.打印初始化日志
            if (verboseLog)
                Debug.Log($"[存档服务] 初始化完成。路径：{_jsonRepository.SavePath}");
        }
        #endregion

        #region 读档与存档
        // 加载存档，无存档则创建默认存档
        public SaveData Load()
        {
            // 尝试加载本地存档
            if (_jsonRepository.TryLoad(out SaveData data))
            {
                CurrentData = data;

                if (verboseLog)
                    Debug.Log("[存档服务] 存档加载成功。");
            }
            else
            {
                // 无存档时创建默认数据
                CurrentData = SaveData.CreateDefault();
                GameBootstrap.Instance.EnsureDefaultTeam(CurrentData);
                // 记录当前场景名
                CurrentData.sceneName = SceneManager.GetActiveScene().name;
                // 保存默认存档
                _jsonRepository.Save(CurrentData);

                if (verboseLog)
                    Debug.Log("[存档服务] 未找到存档，已创建默认存档。");
            }

            return CurrentData;
        }

        // 保存指定的存档数据
        public void Save(SaveData data)
        {
            //1.空数据不保存
            if (data == null)
            {
                Debug.Log("[存档服务] 空数据 存档保存失败。");
                return;
            }
            //2.保存
            if (_jsonRepository.Save(data))
            {
                CurrentData = data;
                if (verboseLog)
                {
                    Debug.Log("[存档服务] 存档保存成功。");
                }
            }
            else
            {
                if (verboseLog)
                {
                    Debug.Log("[存档服务] 存档保存失败。");
                }
            }
        }

        // 删除当前本地存档文件
        public bool DeleteSave()
        {
            //1.仓储未初始化时不删除
            if (_jsonRepository == null)
            {
                Debug.Log("[存档服务] 删除存档失败：存档仓储未初始化。");
                return false;
            }

            //2.删除本地JSON存档文件
            if (_jsonRepository.Delete())
            {
                CurrentData = null;

                if (verboseLog)
                {
                    Debug.Log("[存档服务] 存档删除成功。");
                }

                return true;
            }

            if (verboseLog)
            {
                Debug.Log("[存档服务] 没有找到可删除的存档。");
            }

            return false;
        }

        // 重置存档：删除旧存档后重新创建一份默认存档
        public SaveData ResetSave()
        {
            //1.先删除旧存档，没有旧存档也允许继续创建默认存档
            DeleteSave();

            //2.创建默认存档数据
            CurrentData = SaveData.CreateDefault();

            //3.补齐默认队伍，避免默认存档队伍为空
            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.EnsureDefaultTeam(CurrentData);
            }

            //4.保存新的默认存档
            Save(CurrentData);

            return CurrentData;
        }
        #endregion


    }
}

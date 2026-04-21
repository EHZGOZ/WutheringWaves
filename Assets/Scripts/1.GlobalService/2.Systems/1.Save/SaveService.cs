using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    // 存档服务：负责初始化仓库、管理三个存档槽的新建、读取和删除
    public class SaveService : MonoBehaviour
    {
        public static SaveService Instance { get; private set; }

        [Header("=== 存档槽配置 ===")]
        [SerializeField] private string saveFilePrefix = "save_slot_"; // 存档文件名前缀
        [SerializeField] private string saveFileExtension = ".json"; // 存档文件后缀
        [SerializeField] private int saveSlotCount = 3; // 存档槽数量

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;  // 是否输出详细日志

        public bool IsInitialized { get; private set; } // 是否已初始化
        public SaveData CurrentData { get; private set; } // 当前内存中的存档数据
        public int CurrentSlotIndex { get; private set; } = -1; // 当前正在使用的存档槽位

        private JsonSaveRepository _jsonRepository; // 当前槽位对应的JSON存档仓储对象
        public PlayerPrefsSettingsRepository SettingsRepository { get; private set; } // 轻量级设置仓储

        #region 初始化
        // 初始化
        public void Initialize()
        {
            // 单例模式核心：如果已存在实例，销毁当前重复对象
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this; // 赋值单例实例
            DontDestroyOnLoad(gameObject); // 场景切换时，不销毁该服务对象

            // 1.初始化轻量级设置仓储
            SettingsRepository = new PlayerPrefsSettingsRepository();

            // 2.清空当前存档状态，等待UI选择具体槽位
            _jsonRepository = null;
            CurrentData = null;
            CurrentSlotIndex = -1;

            // 3.标记初始化完成
            IsInitialized = true;

            // 4.打印初始化日志
            if (verboseLog)
            {
                Debug.Log("[存档服务] 初始化完成，等待选择存档槽位。");
            }
        }
        #endregion

        #region 新建存档 读取存档 删除存档
        // 新建指定槽位存档：用于空槽位创建新游戏
        public SaveData CreateSave(int slotIndex)
        {
            // 1.槽位非法时不创建
            if (!IsValidSlotIndex(slotIndex))
            {
                Debug.Log($"[存档服务] 新建存档失败：槽位索引非法。slotIndex = {slotIndex}");
                return null;
            }

            // 2.记录当前使用的槽位
            CurrentSlotIndex = slotIndex;

            // 3.根据槽位创建对应JSON仓储
            _jsonRepository = CreateRepository(slotIndex);

            // 4.创建默认存档数据
            CurrentData = SaveData.CreateDefault();

            // 5.确保默认存档里有默认队伍，避免新存档没有角色
            GameBootstrap.Instance?.EnsureDefaultTeam(CurrentData);

            // 6.记录当前场景名
            CurrentData.sceneName = SceneManager.GetActiveScene().name;

            // 7.保存默认存档到本地
            if (_jsonRepository.Save(CurrentData))
            {
                if (verboseLog)
                {
                    Debug.Log($"[存档服务] 槽位 {slotIndex + 1} 新建存档成功。");
                }

                return CurrentData;
            }

            // 8.保存失败时清空当前数据，避免内存里保留无效存档
            CurrentData = null;
            CurrentSlotIndex = -1;

            if (verboseLog)
            {
                Debug.Log($"[存档服务] 槽位 {slotIndex + 1} 新建存档失败。");
            }

            return null;
        }

        // 读取指定槽位存档：用于已有存档进入游戏
        public SaveData LoadSave(int slotIndex)
        {
            // 1.槽位非法时不读档
            if (!IsValidSlotIndex(slotIndex))
            {
                Debug.Log($"[存档服务] 读取存档失败：槽位索引非法。slotIndex = {slotIndex}");
                return null;
            }

            // 2.记录当前使用的槽位
            CurrentSlotIndex = slotIndex;

            // 3.根据槽位创建对应JSON仓储
            _jsonRepository = CreateRepository(slotIndex);

            // 4.尝试加载本地存档
            if (_jsonRepository.TryLoad(out SaveData data))
            {
                CurrentData = data;

                if (verboseLog)
                {
                    Debug.Log($"[存档服务] 槽位 {slotIndex + 1} 存档读取成功。");
                }

                return CurrentData;
            }

            // 5.读取失败时不自动创建，避免“读档按钮”误生成新存档
            CurrentData = null;
            CurrentSlotIndex = -1;

            if (verboseLog)
            {
                Debug.Log($"[存档服务] 槽位 {slotIndex + 1} 没有可读取的存档。");
            }

            return null;
        }

        // 删除指定槽位存档
        public bool DeleteSave(int slotIndex)
        {
            // 1.槽位非法时不删除
            if (!IsValidSlotIndex(slotIndex))
            {
                Debug.Log($"[存档服务] 删除存档失败：槽位索引非法。slotIndex = {slotIndex}");
                return false;
            }

            // 2.根据槽位创建对应JSON仓储
            JsonSaveRepository repository = CreateRepository(slotIndex);

            // 3.删除本地JSON存档文件
            if (repository.Delete())
            {
                // 4.如果删除的是当前正在使用的槽位，就清空当前数据
                if (CurrentSlotIndex == slotIndex)
                {
                    CurrentData = null;
                    CurrentSlotIndex = -1;
                    _jsonRepository = null;
                }

                if (verboseLog)
                {
                    Debug.Log($"[存档服务] 槽位 {slotIndex + 1} 存档删除成功。");
                }

                return true;
            }

            if (verboseLog)
            {
                Debug.Log($"[存档服务] 槽位 {slotIndex + 1} 没有找到可删除的存档。");
            }

            return false;
        }
        #endregion

        #region 保存当前存档
        // 保存当前正在使用的存档：用于游戏内主动保存、退出前兜底保存
        public bool SaveCurrentSave()
        {
            // 1.当前没有存档数据时不保存
            if (CurrentData == null)
            {
                Debug.Log("[存档服务] 保存当前存档失败：当前存档数据为空。");
                return false;
            }

            // 2.当前槽位非法时不保存，避免写错存档槽
            if (!IsValidSlotIndex(CurrentSlotIndex))
            {
                Debug.Log($"[存档服务] 保存当前存档失败：当前槽位非法。CurrentSlotIndex = {CurrentSlotIndex}");
                return false;
            }

            // 3.兜底创建当前槽位仓储，避免读取流程异常导致仓储为空
            if (_jsonRepository == null)
            {
                _jsonRepository = CreateRepository(CurrentSlotIndex);
            }

            // 4.保存当前内存中的存档数据
            bool ok = _jsonRepository.Save(CurrentData);
            if (verboseLog)
            {
                if (ok)
                {
                    Debug.Log($"[存档服务] 槽位 {CurrentSlotIndex + 1} 当前存档保存成功。");
                }
                else
                {
                    Debug.Log($"[存档服务] 槽位 {CurrentSlotIndex + 1} 当前存档保存失败。");
                }
            }

            return ok;
        }
        #endregion

        #region 保存到指定槽位
        // 保存到指定槽位：用于游戏中主动存档，可以覆盖已有存档，但不切换当前正在玩的槽位
        public bool SaveToSlot(int slotIndex, SaveData data)
        {
            // 1.槽位非法时不保存
            if (!IsValidSlotIndex(slotIndex))
            {
                Debug.Log($"[存档服务] 保存到指定槽位失败：槽位索引非法。slotIndex = {slotIndex}");
                return false;
            }

            // 2.存档数据为空时不保存
            if (data == null)
            {
                Debug.Log("[存档服务] 保存到指定槽位失败：存档数据为空。");
                return false;
            }

            // 3.创建目标槽位仓储，只写目标槽，不改变当前正在使用的槽位
            JsonSaveRepository targetRepository = CreateRepository(slotIndex);

            // 4.保存到目标槽位，已有文件会被覆盖
            bool ok = targetRepository.Save(data);

            if (verboseLog)
            {
                if (ok)
                {
                    Debug.Log($"[存档服务] 槽位 {slotIndex + 1} 主动存档成功。当前游玩槽位仍然是 {CurrentSlotIndex + 1}。");
                }
                else
                {
                    Debug.Log($"[存档服务] 槽位 {slotIndex + 1} 主动存档失败。");
                }
            }

            return ok;
        }


        #endregion

        #region 存档槽查询
        // 判断指定槽位是否已有存档
        public bool HasSave(int slotIndex)
        {
            // 1.槽位非法时视为没有存档
            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            // 2.尝试读取该槽位，能读到数据就说明有存档
            JsonSaveRepository repository = CreateRepository(slotIndex);
            return repository.TryLoad(out SaveData data) && data != null;
        }
        #endregion

        #region 工具方法
        // 根据槽位索引创建对应仓储
        private JsonSaveRepository CreateRepository(int slotIndex)
        {
            return new JsonSaveRepository(GetSaveFileName(slotIndex));
        }

        // 根据槽位索引生成存档文件名
        private string GetSaveFileName(int slotIndex)
        {
            return $"{saveFilePrefix}{slotIndex}{saveFileExtension}";
        }

        // 判断槽位索引是否合法
        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < saveSlotCount;
        }
        #endregion
    }
}

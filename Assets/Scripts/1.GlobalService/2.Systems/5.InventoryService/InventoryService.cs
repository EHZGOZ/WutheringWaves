using UnityEngine;

namespace WutheringWaves
{
    // 背包服务：负责管理当前账号存档中的背包数据
    public class InventoryService : MonoBehaviour
    {
        public static InventoryService Instance { get; private set; }

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;

        #region 核心引用
        private InventoryData currentInventory; // 当前账号存档中的背包数据引用
        #endregion

        #region 外部访问
        public bool IsInitialized { get; private set; }

        public InventoryData CurrentInventory => currentInventory; // 外部只读访问当前背包数据
        #endregion

        #region 生命周期
        private void Awake()
        {
            // 1.保持单例，避免多个InventoryService争抢当前背包数据
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
        public void Initialize()
        {
            // 1.防止重复初始化
            if (IsInitialized)
            {
                return;
            }

            // 2.标记初始化完成
            IsInitialized = true;

            if (verboseLog)
            {
                Debug.Log("[背包服务] 初始化完成，等待存档数据绑定。");
            }
        }
        #endregion

        #region 背包绑定与清理
        // 绑定当前存档中的背包数据：登录/读档成功后由外部调用
        public void Bind(SaveData saveData)
        {
            // 1.存档数据为空时停止绑定，并清理旧账号背包引用
            // 避免绑定失败后继续操作上一个账号的背包数据
            if (saveData == null)
            {
                currentInventory = null;
                Debug.LogWarning("[背包服务] 背包绑定失败：存档数据为空。", this);
                return;
            }

            // 2.存档中的背包数据为空时创建空背包
            // 创建后写回SaveData，保证后续背包变化能够跟随存档一起保存
            if (saveData.inventory == null)
            {
                saveData.inventory = new InventoryData();
            }

            // 3.背包物品列表为空时创建空列表
            // 兼容旧存档或字段不完整的异常存档，避免后续增删物品时发生空引用
            if (saveData.inventory.items == null)
            {
                saveData.inventory.items = new();
            }

            // 4.缓存当前背包数据引用
            // currentInventory与saveData.inventory指向同一个对象，不需要额外复制数据
            currentInventory = saveData.inventory;

            // 5.输出绑定日志，方便确认账号存档是否已经接入背包服务
            if (verboseLog)
            {
                Debug.Log("[背包服务] 当前存档背包数据绑定完成。");
            }
        }

        // 清理当前背包数据引用：注销账号或切换账号时调用
        public void Clear()
        {
            // 1.清空当前背包引用，避免下一个账号误用旧账号的背包数据
            currentInventory = null;

            // 2.输出清理日志，方便排查账号切换流程
            if (verboseLog)
            {
                Debug.Log("[背包服务] 当前背包数据已清理。");
            }
        }
        #endregion

    }
}
using UnityEngine;

namespace WutheringWaves
{
    // 背包服务：负责管理当前账号存档中的背包数据
    public class InventoryService : MonoBehaviour
    {
        public static InventoryService Instance { get; private set; }

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;

        public bool IsInitialized { get; private set; }

        private InventoryData currentInventory; // 当前账号存档中的背包数据引用
        public InventoryData CurrentInventory => currentInventory; // 外部只读访问当前背包数据

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

        #region 绑定与清理
        // 绑定当前存档中的背包数据：登录/读档成功后由外部调用
        public void Bind(InventoryData inventoryData)
        {
            // 1.如果传入的背包数据为空，则创建一份空背包，避免后续访问空引用
            if (inventoryData == null)
            {
                inventoryData = new InventoryData();
            }

            // 2.缓存当前背包数据引用，后续所有背包操作都基于这份数据
            currentInventory = inventoryData;

            // 3.输出绑定日志，方便确认账号存档是否已经接入背包服务
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
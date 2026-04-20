using System;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    // 存档菜单控制器：负责三个存档槽的显示和按钮请求转发
    public class SavedGameMenuController : MonoBehaviour
    {
        [Header("存档菜单面板")]
        [SerializeField] private GameObject savedGamePanel; // 存档菜单面板

        [Header("存档槽位UI")]
        [SerializeField] private SaveSlotUI[] slotUIs; // 三个存档槽UI

        [Header("返回按钮")]
        [SerializeField] private Button backButton; // 返回主菜单按钮

        private Action<int> onCreateSaveRequested; // 新建存档请求
        private Action<int> onLoadSaveRequested; // 读取存档请求
        private Action<int> onDeleteSaveRequested; // 删除存档请求
        private Action onBackRequested; // 返回主菜单请求

        private bool initialized; // 是否已初始化
        private bool listenersBound; // 是否已绑定按钮事件

        #region 生命周期
        private void OnEnable()
        {
            if (initialized)
            {
                BindListeners();
                RefreshSlots();
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

        #region 初始化
        // 初始化存档菜单：由UIRoot传入各类请求回调
        public void Initialize(
            Action<int> createSaveRequested,
            Action<int> loadSaveRequested,
            Action<int> deleteSaveRequested,
            Action backRequested)
        {
            // 1.缓存外部流程回调
            onCreateSaveRequested = createSaveRequested;
            onLoadSaveRequested = loadSaveRequested;
            onDeleteSaveRequested = deleteSaveRequested;
            onBackRequested = backRequested;

            // 2.自动补齐槽位引用
            ResolveSlotUIs();

            // 3.初始化每个槽位
            InitializeSlots();

            // 4.绑定返回按钮事件
            BindListeners();

            // 5.标记初始化完成
            initialized = true;
        }
        // 自动补齐存档槽位UI
        private void ResolveSlotUIs()
        {
            if (slotUIs == null || slotUIs.Length == 0)
            {
                slotUIs = GetComponentsInChildren<SaveSlotUI>(true);
            }
        }
        // 初始化三个槽位
        private void InitializeSlots()
        {
            if (slotUIs == null)
            {
                return;
            }

            for (int i = 0; i < slotUIs.Length; i++)
            {
                SaveSlotUI slotUI = slotUIs[i];
                if (slotUI == null)
                {
                    continue;
                }

                slotUI.Initialize(
                    i,
                    HandleCreateSaveRequested,
                    HandleLoadSaveRequested,
                    HandleDeleteSaveRequested);
            }
        }
        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBackClicked);
            }

            listenersBound = true;
        }
        //解绑
        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackClicked);
            }

            listenersBound = false;
        }

        private void HandleCreateSaveRequested(int slotIndex)
        {
            onCreateSaveRequested?.Invoke(slotIndex);
        }

        private void HandleLoadSaveRequested(int slotIndex)
        {
            onLoadSaveRequested?.Invoke(slotIndex);
        }

        private void HandleDeleteSaveRequested(int slotIndex)
        {
            onDeleteSaveRequested?.Invoke(slotIndex);
        }

        private void HandleBackClicked()
        {
            onBackRequested?.Invoke();
        }

        #endregion

        #region 外部调用
        // 设置存档菜单显隐
        public void SetVisible(bool visible)
        {
            if (savedGamePanel != null)
            {
                savedGamePanel.SetActive(visible);
            }

            if (visible)
            {
                RefreshSlots();
            }
        }

        // 刷新所有槽位显示
        public void RefreshSlots()
        {
            ResolveSlotUIs();

            for (int i = 0; i < slotUIs.Length; i++)
            {
                SaveSlotUI slotUI = slotUIs[i];
                if (slotUI == null)
                {
                    continue;
                }

                // 根据真实本地存档文件刷新槽位状态
                bool hasSave = SaveService.Instance != null && SaveService.Instance.HasSave(i);

                slotUI.Refresh(hasSave);
            }
        }
        #endregion


        
    }
}

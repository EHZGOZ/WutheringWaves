using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    // 单个存档槽UI：只负责显示槽位状态和转发按钮点击事件
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("槽位文本")]
        [SerializeField] private TextMeshProUGUI slotTitleText; // 槽位标题文本
        [SerializeField] private TextMeshProUGUI slotStateText; // 槽位状态文本

        [Header("新建存档")]
        [SerializeField] private Button createButton; // 新建存档按钮
        [Header("读取存档")]
        [SerializeField] private Button loadButton; // 读取存档按钮
        [Header("删除存档")]
        [SerializeField] private Button deleteButton; // 删除存档按钮

        private int slotIndex; // 当前槽位索引
        private Action<int> onCreateRequested; // 新建存档请求
        private Action<int> onLoadRequested; // 读取存档请求
        private Action<int> onDeleteRequested; // 删除存档请求

        private bool listenersBound; // 是否已经绑定按钮事件

        #region 生命周期
        private void OnEnable()
        {
            BindListeners();
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
        // 初始化槽位：由SavedGameMenuController传入槽位编号和按钮回调
        public void Initialize(
            int slotIndex,
            Action<int> createRequested,
            Action<int> loadRequested,
            Action<int> deleteRequested)
        {
            this.slotIndex = slotIndex;
            onCreateRequested = createRequested;
            onLoadRequested = loadRequested;
            onDeleteRequested = deleteRequested;

            BindListeners();
        }
        #endregion

        #region 按钮监听
        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (createButton != null)
            {
                createButton.onClick.AddListener(HandleCreateClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(HandleLoadClicked);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(HandleDeleteClicked);
            }

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            if (createButton != null)
            {
                createButton.onClick.RemoveListener(HandleCreateClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(HandleLoadClicked);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(HandleDeleteClicked);
            }

            listenersBound = false;
        }

        private void HandleCreateClicked()
        {
            onCreateRequested?.Invoke(slotIndex);
        }

        private void HandleLoadClicked()
        {
            onLoadRequested?.Invoke(slotIndex);
        }

        private void HandleDeleteClicked()
        {
            onDeleteRequested?.Invoke(slotIndex);
        }

        private void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }
        #endregion

        // 刷新槽位显示：根据是否有存档决定显示哪些按钮
        public void Refresh(bool hasSave)
        {
            if (slotTitleText != null)
            {
                slotTitleText.text = $"存档槽 {slotIndex + 1}";
            }

            if (slotStateText != null)
            {
                slotStateText.text = hasSave ? "已有存档" : "空存档槽";
            }

            SetButtonVisible(createButton, !hasSave);
            SetButtonVisible(loadButton, hasSave);
            SetButtonVisible(deleteButton, hasSave);
        }

    }
}

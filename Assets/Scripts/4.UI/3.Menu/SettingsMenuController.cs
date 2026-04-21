using System;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    // 设置菜单控制器：负责设置菜单按钮事件转发
    public class SettingsMenuController : MonoBehaviour
    {
        [Header("设置菜单面板")]
        [SerializeField] private GameObject settingsPanel; // 设置主面板

        [Header("音量设置面板")]
        [SerializeField] private GameObject volumePanel; // 音量子面板

        [Header("进入读档界面按钮")]
        [SerializeField] private Button savedGameButton; // 进入读档界面按钮

        [Header("返回游戏按钮")]
        [SerializeField] private Button backToGameButton; // 返回游戏按钮

        private Action onSavedGameRequested; // 进入读档界面请求
        private Action onBackToGameRequested; // 返回游戏请求

        private bool initialized; // 是否已初始化
        private bool listenersBound; // 是否已绑定按钮事件

        #region 生命周期
        private void OnEnable()
        {
            if (initialized)
            {
                BindListeners();
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
        // 初始化设置菜单：由UIRoot传入页面切换回调
        public void Initialize(Action savedGameRequested, Action backToGameRequested)
        {
            // 1.缓存外部流程回调
            onSavedGameRequested = savedGameRequested;
            onBackToGameRequested = backToGameRequested;

            // 2.绑定按钮事件
            BindListeners();

            // 3.启动时隐藏设置菜单
            SetVisible(false);

            // 4.标记初始化完成
            initialized = true;
        }

        // 绑定按钮事件
        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (savedGameButton != null)
            {
                savedGameButton.onClick.AddListener(HandleSavedGameClicked);
            }

            if (backToGameButton != null)
            {
                backToGameButton.onClick.AddListener(HandleBackToGameClicked);
            }

            listenersBound = true;
        }

        // 解绑按钮事件
        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            if (savedGameButton != null)
            {
                savedGameButton.onClick.RemoveListener(HandleSavedGameClicked);
            }

            if (backToGameButton != null)
            {
                backToGameButton.onClick.RemoveListener(HandleBackToGameClicked);
            }

            listenersBound = false;
        }

        // 点击进入读档界面按钮
        private void HandleSavedGameClicked()
        {
            onSavedGameRequested?.Invoke();
        }

        // 点击返回游戏按钮
        private void HandleBackToGameClicked()
        {
            onBackToGameRequested?.Invoke();
        }
        #endregion

        #region 外部调用
        // 设置设置菜单显隐
        public void SetVisible(bool visible)
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(visible);
            }

            if (volumePanel != null)
            {
                volumePanel.SetActive(visible);
            }
        }
        #endregion
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 主菜单控制器：负责开始、音量和退出按钮的事件转发
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main Menu")]
        [SerializeField] private GameObject mainPanel; // 主菜单面板
        [SerializeField] private Button startButton; // 开始按钮
        [SerializeField] private Button volumeButton; // 音量按钮
        [SerializeField] private Button quitButton; // 退出按钮

        private Action onStartRequested; // 开始游戏回调
        private Action onOpenVolumeRequested; // 打开音量面板回调
        private Action onQuitRequested; // 退出游戏回调
        private bool initialized; // 是否已完成初始化
        private bool listenersBound; // 是否已完成事件绑定

        public void ConfigureIfMissing(GameObject panel, Button start, Button volume, Button quit)
        {
            // 兼容旧场景接线：只有字段为空时才补引用。
            if (mainPanel == null) mainPanel = panel;
            if (startButton == null) startButton = start;
            if (volumeButton == null) volumeButton = volume;
            if (quitButton == null) quitButton = quit;
        }

        public void Initialize(Action startRequested, Action openVolumeRequested, Action quitRequested)
        {
            // 初始化只负责缓存回调，并确保按钮事件已绑定。
            onStartRequested = startRequested;
            onOpenVolumeRequested = openVolumeRequested;
            onQuitRequested = quitRequested;
            initialized = true;
            BindListeners();
        }

        public void SetVisible(bool visible)
        {
            // 主菜单显隐统一通过面板根节点控制。
            if (mainPanel != null)
            {
                mainPanel.SetActive(visible);
            }
        }

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

        private void BindListeners()
        {
            // 避免重复绑定，防止按钮点击被多次触发。
            if (listenersBound)
            {
                return;
            }

            if (startButton != null)
            {
                startButton.onClick.AddListener(HandleStartClicked);
            }

            if (volumeButton != null)
            {
                volumeButton.onClick.AddListener(HandleVolumeClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(HandleQuitClicked);
            }

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            // 面板失活时解绑，避免重复进入场景后监听器叠加。
            if (!listenersBound)
            {
                return;
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartClicked);
            }

            if (volumeButton != null)
            {
                volumeButton.onClick.RemoveListener(HandleVolumeClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(HandleQuitClicked);
            }

            listenersBound = false;
        }

        private void HandleStartClicked()
        {
            onStartRequested?.Invoke();
        }

        private void HandleVolumeClicked()
        {
            onOpenVolumeRequested?.Invoke();
        }

        private void HandleQuitClicked()
        {
            onQuitRequested?.Invoke();
        }
    }
}

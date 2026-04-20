using UnityEngine;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 设置菜单控制器：当前先作为空壳，只负责设置菜单显隐
    public class SettingsMenuController : MonoBehaviour
    {
        [Header("设置菜单面板")]
        [SerializeField] private GameObject settingsPanel; // 设置主面板

        [Header("音量设置面板")]
        [SerializeField] private GameObject volumePanel; // 音量子面板

        // 初始化设置菜单：功能还没做，先保证启动时处于隐藏状态
        public void Initialize()
        {
            SetVisible(false);
        }

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
    }
}

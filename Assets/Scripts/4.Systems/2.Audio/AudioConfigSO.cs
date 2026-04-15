using UnityEngine;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 音频配置占位脚本：当前阶段仅作为后续接入真实音频配置的挂点
    public class AudioConfigSO : MonoBehaviour
    {
        [Header("=== 占位参数 ===")]
        [SerializeField] private float defaultMasterVolume = 1f; // 默认总音量
        [SerializeField] private float defaultBackgroundVolume = 1f; // 默认背景音量

        public float DefaultMasterVolume => Mathf.Clamp01(defaultMasterVolume); // 默认总音量只读访问
        public float DefaultBackgroundVolume => Mathf.Clamp01(defaultBackgroundVolume); // 默认背景音量只读访问
    }
}

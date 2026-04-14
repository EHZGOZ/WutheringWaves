using UnityEngine;

namespace WutheringWaves
{
    // 本地设置仓库：通过PlayerPrefs保存轻量级设置项
    public sealed class PlayerPrefsSettingsRepository
    {
        private const string MasterVolumeKey = "Volume"; // 总音量键
        private const string BgmVolumeKey = "BackgroundVolume"; // 背景音量键

        public float GetMasterVolume(float defaultValue = 1f)
        {
            return PlayerPrefs.GetFloat(MasterVolumeKey, defaultValue);
        }

        public void SetMasterVolume(float value)
        {
            // 保存前统一做01区间限制。
            PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }

        public float GetBackgroundVolume(float defaultValue = 1f)
        {
            return PlayerPrefs.GetFloat(BgmVolumeKey, defaultValue);
        }

        public void SetBackgroundVolume(float value)
        {
            // 背景音量与总音量保持一致的安全写入策略。
            PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}

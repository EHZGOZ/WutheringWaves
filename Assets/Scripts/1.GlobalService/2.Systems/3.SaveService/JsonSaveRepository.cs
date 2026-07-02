using System;
using System.IO;
using UnityEngine;

namespace WutheringWaves
{
    // Json存档仓库：仅负责存档文件的读写
    public sealed class JsonSaveRepository
    {
        private readonly string _savePath; // 存档文件完整路径

        public string SavePath => _savePath; // 对外暴露实际存档路径

        public JsonSaveRepository(string fileName)
        {
            // 文件名为空时回退到默认值，避免生成非法路径。
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "save.json";
            }

            _savePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public bool TryLoad(out SaveData saveData)
        {
            saveData = null;
            // 文件不存在时直接返回false，由上层决定是否创建默认存档。
            if (!File.Exists(_savePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(_savePath);
                saveData = JsonUtility.FromJson<SaveData>(json);
                return saveData != null;
            }
            catch (Exception e)
            {
                // 读取或反序列化异常统一记录日志，便于定位存档损坏问题。
                Debug.LogError($"[JsonSaveRepository] Load failed: {e.Message}");
                return false;
            }
        }

        public bool Save(SaveData data)
        {
            // 空数据不落盘，避免覆盖掉已有有效存档。
            if (data == null)
            {
                Debug.LogWarning("[JsonSaveRepository] Save skipped because data is null.");
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(_savePath, json);
                return true;
            }
            catch (Exception e)
            {
                // 写盘失败时只返回false，由服务层决定后续处理策略。
                Debug.LogError($"[JsonSaveRepository] Save failed: {e.Message}");
                return false;
            }
        }
        // 删除本地存档文件：只负责删除JSON文件，不负责重新创建默认存档
        public bool Delete()
        {
            if (!File.Exists(_savePath))
            {
                return false;
            }

            try
            {
                File.Delete(_savePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonSaveRepository] Delete failed: {e.Message}");
                return false;
            }
        }
        // 判断当前存档文件是否存在
        public bool Exists()
        {
            return File.Exists(_savePath);
        }

    }
}

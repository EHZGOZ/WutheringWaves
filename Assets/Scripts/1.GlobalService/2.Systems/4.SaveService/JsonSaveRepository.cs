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

        // 保存JSON存档数据
        public bool Save(SaveData data)
        {
            // 1.空数据不落盘，避免覆盖已有的有效存档
            if (data == null)
            {
                Debug.LogWarning("[JsonSaveRepository] 保存失败：存档数据为空。");
                return false;
            }

            try
            {
                // 2.获取当前存档文件所在的账号目录
                string directoryPath = Path.GetDirectoryName(_savePath);
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    Debug.LogError(
                        $"[JsonSaveRepository] 保存失败：无法解析存档目录。savePath = {_savePath}"
                    );
                    return false;
                }

                // 3.确保账号存档目录存在
                // 目录已经存在时不会重复创建，也不会抛出异常
                Directory.CreateDirectory(directoryPath);

                // 4.将当前存档数据转换为格式化JSON文本
                string json = JsonUtility.ToJson(data, true);

                // 5.将JSON文本写入当前账号的存档文件
                File.WriteAllText(_savePath, json);

                return true;
            }
            catch (Exception e)
            {
                // 6.统一记录目录创建、序列化或文件写入异常
                Debug.LogError(
                    $"[JsonSaveRepository] 保存失败：{e.Message}，savePath = {_savePath}"
                );
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

using System;
using System.IO;
using UnityEngine;

namespace WutheringWaves
{
    // 账号数据 JSON 仓储实现：读写 accounts/index.json
    public class JsonAccountRepository : IAccountRepository
    {
        private readonly string filePath; // index.json 的完整路径

        // 构造函数：外部传 rootDir 和 fileName，内部拼出完整路径
        public JsonAccountRepository(string rootDir, string fileName)
        {
            filePath = Path.Combine(rootDir, "accounts", fileName);
        }

        // 读取索引：文件不存在或损坏都返回空列表，绝不抛异常
        public AccountIndexData Load()
        {
            if (!File.Exists(filePath))
            {
                return new AccountIndexData();
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<AccountIndexData>(json) ?? new AccountIndexData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonAccountRepository] 读取索引失败：{e.Message}");
                return new AccountIndexData();
            }
        }

        // 保存索引：负责建目录和写文件
        public bool Save(AccountIndexData index)
        {
            if (index == null)
            {
                return false;
            }

            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonUtility.ToJson(index, true);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonAccountRepository] 保存索引失败：{e.Message}");
                return false;
            }
        }

        // 文件是否存在
        public bool Exists()
        {
            return File.Exists(filePath);
        }
    }
}
using System;
using UnityEngine;

namespace WutheringWaves
{
    // 本他账号数据：存储在 accounts_index.json 中，用于账号列表展示
    // Serializable 标记确保可以被 JsonUtility 序列化
    [Serializable]
    public class AccountInfo
    {
        public string username;          // 账号名（唯一标识，不能重复）
        public string passwordHash;      // 密码的 SHA256 哈希值（不是明文）
        public string salt;              // 加盐值，防止哈希碰撞
        public long createdAt;           // 注册时间戳（毫秒）
        public long lastLoginAt;         // 最近一次登录时间戳
    }

    // 账号列表：整个 accounts_index.json 的根结构
    [Serializable]
    public class AccountIndexData
    {
        // 所有注册过的账号，按 username 索引
        public AccountInfo[] accounts = Array.Empty<AccountInfo>();
    }
}
using System;
using System.Security.Cryptography;
using System.Text;

namespace WutheringWaves
{
    // 账号密码加密工具：纯静态方法，不依赖 MonoBehaviour
    public static class AccountCrypto
    {
        // 生成16字节随机盐，转成 Base64 字符串存储
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            return Convert.ToBase64String(saltBytes);
        }

        // 盐 + 明文密码 → SHA256 → Base64
        // 根据密码和盐生成哈希
        public static string HashPassword(string password, string salt)
        {
            // 1.把盐拼到密码前面
            // 例如：salt = "AbCd123"，password = "123456"
            // salted = "AbCd123123456"
            string salted = salt + password;

            // 2.使用SHA256计算 salted 字符串的哈希
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(salted));

                // 3.把哈希字节转换成Base64字符串，方便保存到JSON里
                return Convert.ToBase64String(hash);
            }
        }

        // 验证密码：明文 + 盐 算出来的哈希 是否等于 存的哈希
        public static bool VerifyPassword(string password, string salt, string storedHash)
        {
            return HashPassword(password, salt) == storedHash;
        }
    }
}
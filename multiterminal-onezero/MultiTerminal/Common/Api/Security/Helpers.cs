using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Arbitrage.Api.Security
{
    public static class Helpers 
    {
        public static string Encrypt(this string data, string key)
        {
            try
            {
                using Rfc2898DeriveBytes keyGenerator = new Rfc2898DeriveBytes(key, 8);
                using var aes = Rijndael.Create();
                aes.IV = keyGenerator.GetBytes(aes.BlockSize / 8);
                aes.Key = keyGenerator.GetBytes(aes.KeySize / 8);
                using var encryptor = aes.CreateEncryptor();
                using var ms = new MemoryStream();
                using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                ms.Write(keyGenerator.Salt, 0, keyGenerator.Salt.Length);
                byte[] rawData = Encoding.UTF8.GetBytes(data);
                cs.Write(rawData, 0, rawData.Length);
                cs.Close();
                return Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
            }
            return string.Empty;
        }
        public static string Decrypt(this string data, string key)
        {
            try
            {
                byte[] rawData = Convert.FromBase64String(data);
                byte[] salt = new byte[8];
                Array.Copy(rawData, salt, 8);
                using Rfc2898DeriveBytes keyGenerator = new Rfc2898DeriveBytes(key, salt);
                using var aes = Rijndael.Create();
                aes.IV = keyGenerator.GetBytes(aes.BlockSize / 8);
                aes.Key = keyGenerator.GetBytes(aes.KeySize / 8);
                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write);
                cs.Write(rawData, 8, rawData.Length - 8);
                cs.Close();
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch
            {
            }
            return string.Empty;
        }
    }
}
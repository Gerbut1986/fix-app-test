using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace Arbitrage.Api.Security
{
    internal static class ComputerId
    {
        internal static string Get()
        {
            using var hash = SHA256.Create();
            byte[] data = hash.ComputeHash(hardwareId());
            return Convert.ToBase64String(data);
        }
        internal static string GetSHA1Hex()
        {
            using var hash = SHA1.Create();
            byte[] data = hash.ComputeHash(hardwareId());
            var sb = new StringBuilder(data.Length * 2);

            foreach (byte b in data)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }
        static string getManagementProperty(string key, string subkey)
        {
            string res = "";
            try
            {
                using var searcher = new ManagementObjectSearcher("select * from "+key);
                foreach (var mo in searcher.Get())
                {
                    try
                    {
                        res += mo[subkey];
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            return res;
        }
        static byte[] hardwareId()
        {
            string id = "##.";
            id += getManagementProperty("Win32_Processor", "ProcessorId");
            id += getManagementProperty("Win32_BaseBoard", "SerialNumber");
            return Encoding.UTF8.GetBytes(id);
        }
    }
}
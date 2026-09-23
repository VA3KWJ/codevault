using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CodeVault
{
    internal sealed record VaultConfig(string Secret, int Digits, int Period);

    /// <summary>
    /// Persists the TOTP secret next to the executable, encrypted with Windows
    /// DPAPI (CurrentUser scope). Unlike simple obfuscation, the encrypted
    /// bytes are only decryptable under the same Windows user account on the
    /// same machine that saved them - moving the file elsewhere, or another
    /// account reading it, yields nothing usable.
    /// </summary>
    internal static class ConfigStore
    {
        // Optional additional entropy, purely to scope this file's DPAPI blobs
        // to this application rather than any other CurrentUser DPAPI use.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CodeVault.v1.dpapi-entropy");

        public static string GetConfigPath()
        {
            string dir = AppContext.BaseDirectory;
            return Path.Combine(dir, "CodeVault.dat");
        }

        public static bool ConfigExists() => File.Exists(GetConfigPath());

        public static VaultConfig? Load()
        {
            string path = GetConfigPath();
            if (!File.Exists(path))
                return null;

            string[] lines = File.ReadAllLines(path);
            if (lines.Length < 3)
                return null;

            int digits = int.Parse(lines[0]);
            int period = int.Parse(lines[1]);
            byte[] encrypted = Convert.FromBase64String(lines[2]);

            byte[] plain = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            string secret = Encoding.UTF8.GetString(plain);

            return new VaultConfig(secret, digits, period);
        }

        public static void Save(string secret, int digits, int period)
        {
            byte[] plain = Encoding.UTF8.GetBytes(secret);
            byte[] encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);

            string content =
                digits.ToString() + Environment.NewLine +
                period.ToString() + Environment.NewLine +
                Convert.ToBase64String(encrypted) + Environment.NewLine;

            File.WriteAllText(GetConfigPath(), content);
        }

        public static void Delete()
        {
            string path = GetConfigPath();
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}

using System;
using System.Security.Cryptography;

namespace CodeVault
{
    /// <summary>
    /// RFC 4648 Base32 decode and RFC 6238 TOTP code generation, using the
    /// framework's own HMACSHA1 implementation (no hand-rolled crypto).
    /// </summary>
    internal static class Totp
    {
        public static byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

            string clean = input.Trim().TrimEnd('=').Replace(" ", "").ToUpperInvariant();
            if (clean.Length == 0)
                throw new FormatException("Secret is empty.");

            int bitBuffer = 0;
            int bitsInBuffer = 0;
            var bytes = new System.Collections.Generic.List<byte>();

            foreach (char c in clean)
            {
                int value = alphabet.IndexOf(c);
                if (value < 0)
                    throw new FormatException("Secret contains invalid Base32 characters.");

                bitBuffer = (bitBuffer << 5) | value;
                bitsInBuffer += 5;

                if (bitsInBuffer >= 8)
                {
                    bitsInBuffer -= 8;
                    bytes.Add((byte)((bitBuffer >> bitsInBuffer) & 0xFF));
                }
            }

            if (bytes.Count == 0)
                throw new FormatException("Secret is too short.");

            return bytes.ToArray();
        }

        public static string Generate(byte[] secret, int digits, int period, DateTimeOffset when)
        {
            long counter = when.ToUnixTimeSeconds() / period;

            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counterBytes);

            using var hmac = new HMACSHA1(secret);
            byte[] hash = hmac.ComputeHash(counterBytes);

            int offset = hash[^1] & 0x0F;
            int binCode =
                ((hash[offset] & 0x7F) << 24) |
                ((hash[offset + 1] & 0xFF) << 16) |
                ((hash[offset + 2] & 0xFF) << 8) |
                (hash[offset + 3] & 0xFF);

            int mod = (int)Math.Pow(10, digits);
            return (binCode % mod).ToString(new string('0', digits));
        }

        public static int SecondsRemaining(int period, DateTimeOffset when)
        {
            long secondsIntoPeriod = when.ToUnixTimeSeconds() % period;
            return period - (int)secondsIntoPeriod;
        }
    }
}

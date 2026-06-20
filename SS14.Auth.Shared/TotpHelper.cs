using System;
using System.Security.Cryptography;

namespace SS14.Auth.Shared;

public static class TotpHelper
{
    private const int TimeStepSeconds = 120;
    private const int Digits = 8;
    private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string GenerateSecret()
    {
        var bytes = new byte[20];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base32Encode(bytes);
    }

    public static string GenerateCode(string secret)
    {
        var counter = GetCurrentCounter();
        var hash = ComputeHmac(secret, counter);
        var code = Truncate(hash, Digits);
        return code.ToString().PadLeft(Digits, '0');
    }

    public static bool ValidateCode(string secret, string code)
    {
        var counter = GetCurrentCounter();
        for (var i = -1; i <= 1; i++)
        {
            var hash = ComputeHmac(secret, counter + i);
            var expected = Truncate(hash, Digits).ToString().PadLeft(Digits, '0');
            if (string.Equals(expected, code, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    public static long GetRemainingSeconds()
    {
        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return TimeStepSeconds - unix % TimeStepSeconds;
    }

    private static long GetCurrentCounter()
    {
        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unix / TimeStepSeconds;
    }

    private static byte[] ComputeHmac(string secret, long counter)
    {
        var secretBytes = Base32Decode(secret);
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secretBytes);
        return hmac.ComputeHash(counterBytes);
    }

    private static int Truncate(byte[] hash, int digits)
    {
        var offset = hash[19] & 0xf;
        var binary = ((hash[offset] & 0x7f) << 24) |
                     (hash[offset + 1] << 16) |
                     (hash[offset + 2] << 8) |
                     hash[offset + 3];
        return binary % (int)Math.Pow(10, digits);
    }

    private static string Base32Encode(byte[] data)
    {
        var result = new char[(data.Length + 4) / 5 * 8];
        var resultIndex = 0;
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                result[resultIndex++] = Base32Chars[(buffer >> bitsLeft) & 0x1f];
            }
        }
        if (bitsLeft > 0)
            result[resultIndex++] = Base32Chars[(buffer << (5 - bitsLeft)) & 0x1f];
        return new string(result, 0, resultIndex);
    }

    private static byte[] Base32Decode(string data)
    {
        data = data.TrimEnd('=').ToUpperInvariant();
        var byteCount = data.Length * 5 / 8;
        var bytes = new byte[byteCount];
        var byteIndex = 0;
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var c in data)
        {
            var index = Base32Chars.IndexOf(c);
            if (index < 0) continue;
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                bytes[byteIndex++] = (byte)((buffer >> bitsLeft) & 0xff);
            }
        }
        return bytes;
    }
}

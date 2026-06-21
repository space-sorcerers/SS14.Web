using System;
using System.Collections.Concurrent;

namespace SS14.Auth.Shared;

public static class SsoCodeStore
{
    public static readonly ConcurrentDictionary<string, SsoAuthData> Codes = new();

    public static string GenerateCode(Guid userId, TimeSpan? expiry = null)
    {
        expiry ??= TimeSpan.FromMinutes(5);
        var code = Guid.NewGuid().ToString("N")[..12];
        Codes[code] = new SsoAuthData(userId, DateTime.UtcNow + expiry.Value);
        return code;
    }
}

public sealed record SsoAuthData(Guid UserId, DateTime Expires);

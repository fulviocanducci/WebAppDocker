using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using StackExchange.Redis;
using System.Security.Claims;

namespace WebAppDocker.Models;

public class RedisTicketStore(IConnectionMultiplexer redis) : ITicketStore
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var sessionId = "auth:" + Guid.NewGuid();
        var data = TicketSerializer.Default.Serialize(ticket);
        var userId = ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _db.StringSetAsync(sessionId, data, TimeSpan.FromHours(2));
        await _db.StringSetAsync($"auth:{userId}", sessionId);
        if (userId != null)
        {
            await _db.SetAddAsync($"user_sessions:{userId}", sessionId);
            await _db.SetAddAsync("users_with_sessions", userId);
        }
        return sessionId;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var data = TicketSerializer.Default.Serialize(ticket);
        await _db.StringSetAsync(key, data, TimeSpan.FromHours(2));
    }

    public async Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        var data = await _db.StringGetAsync(key);
        if (data.IsNullOrEmpty) return null;
        return TicketSerializer.Default.Deserialize(data);
    }

    public async Task RemoveAsync(string key)
    {
        var data = await _db.StringGetAsync(key);
        if (!data.IsNullOrEmpty)
        {
            var ticket = TicketSerializer.Default.Deserialize(data);
            var userId = ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var userKey = $"user_sessions:{userId}";
                await _db.SetRemoveAsync(userKey, key);
                var remaining = await _db.SetLengthAsync(userKey);
                if (remaining == 0)
                {
                    await _db.KeyDeleteAsync(userKey);
                    await _db.SetRemoveAsync("users_with_sessions", userId);
                }
            }
        }
        await _db.KeyDeleteAsync(key);
    }
}
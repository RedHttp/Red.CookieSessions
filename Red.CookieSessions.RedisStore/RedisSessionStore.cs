using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Red.CookieSessions.RedisSessionStore
{
    public class RedisSessionStore<T> : ICookieStore<T>
        where T : ICookieSession, new()
    {
        private readonly ConnectionMultiplexer _redisConnection;
        private const string CacheKey = "Red.CookieSession:";
        
        public RedisSessionStore(ConnectionMultiplexer redisConnection)
        {
            _redisConnection = redisConnection;
        }
        
        public async Task<ValueTuple<bool, T>> TryGet(string sessionId)
        {
            var key = CacheKey + sessionId;
            var value = await _redisConnection.GetDatabase().StringGetAsync(key);
            
            return (value.HasValue, value.HasValue ? JsonSerializer.Deserialize<T>(value) : default);
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            var key = CacheKey + sessionId;
            return await _redisConnection.GetDatabase().KeyDeleteAsync(key);
        }

        public Task Set(T session)
        {
            var key = CacheKey + session.SessionId;
            var json = JsonSerializer.Serialize(session);
            var expiration = session.Expires.Subtract(DateTime.UtcNow);

            return _redisConnection.GetDatabase().StringSetAsync(key, json, expiration);
        }

        public async Task RemoveExpired()
        {
            // redis does this automatically
        }
    }
}
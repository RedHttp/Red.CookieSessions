using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Red.CookieSessions.RedisStore
{
    public class RedisSessionStore<T> : ICookieStore<T>
        where T : class, ICookieSession, new()
    {
        private readonly ConnectionMultiplexer _redisConnection;
        private const string CacheKey = "Red.CookieSession:";
        
        public RedisSessionStore(ConnectionMultiplexer redisConnection)
        {
            _redisConnection = redisConnection;
        }
        
        public async Task<T?> TryGet(string sessionId)
        {
            var key = CacheKey + sessionId;
            var value = await _redisConnection.GetDatabase().StringGetAsync(key);
            
            return value.HasValue ? JsonSerializer.Deserialize<T>(value) : default;
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            var key = CacheKey + sessionId;
            return await _redisConnection.GetDatabase().KeyDeleteAsync(key);
        }

        public Task Set(T session)
        {
            var key = CacheKey + session.Id;
            var json = JsonSerializer.Serialize(session);
            var expiration = session.Expiration.Subtract(DateTime.UtcNow);

            return _redisConnection.GetDatabase().StringSetAsync(key, json, expiration);
        }

        public async Task RemoveExpired()
        {
            // redis does this automatically
        }
    }
}
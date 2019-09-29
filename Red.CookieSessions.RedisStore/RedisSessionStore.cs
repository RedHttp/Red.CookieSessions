using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;

namespace Red.CookieSessions.RedisSessionStore
{
    public class RedisSessionStore<T> : ICookieStore<T>
        where T : ICookieSession, new()
    {
        private readonly ConnectionMultiplexer _redisConnection;

        public RedisSessionStore(ConnectionMultiplexer redisConnection)
        {
            _redisConnection = redisConnection;
        }


        public async Task<ValueTuple<bool, T>> TryGet(string id)
        {
            var db = _redisConnection.GetDatabase();
            var result = await db<T>(id);
            return (result != null, result);
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            return await _redisConnection.DeleteAsync<T>(sessionId) > 0;
        }

        public Task Set(T session)
        {
            return _redisConnection.InsertOrReplaceAsync(session);
        }

        public async Task RemoveExpired()
        {
            var now = DateTime.UtcNow;
            await _redisConnection.Table<T>().DeleteAsync(s => s.Expires <= now);
        }
    }
}
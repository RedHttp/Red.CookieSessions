using System;
using System.Threading.Tasks;
using SQLite;

namespace Red.CookieSessions.SQLiteStore
{
    public class SQLiteSessionStore<T> : ICookieStore<T>
        where T : ICookieSession, new()
    {
        private readonly SQLiteAsyncConnection _db;

        public SQLiteSessionStore(SQLiteAsyncConnection db)
        {
            _db = db;
        }


        public async Task<ValueTuple<bool, T>> TryGet(string id)
        {
            var result = await _db.GetAsync<T>(id);
            return (result != null, result);
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            return await _db.DeleteAsync<T>(sessionId) > 0;
        }

        public Task Set(T session)
        {
            return _db.InsertOrReplaceAsync(session);
        }

        public async Task RemoveExpired()
        {
            var now = DateTime.UtcNow;
            await _db.Table<T>().DeleteAsync(s => s.Expiration <= now);
        }
    }
}
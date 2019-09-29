using System;
using System.Threading.Tasks;
using LiteDB;

namespace Red.CookieSessions.LiteDBStore
{
    public class LiteDBSessionStore<T> : ICookieStore<T>
        where T : ICookieSession, new()
    {
        private readonly LiteCollection<T> _db;

        public LiteDBSessionStore(LiteDatabase db)
        {
            _db = db.GetCollection<T>();
        }


        public async Task<ValueTuple<bool, T>> TryGet(string id)
        {
            var result = _db.FindById(id);
            return (result != null, result);
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            return _db.Delete(sessionId);
        }

        public async Task Set(T session)
        {
            _db.Upsert(session);
        }

        public async Task RemoveExpired()
        {
            var now = DateTime.UtcNow;
            _db.Delete(s => s.Expiration <= now);
        }
    }
}
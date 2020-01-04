using System;
using System.Threading.Tasks;
using LiteDB;

namespace Red.CookieSessions.LiteDBStore
{
    public class LiteDBSessionStore<T> : ICookieStore<T>
        where T : class, ICookieSession, new()
    {
        private readonly LiteCollection<T> _db;

        public LiteDBSessionStore(LiteDatabase db)
        {
            _db = db.GetCollection<T>();
        }


        public async Task<T?> TryGet(string id)
        {
            return _db.FindById(id);
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
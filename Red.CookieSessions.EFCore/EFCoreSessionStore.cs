using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Red.CookieSessions.EFCore
{
    public class EFCoreSessionStore<TSession> : ICookieStore<TSession>
        where TSession : class, ICookieSession, new()
    {
        private readonly DbContext _db;

        public EFCoreSessionStore(DbContext dbContext)
        {
            _db = dbContext;
        }


        public async Task<ValueTuple<bool, TSession>> TryGet(string sessionId)
        {
            var result = await _db.Set<TSession>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId);
            return (result != null, result);
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            var result = await _db.Set<TSession>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId);
            if (result == default) return true;
            _db.Set<TSession>().Remove(result);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task Set(TSession session)
        {
            var result = await _db.Set<TSession>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == session.Id);
            if (result != default)
            {
                _db.Remove(result);
            }
            _db.Add(session);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveExpired()
        {
            var now = DateTime.UtcNow;
            var expired = _db.Set<TSession>().Where(s => s.Expiration <= now);
            _db.RemoveRange(expired);
            await _db.SaveChangesAsync();
        }
    }
}
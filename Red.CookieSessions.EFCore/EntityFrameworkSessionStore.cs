using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Red.CookieSessions.EFCore
{
    public class EntityFrameworkSessionStore<TSession> : ICookieStore<TSession>
        where TSession : class, ICookieSession, new()
    {
        private readonly Func<DbContext> _resolveContextFunc;

        private DbContext GetDb() => _resolveContextFunc();
        public EntityFrameworkSessionStore(Func<DbContext> resolveContext)
        {
            _resolveContextFunc = resolveContext;
        }


        public async Task<TSession?> TryGet(string sessionId)
        {
            return await GetDb().Set<TSession>().FindAsync(sessionId);
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            var db = GetDb();
            var result = await db.Set<TSession>().FindAsync(sessionId);
            if (result != default)
            {
                db.Set<TSession>().Remove(result);
                await db.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task Set(TSession session)
        {
            var db = GetDb();
            var result = await db.Set<TSession>().FindAsync(session.Id);
            if (result != default)
            {
                db.Remove(result);
            }
            db.Add(session);
            await db.SaveChangesAsync();
        }

        public async Task RemoveExpired()
        {
            var db = GetDb();
            var now = DateTime.UtcNow;
            var expired = await db.Set<TSession>().Where(s => s.Expiration <= now).ToListAsync();
            db.RemoveRange(expired);
            await db.SaveChangesAsync();
        }
    }
}
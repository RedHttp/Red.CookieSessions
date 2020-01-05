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
        private readonly Func<DbContext> _getContext;
        private readonly Expression<Func<TSession, object>>[] _includes;

        public EntityFrameworkSessionStore(Func<DbContext> getContextContext, params Expression<Func<TSession, object>>[] includes)
        {
            _getContext = getContextContext;
            _includes = includes;
        }


        private IQueryable<TSession> WithIncludes(DbContext context)
        {
            return _includes.Aggregate(context.Set<TSession>().AsNoTracking(), (current, expression) => current.Include(expression));
        }

        public async Task<TSession?> TryGet(string sessionId)
        {
            await using var db = _getContext();
            return await WithIncludes(db).FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            await using var db = _getContext();
            var result = await WithIncludes(db).FirstOrDefaultAsync(s => s.Id == sessionId);
            if (result == default)
                return false;
            
            db.Set<TSession>().Remove(result);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task Set(TSession session)
        {
            await using var db = _getContext();
            var result = await WithIncludes(db).FirstOrDefaultAsync(s => s.Id == session.Id);
            if (result != default)
                db.Remove(result);
            
            db.Add(session);
            await db.SaveChangesAsync();
        }

        public async Task RemoveExpired()
        {
            await using var db = _getContext();
            var now = DateTime.UtcNow;
            var expired = await db.Set<TSession>().Where(s => s.Expiration <= now).ToListAsync();
            db.RemoveRange(expired);
            await db.SaveChangesAsync();
        }
    }
}
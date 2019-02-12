using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Red.CookieSessions
{
    public class InMemoryCookieStore<TCookieSession> : ICookieStore<TCookieSession> where TCookieSession : class, ICookieSession, new()
    {
        private readonly ConcurrentDictionary<string, TCookieSession> _sessions =
            new ConcurrentDictionary<string, TCookieSession>();

        public async Task RemoveExpired()
        {
            var now = DateTime.Now;
            var expired = _sessions.Where(kvp => kvp.Value.Expires < now).ToList();
            foreach (var session in expired)
            {
                _sessions.TryRemove(session.Key, out _);
            }
        }

        public async Task Set(string token, TCookieSession session)
        {
            _sessions[token] = session;
        }

        public async Task<Tuple<bool, TCookieSession>> TryGet(string token)
        {
            var success = _sessions.TryGetValue(token, out var session);
            return new Tuple<bool, TCookieSession>(success, session);
        }

        public async Task<bool> TryRemove(string token)
        {
            return _sessions.TryRemove(token, out _);
        }
    }
}
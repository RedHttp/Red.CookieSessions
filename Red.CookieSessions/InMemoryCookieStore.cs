using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Red.CookieSessions
{
    public class InMemoryCookieStore<TCookieSession> : ICookieStore<TCookieSession>
    {

        private readonly ConcurrentDictionary<string, CookieSession<TCookieSession>> _sessions =
            new ConcurrentDictionary<string, CookieSession<TCookieSession>>();

        public async Task RemoveExpired()
        {
            var now = DateTime.Now;
            var expired = _sessions.Where(kvp => kvp.Value.Expires < now).ToList();
            foreach (var session in expired)
            {
                _sessions.TryRemove(session.Key, out _);
            }
        }

        public async Task Set(string token, CookieSession<TCookieSession> session)
        {
            _sessions[token] = session;
        }

        public async Task<Tuple<bool, CookieSession<TCookieSession>>> TryGet(string token)
        {
            var success = _sessions.TryGetValue(token, out var session);
            return new Tuple<bool, CookieSession<TCookieSession>>(success, session);
        }

        public async Task<bool> TryRemove(string token)
        {
            return _sessions.TryRemove(token, out _);
        }
    }
}
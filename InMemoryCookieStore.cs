using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Red.CookieSessions
{
    public class InMemoryCookieStore : ICookieStore
    {

        private readonly ConcurrentDictionary<string, CookieSession> _sessions =
            new ConcurrentDictionary<string, CookieSession>();

        public void RemoveExpired()
        {
            var now = DateTime.Now;
            var expired = _sessions.Where(kvp => kvp.Value.Expires < now).ToList();
            foreach (var session in expired)
            {
                _sessions.TryRemove(session.Key, out _);
            }
        }

        public void Set(string token, in CookieSession session)
        {
            _sessions[token] = session;
        }

        public bool TryGet(string token, out CookieSession session)
        {
            return _sessions.TryGetValue(token, out session);
        }

        public bool TryRemove(string token)
        {
            return _sessions.TryRemove(token, out _);
        }
    }
}
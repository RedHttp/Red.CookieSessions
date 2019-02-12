using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Red.CookieSessions
{
    public class InMemoryCookieStore<TCookieSession> : ICookieStore<TCookieSession> where TCookieSession : class, ICookieSession, new()
    {
        private readonly ConcurrentDictionary<string, TCookieSession> _sessions = new ConcurrentDictionary<string, TCookieSession>();

        public async Task RemoveExpired()
        {
            var now = DateTime.Now;
            var expired = _sessions.Where(kvp => kvp.Value.Expires < now).ToList();
            foreach (var session in expired)
            {
                _sessions.TryRemove(session.Key, out _);
            }
        }

        public async Task Set(TCookieSession session)
        {
            _sessions[session.SessionId] = session;
        }

        public async Task<Tuple<bool, TCookieSession>> TryGet(string id)
        {
            var success = _sessions.TryGetValue(id, out var session);
            return new Tuple<bool, TCookieSession>(success, session);
        }

        public async Task<bool> TryRemove(string sessionId)
        {
            return _sessions.TryRemove(sessionId, out _);
        }
    }
}
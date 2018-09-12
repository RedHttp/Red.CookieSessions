using System;

namespace Red.CookieSessions
{
    public interface ICookieStore
    {
        bool TryGet(string token, out CookieSession session);
        bool TryRemove(string token);
        void Set(string token, in CookieSession session);
        void RemoveExpired();
    }
}
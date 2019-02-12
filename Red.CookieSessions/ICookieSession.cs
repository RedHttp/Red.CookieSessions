using System;

namespace Red.CookieSessions
{
    public interface ICookieSession
    {
        DateTime Expires { get; set; }
        string SessionId { get; set; }
    }
}
using System;

namespace Red.CookieSessions
{
    public interface ICookieSession
    {
        DateTime Expiration { get; set; }
        string Id { get; set; }
    }
}
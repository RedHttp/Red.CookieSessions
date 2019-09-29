using System;
using System.Threading.Tasks;

namespace Red.CookieSessions
{

    public abstract class CookieSessionBase : ICookieSession
    {
        public DateTime Expiration { get; set; }
        public string Id { get; set; }
    }
}
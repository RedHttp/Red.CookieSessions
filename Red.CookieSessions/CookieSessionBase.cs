using System;
using System.Threading.Tasks;

namespace Red.CookieSessions
{

    public abstract class CookieSessionBase : ICookieSession
    {
        public DateTime Expires { get; set; }
        public string SessionId { get; set; }
    }
}
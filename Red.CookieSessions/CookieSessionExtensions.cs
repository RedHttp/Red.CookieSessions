using System.Threading.Tasks;

namespace Red.CookieSessions
{
    public static class CookieSessionExtensions
    {
        /// <summary>
        ///     Opens a new session and adds cookie for authentication. 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="session"></param>
        public static async Task OpenSession<TCookieSession>(this Response response, TCookieSession session) 
            where TCookieSession : class, ICookieSession, new()
        {
            var context = response.Context;
            var existing = context.GetData<TCookieSession>();
            if (existing != default)
            {
                await response.CloseSession(existing);
            }

            var manager = context.Plugins.Get<CookieSessions<TCookieSession>>();
            var cookie = await manager.OpenSession(session);
            response.Headers["Set-Cookie"] = cookie;
        }

        /// <summary>
        ///    Renews the session expiry time and updates the cookie
        /// </summary>
        /// <param name="session"></param>
        /// <param name="response"></param>
        public static async Task RenewSession<TCookieSession>(this Response response, TCookieSession session)  
            where TCookieSession : class, ICookieSession, new()
        {
            var manager = response.Context.Plugins.Get<CookieSessions<TCookieSession>>();
            var newCookie = await manager.RenewSession(session);
            response.Headers["Set-Cookie"] = newCookie;
        }
        
        /// <summary>
        ///    Saves changes to the session object without renewing the expiration
        /// </summary>
        /// <param name="session"></param>
        /// <param name="response"></param>
        public static async Task Resave<TCookieSession>(this Response response, TCookieSession session)  
            where TCookieSession : class, ICookieSession, new()
        {
            var manager = response.Context.Plugins.Get<CookieSessions<TCookieSession>>();
            await manager.RenewSession(session);
        }

        /// <summary>
        ///    Closes the session and updates the cookie
        /// </summary>
        /// <param name="session"></param>
        /// <param name="response"></param>
        public static async Task CloseSession<TCookieSession>(this Response response, TCookieSession session)  
            where TCookieSession : class, ICookieSession, new()
        {
            var manager = response.Context.Plugins.Get<CookieSessions<TCookieSession>>();
            await manager.CloseSession(session, out var cookie);
            response.Headers["Set-Cookie"] = cookie;
        }
    }
}
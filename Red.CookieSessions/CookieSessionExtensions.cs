using System.Threading.Tasks;

namespace Red.CookieSessions
{
    public static class CookieSessionExtensions
    {
        /// <summary>
        ///     Opens a new session and adds cookie for authentication. 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="sessionData"></param>
        public static async Task OpenSession<TCookieSession>(this Request request, TCookieSession sessionData)
        {
            var existing = request.GetSession<TCookieSession>();
            existing?.Close(request);

            var manager = request.ServerPlugins.Get<CookieSessions<TCookieSession>>();
            var cookie = await manager.OpenSession(sessionData);
            request.UnderlyingContext.Response.Headers["Set-Cookie"] = cookie;
        }
        /// <summary>
        ///     Gets the session-object attached to the request, added by the CookieSessions middleware
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static CookieSession<TCookieSession> GetSession<TCookieSession>(this Request request)
        {
            return request.GetData<CookieSession<TCookieSession>>();
        }
    }
}
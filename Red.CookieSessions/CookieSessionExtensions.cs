﻿using System.Threading.Tasks;

namespace Red.CookieSessions
{
    public static class CookieSessionExtensions
    {
        /// <summary>
        ///     Opens a new session and adds cookie for authentication. 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="sessionData"></param>
        public static async Task OpenSession<TCookieSession>(this Request request, TCookieSession sessionData) where TCookieSession : class, ICookieSession, new()
        {
            var existing = request.GetSession<TCookieSession>();
            existing?.Close(request);

            var context = request.Context;
            var manager = request.Context.Plugins.Get<CookieSessions<TCookieSession>>();
            var cookie = await manager.OpenSession(sessionData);
            context.Response.AspNetResponse.Headers["Set-Cookie"] = cookie;
        }
        /// <summary>
        ///     Gets the session-object attached to the request, added by the CookieSessions middleware
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static TCookieSession GetSession<TCookieSession>(this Request request)
        {
            return request.GetData<TCookieSession>();
        }

        /// <summary>
        ///    Renews the session expiry time and updates the cookie
        /// </summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        public static async Task Renew<TCookieSession>(this TCookieSession session, Request request)  where TCookieSession : class, ICookieSession, new()
        {
            var context = request.Context;
            var manager = context.Plugins.Get<CookieSessions<TCookieSession>>();
            var newCookie = await manager.RenewSession(session);
            context.Response.AspNetResponse.Headers["Set-Cookie"] = newCookie;
        }

        /// <summary>
        ///    Closes the session and updates the cookie
        /// </summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        public static async Task Close<TCookieSession>(this TCookieSession session, Request request)  where TCookieSession : class, ICookieSession, new()
        {
            var context = request.Context;
            var manager = context.Plugins.Get<CookieSessions<TCookieSession>>();
            var closed = await manager.CloseSession(session, out var cookie);
            if (closed)
            {
                context.Response.AspNetResponse.Headers["Set-Cookie"] = cookie;
            }
        }
    }
}
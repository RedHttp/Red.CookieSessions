using System;
using System.Threading.Tasks;

namespace Red.CookieSessions
{
    public class CookieSession<TCookieSession> 
    {
        internal CookieSession(TCookieSession session, DateTime expiration)
        {
            Data = session;
            Expires = expiration;
        }

        /// <summary>
        /// 
        /// </summary>
        public TCookieSession Data { get; }
        public DateTime Expires { get; internal set; }


        /// <summary>
        ///    Renews the session expiry time and updates the cookie
        /// </summary>
        /// <param name="request"></param>
        public async Task Renew(Request request)
        {
            var manager = request.ServerPlugins.Get<CookieSessions<TCookieSession>>();
            var existingCookie = request.Cookies[manager._tokenName];
            var newCookie = await manager.RenewSession(existingCookie);
            if (newCookie != "")
                request.UnderlyingRequest.HttpContext.Response.Headers["Set-Cookie"] = newCookie;
        }

        /// <summary>
        ///    Closes the session and updates the cookie
        /// </summary>
        /// <param name="request"></param>
        public async Task Close(Request request)
        {
            var manager = request.ServerPlugins.Get<CookieSessions<TCookieSession>>();
            var closed = await manager.CloseSession(request.Cookies[manager._tokenName], out var cookie);
            if (closed)
                request.UnderlyingRequest.HttpContext.Response.Headers["Set-Cookie"] = cookie;
        }
    }
}
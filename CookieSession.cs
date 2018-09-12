using System;

namespace Red.CookieSessions
{
    public class CookieSession
    {
        private readonly CookieSessions _manager;

        internal CookieSession(object tsess, DateTime exp, CookieSessions manager)
        {
            _manager = manager;
            Data = tsess;
            Expires = exp;
        }

        public object Data { get; }
        public DateTime Expires { get; internal set; }


        /// <summary>
        ///    Renews the session expiry time and updates the cookie
        /// </summary>
        /// <param name="request"></param>
        public void Renew(Request request)
        {
            var existingCookie = request.Cookies[_manager._tokenName];
            var newCookie = _manager.RenewSession(existingCookie);
            if (newCookie != "")
                request.UnderlyingRequest.HttpContext.Response.Headers["Set-Cookie"] = newCookie;
        }

        /// <summary>
        ///    Closes the session and updates the cookie
        /// </summary>
        /// <param name="request"></param>
        public void Close(Request request)
        {
            if (_manager.CloseSession(request.Cookies[_manager._tokenName], out var cookie))
                request.UnderlyingRequest.HttpContext.Response.Headers["Set-Cookie"] = cookie;
        }
    }

    public class CookieSession<TSession> : CookieSession
    {
        internal CookieSession(TSession tsess, DateTime exp, CookieSessions manager) : base(tsess, exp, manager) { }

        public new TSession Data { get; }
    }
}
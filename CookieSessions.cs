using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Red;
using Red.Interfaces;

namespace Red.CookieSessions
{
    /// <summary>
    ///     RedMiddleware for CookieSessions
    /// </summary>
    public class CookieSessions : IRedMiddleware, IRedWebSocketMiddleware
    {
        /// <summary>chro
        /// Constructor for CookieSession Middleware
        /// </summary>
        /// <param name="settings">Settings object</param>
        public CookieSessions(CookieSessionSettings settings)
        {
            _settings = settings;
            _tokenName = settings.TokenName;
            var d = settings.Domain == "" ? "" : $" Domain={settings.Domain};";
            var p = settings.Path == "" ? "" : $" Path={settings.Path};";
            var h = settings.HttpOnly ? " HttpOnly;" : "";
            var s = settings.Secure ? " Secure;" : "";
            var ss = settings.SameSite == SameSiteSetting.None ? "" : $" SameSite={settings.SameSite};";
            _cookie = d + p + h + s + ss;
            _expiredCookie =
                $"{settings.TokenName}=;{_cookie} Expires=Thu, 01 Jan 1970 00:00:00 GMT; Max-Age={(int) settings.SessionLength.TotalSeconds};";
            ReapLoop();
        }

        internal readonly Random _random = new Random();
        internal readonly string _cookie;

        private readonly RandomNumberGenerator _tokenGenerator = RandomNumberGenerator.Create();

        internal readonly string _tokenName;
        private readonly string _expiredCookie;
        internal readonly CookieSessionSettings _settings;

        /// <summary>
        ///     Do not invoke. Is invoked by the server when it starts. 
        /// </summary>
        public void Initialize(RedHttpServer server)
        {
            server.Plugins.Register(this);
        }

        /// <summary>
        ///     Do not invoke. Is invoked by the server with every websocket request
        /// </summary>
        public async Task Process(Request req, WebSocketDialog wsd, Response res) => await Task.Run(() => Authenticate(req, res));

        /// <summary>
        ///     Do not invoke. Is invoked by the server with every request
        /// </summary>
        public async Task Process(Request req, Response res) => await Task.Run(() => Authenticate(req, res));

        // Simple maintainer loop
        private async void ReapLoop()
        {
            while (true)
            {
                await Task.Delay(_settings.ReapInterval);
                _settings.Store.RemoveExpired();
            }
        }

        /// <summary>
        /// Authenticates a request and sets the sessionData if valid, and responds with 401 when invalid
        /// </summary>
        /// <param name="req">The given request</param>
        /// <param name="res">The response for the request</param>
        /// <returns>True when valid</returns>
        public void Authenticate(Request req, Response res)
        {
            if (!req.Cookies.ContainsKey(_tokenName) || req.Cookies[_tokenName] == "")
            {
                return;
            }

            if (!TryAuthenticateToken(req.Cookies[_tokenName], out var session))
            {
                res.AddHeader("Set-Cookie", _expiredCookie);
                return;
            }

            if (_settings.AutoRenew)
            {
                session.Renew(req);
            }
            
            req.SetData(session);

        }

        private bool TryAuthenticateToken(string token, out CookieSession data)
        {
            if (!_settings.Store.TryGet(token, out var session))
            {
                data = null;
                return false;
            }
            else if (session.Expires >= DateTime.Now)
            {
                
                data = null;
                return false;
            }

            data = session;
            return true;
        }

        private string GenerateToken()
        {
            var data = new byte[32];
            _tokenGenerator.GetBytes(data);
            var b64 = Convert.ToBase64String(data);
            var id = new StringBuilder(b64, 46);
            id.Replace('+', (char) _random.Next(97, 122));
            id.Replace('=', (char) _random.Next(97, 122));
            id.Replace('/', (char) _random.Next(97, 122));
            return id.ToString();
        }

        internal string OpenSession<TSession>(TSession sessionData)
        {
            var id = GenerateToken();
            var exp = DateTime.UtcNow.Add(_settings.SessionLength);
            _settings.Store.Set(id, new CookieSession<TSession>(sessionData, exp, this));
            return $"{_tokenName}={id};{_cookie} Expires={exp:R}";
        }

        internal string RenewSession(string token)
        {
            if (!_settings.Store.TryGet(token, out var session))
            {
                return "";
            }
            session.Expires = DateTime.UtcNow.Add(_settings.SessionLength);
            _settings.Store.Set(token, session);
            return $"{_tokenName}={token};{_cookie} Expires={session.Expires:R}";
        }

        internal bool CloseSession(string token, out string cookie)
        {
            cookie = _expiredCookie;
            return _settings.Store.TryRemove(token);
        }

    }
}
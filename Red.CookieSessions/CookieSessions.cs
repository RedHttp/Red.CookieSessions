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
    public class CookieSessions<TCookieSession> : IRedMiddleware, IRedWebSocketMiddleware
    {
        public ICookieStore<TCookieSession> Store = new InMemoryCookieStore<TCookieSession>();
        
        
        /// <summary>
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
        public Task Process(Request req, WebSocketDialog wsd, Response res) => Authenticate(req, res);

        /// <summary>
        ///     Do not invoke. Is invoked by the server with every request
        /// </summary>
        public Task Process(Request req, Response res) => Authenticate(req, res);

        // Simple maintainer loop
        private async void ReapLoop()
        {
            while (true)
            {
                await Store.RemoveExpired();
                await Task.Delay(_settings.ReapInterval);
            }
        }

        /// <summary>
        /// Authenticates a request and sets the sessionData if valid, and responds with 401 when invalid
        /// </summary>
        /// <param name="req">The given request</param>
        /// <param name="res">The response for the request</param>
        /// <returns>True when valid</returns>
        public async Task Authenticate(Request req, Response res)
        {
            if (!req.Cookies.ContainsKey(_tokenName) || req.Cookies[_tokenName] == "")
            {
                return;
            }

            var auth = await TryAuthenticateToken(req.Cookies[_tokenName]);
            if (!auth.Item1)
            {
                res.AddHeader("Set-Cookie", _expiredCookie);
                return;
            }

            var session = auth.Item2;

            if (_settings.AutoRenew)
            {
                await session.Renew(req);
            }
            
            req.SetData(session);

        }

        private async Task<Tuple<bool, CookieSession<TCookieSession>>> TryAuthenticateToken(string token)
        {
            var got = await Store.TryGet(token);
            if (!got.Item1 || got.Item2.Expires <= DateTime.Now)
            {
                return new Tuple<bool, CookieSession<TCookieSession>>(false, null);
            }

            return new Tuple<bool, CookieSession<TCookieSession>>(true, got.Item2);
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

        internal async Task<string> OpenSession(TCookieSession sessionData)
        {
            var id = GenerateToken();
            var exp = DateTime.UtcNow.Add(_settings.SessionLength);
            await Store.Set(id, new CookieSession<TCookieSession>(sessionData, exp));
            return $"{_tokenName}={id};{_cookie} Expires={exp:R}";
        }

        internal async Task<string> RenewSession(string token)
        {
            var got = await Store.TryGet(token);
            if (!got.Item1)
            {
                return "";
            }

            var session = got.Item2;
            session.Expires = DateTime.UtcNow.Add(_settings.SessionLength);
            await Store.Set(token, session);
            return $"{_tokenName}={token};{_cookie} Expires={session.Expires:R}";
        }

        internal Task<bool> CloseSession(string token, out string cookie)
        {
            cookie = _expiredCookie;
            return Store.TryRemove(token);
        }
        
    }
}
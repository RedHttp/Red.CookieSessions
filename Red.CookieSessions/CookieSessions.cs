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
    public class CookieSessions<TCookieSession> : IRedMiddleware, IRedWebSocketMiddleware where TCookieSession : class, ICookieSession, new()
    {
        /// <summary>
        /// The storage for the sessions
        /// </summary>
        public ICookieStore<TCookieSession> Store = new InMemoryCookieStore<TCookieSession>();
        
        /// <summary>
        /// The length of a session
        /// </summary>
        public TimeSpan SessionLength;
        
        public string Domain = "";
        public string Path = "";
        public bool HttpOnly = true;
        public bool Secure = true;
        public SameSiteSetting SameSite = SameSiteSetting.Strict;
        
        /// <summary>
        /// The name of the cookie 
        /// </summary>
        public string TokenName = "session_token";
        
        /// <summary>
        /// How often expired sessions should be removed
        /// </summary>
        public TimeSpan ReapInterval = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Renew session on each authenticated request
        /// </summary>
        public bool AutoRenew = false;

        
        
        /// <summary>
        /// Constructor for CookieSession Middleware
        /// </summary>
        public CookieSessions(TimeSpan sessionLength)
        {
            SessionLength = sessionLength;
            _expiredCookie =
                $"{TokenName}=; Expires=Thu, 01 Jan 1970 00:00:00 GMT; Max-Age={(int) SessionLength.TotalSeconds};";
            ReapLoop();
        }

        private string GenerateCookie()
        {
            var d = Domain == "" ? "" : $" Domain={Domain};";
            var p = Path == "" ? "" : $" Path={Path};";
            var h = HttpOnly ? " HttpOnly;" : "";
            var s = Secure ? " Secure;" : "";
            var ss = SameSite == SameSiteSetting.None ? "" : $" SameSite={SameSite};";
            return $"{d}{p}{h}{s}{ss}";
        }
        private readonly Random _random = new Random();

        private readonly RandomNumberGenerator _tokenGenerator = RandomNumberGenerator.Create();

        private readonly string _expiredCookie;

        /// <summary>
        ///     Do not invoke. Is invoked by the server when it starts. 
        /// </summary>
        public void Initialize(RedHttpServer server)
        {
            server.Plugins.Register<CookieSessions<TCookieSession>, CookieSessions<TCookieSession>>(this);
        }

        /// <summary>
        ///     Do not invoke. Is invoked by the server with every websocket request
        /// </summary>
        public Task<HandlerType> Invoke(Request req, WebSocketDialog wsd, Response res) => Authenticate(req, res);

        /// <summary>
        ///     Do not invoke. Is invoked by the server with every request
        /// </summary>
        public Task<HandlerType> Invoke(Request req, Response res) => Authenticate(req, res);

        // Simple maintainer loop
        private async void ReapLoop()
        {
            while (true)
            {
                await Store.RemoveExpired();
                await Task.Delay(ReapInterval);
            }
        }

        /// <summary>
        /// Authenticates a request and sets the sessionData if valid, and responds with 401 when invalid
        /// </summary>
        /// <param name="req">The given request</param>
        /// <param name="res">The response for the request</param>
        /// <returns>True when valid</returns>
        public async Task<HandlerType> Authenticate(Request req, Response res)
        {
            if (!req.Cookies.ContainsKey(TokenName) || req.Cookies[TokenName] == "")
            {
                return HandlerType.Continue;
            }

            var auth = await TryAuthenticateToken(req.Cookies[TokenName]);
            if (!auth.Item1)
            {
                res.AddHeader("Set-Cookie", _expiredCookie);
                return HandlerType.Continue;
            }

            var session = auth.Item2;

            if (AutoRenew)
            {
                await session.Renew(req);
            }
            
            req.SetData(session);
            return HandlerType.Continue;
        }

        private async Task<(bool sucess, TCookieSession found)> TryAuthenticateToken(string token)
        {
            var (success, found) = await Store.TryGet(token);
            if (!success || found.Expires <= DateTime.Now)
            {
                return (false, null);
            }
            return (true, found);
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

        internal async Task<string> OpenSession(TCookieSession session) 
        {
            session.SessionId = GenerateToken();
            session.Expires = DateTime.UtcNow.Add(SessionLength);
            await Store.Set(session);
            return $"{TokenName}={session.SessionId}; {GenerateCookie()} Expires={session.Expires:R}";
        }

        internal async Task<string> RenewSession(TCookieSession session)
        {
            session.Expires = DateTime.UtcNow.Add(SessionLength);
            await Store.Set(session);
            return $"{TokenName}={session.SessionId}; {GenerateCookie()} Expires={session.Expires:R}";
        }

        internal Task<bool> CloseSession(TCookieSession session, out string cookie)
        {
            cookie = _expiredCookie;
            return Store.TryRemove(session.SessionId);
        }
        
    }
}
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
        where TCookieSession : class, ICookieSession, new()
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
            _expiredCookie = $"{TokenName}=; Expires=Thu, 01 Jan 1970 00:00:00 GMT;";
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
        public Task<HandlerType> Invoke(Request req, Response res, WebSocketDialog _) => Authenticate(req, res);

        /// <summary>
        ///     Do not invoke. Is invoked by the server with every request
        /// </summary>
        public Task<HandlerType> Invoke(Request req, Response res) => Authenticate(req, res);

        // Simple maintainer loop
        private async Task ReapLoop()
        {
            while (ReapInterval != default)
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
            if (!req.Cookies.TryGetValue(TokenName, out var token) || token == "")
                return HandlerType.Continue;

            var session = await TryAuthenticateToken(req.Cookies[TokenName]);
            if (session == null)
            {
                res.AspNetResponse.Headers["Set-Cookie"] = _expiredCookie;
                return HandlerType.Continue;
            }

            if (AutoRenew)
                await res.RenewSession(session);
            
            req.SetData(session);
            return HandlerType.Continue;
        }

        private async Task<TCookieSession?> TryAuthenticateToken(string token)
        {
            var found = await Store.TryGet(token);
            if (found == null || found.Expiration <= DateTime.Now)
            {
                return null;
            }

            return found;
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

        internal Task<string> OpenSession(TCookieSession session) 
        {
            session.Id = GenerateToken();
            return RenewSession(session);
        }

        internal Task<string> RenewSession(TCookieSession session)
        {
            session.Expiration = DateTime.UtcNow.Add(SessionLength);
            return SetSession(session);
        }

        internal async Task<string> SetSession(TCookieSession session)
        {
            await Store.Set(session);
            return $"{TokenName}={session.Id}; {GenerateCookie()} Expires={session.Expiration:R}";
        }
        
        
        internal Task<bool> CloseSession(TCookieSession session, out string cookie)
        {
            cookie = _expiredCookie;
            return Store.TryRemove(session.Id);
        }
        
    }
}
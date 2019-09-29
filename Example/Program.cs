using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Red;
using Red.CookieSessions;

namespace Example
{
    class MySession : CookieSessionBase
    {
        public string Username;
    }
    class Program
    {
        private static async Task<HandlerType> Auth(Request req, Response res)
        {
            if (req.GetData<MySession>() == null)
            {
                await res.SendStatus(HttpStatusCode.Unauthorized);
                return HandlerType.Final;
            }
            return HandlerType.Continue;
        }
        public static async Task Main(string[] args)
        {
            // We serve static files, such as index.html from the 'public' directory
            var server = new RedHttpServer(5000, "public");

            server.Use(new CookieSessions<MySession>(TimeSpan.FromDays(5))
            {
                Secure = false
            });

            server.Get("/", Auth, (req, res) =>
            {
                var session = req.GetData<MySession>();
                return res.SendString($"Hi {session.Username}");
            });

            server.Get("/login", async (req, res) =>
            {
                // To make it easy to test the session system only using the browser and no credentials
                await res.OpenSession(new MySession {Username = "benny"});
                return await res.SendStatus(HttpStatusCode.OK);
            });

            server.Get("/logout", Auth, async (req, res) =>
            {
                await res.CloseSession(req.GetData<MySession>());
                return await res.SendStatus(HttpStatusCode.OK);
            });
            await server.RunAsync();
        }
    }
}
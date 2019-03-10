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
        private static async Task Auth(Request req, Response res)
        {
            if (req.GetSession<MySession>() == null)
            {
                await res.SendStatus(HttpStatusCode.Unauthorized);
            }
        }
        public static async Task Main(string[] args)
        {
            // We serve static files, such as index.html from the 'public' directory
            var server = new RedHttpServer(5000, "public");

            server.Use(new CookieSessions<MySession>(TimeSpan.FromDays(5))
            {
                Secure = false
            });



            server.Get("/", Auth, async (req, res) =>
            {
                var session = req.GetSession<MySession>();
                await res.SendString($"Hi {session.Username}");
            });

            server.Get("/login", async (req, res) =>
            {
                // To make it easy to test the session system only using the browser and no credentials
                await req.OpenSession(new MySession {Username = "benny"});
                await res.SendStatus(HttpStatusCode.OK);
            });

            server.Get("/logout", Auth, async (req, res) =>
            {
                await req.GetSession<MySession>().Close(req);
                await res.SendStatus(HttpStatusCode.OK);
            });
            await server.RunAsync();
        }
    }
}
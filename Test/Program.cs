using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Red;
using Red.CookieSessions;

namespace Test
{
    class Program
    {
        class MySess
        {
            public string Username;
        }

        public static async Task Main(string[] args)
        {
            // We serve static files, such as index.html from the 'public' directory
            var server = new RedHttpServer(5000, "public");

            var sessions = new CookieSessions<MySess>(new CookieSessionSettings(TimeSpan.FromDays(5))
            {
                Secure = false
            });

            server.Use(sessions);


            async Task Auth(Request req, Response res)
            {
                if (req.GetSession<MySess>() == null)
                {
                    await res.SendStatus(HttpStatusCode.Unauthorized);
                }
            }

            // URL param demo
            server.Get("/:param1/:paramtwo/:somethingthird", Auth, async (req, res) =>
            {
                var session = req.GetSession<MySess>();
                await res.SendString(
                    $"Hi {session.Data.Username} URL: {req.Parameters["param1"]} / {req.Parameters["paramtwo"]} / {req.Parameters["somethingthird"]}");
            });

            server.Get("/login", async (req, res) =>
            {
                // To make it easy to test the session system only using the browser and no credentials
                await req.OpenSession(new MySess {Username = "benny"});
                await res.SendStatus(HttpStatusCode.OK);
            });

            server.Post("/login", async (req, res) =>
            {
                // Here we could authenticate the user properly, with credentials sent in a form, or similar
                await req.OpenSession(new MySess {Username = "benny"});
                await res.SendStatus(HttpStatusCode.OK);
            });

            server.Get("/logout", Auth, async (req, res) =>
            {
                await req.GetSession<MySess>().Close(req);
                await res.SendStatus(HttpStatusCode.OK);
            });
            await server.RunAsync();
        }
    }
}
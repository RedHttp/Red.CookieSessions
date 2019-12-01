# Cookie Sessions for RedHttpServer
### Simple session management middleware for Red. 
[![GitHub](https://img.shields.io/github/license/redhttp/red.cookiesessions)](https://github.com/RedHttp/Red.CookieSessions/blob/master/LICENSE.md)
[![Nuget](https://img.shields.io/nuget/v/red.cookiesessions)](https://www.nuget.org/packages/red.cookiesessions/)
[![Nuget](https://img.shields.io/nuget/dt/red.cookiesessions)](https://www.nuget.org/packages/red.cookiesessions/)
![Dependent repos (via libraries.io)](https://img.shields.io/librariesio/dependent-repos/nuget/red.cookiesessions)

### SessionStores already available:
- Entity Framework Core
- Redis
- LiteDB
- SQLite

### Usage
After installing and referencing this library, the `Red.Response` has the extension methods `OpenSession(TSession session)`, `RenewSession(TSession session)` and `CloseSession(TSession session)`

### Example
```csharp
class MySession : CookieSessionBase
{
    public string Username;
}
...

var server = new RedHttpServer(5000, "public");

server.Use(new CookieSessions<MySession>(TimeSpan.FromDays(5)));


async Task Auth(Request req, Response res)
{
	if (req.GetSession<MySession>() == null)
	{
		await res.SendStatus(HttpStatusCode.Unauthorized);
	}
}

server.Get("/", Auth, async (req, res) =>
{
    var session = req.GetSession<MySession>();
    await res.SendString($"Hi {session.Username}");
});

server.Get("/login", async (req, res) =>
{
    // To make it easy to test the session system only using the browser and no credentials
    // Would most likely be a POST-request in the real world
    await res.OpenSession(new MySession { Username = "benny" });
    await res.SendStatus(HttpStatusCode.OK);
});

server.Get("/logout", Auth, async (req, res) =>
{
    var session = req.GetData<MySession>();
    await res.CloseSession(session);
    await res.SendStatus(HttpStatusCode.OK);
});
await server.RunAsync();
```

#### Implementation
`OpenSession` will open a new session and attach a `Set-Cookie` header to the associated response. 
This header's value contains the token used for authentication. 
The token is generated using the `RandomNumberGenerator` from `System.Security.Cryptography`, 
so it shouldn't be too easy to "guess" other tokens, even with knowledge of some tokens.

# Cookie Sessions for RedHttpServer
Simple session management middleware for Red. 

### Usage
After installing and referencing this library, the `Red.Request` has the extension methods `OpenSession(sessionData)` and `GetSession()`.

`OpenSession(sessionData)` will open a new session and add a header to the response associated with the request.

`GetSession<TSession>()` will return the `CookieSession` object wrapping the `TSession`-data, which has two methods: `Renew()` and `Close()`, and the field `Data`, which holds the session-data object


### Example
```csharp
class MySess : CookieSessionBase
{
    public string Username;
}
...

var server = new RedHttpServer(5000, "public");

var sessions = new CookieSessions<MySess>(TimeSpan.FromDays(5))
{
    Secure = false
};

server.Use(sessions);


async Task Auth(Request req, Response res)
{
	if (req.GetSession<MySess>() == null)
	{
		await res.SendStatus(HttpStatusCode.Unauthorized);
	}
}

server.Get("/", Auth, async (req, res) =>
{
    var session = req.GetSession<MySess>();
    await res.SendString($"Hi {session.Username}");
});

server.Get("/login", async (req, res) =>
{
    // To make it easy to test the session system only using the browser and no credentials
    // Would most likely be a POST-request in the real world
    await req.OpenSession(new MySess {Username = "benny"});
    await res.SendStatus(HttpStatusCode.OK);
});

server.Get("/logout", Auth, async (req, res) =>
{
    await req.GetSession<MySess>().Close(req);
    await res.SendStatus(HttpStatusCode.OK);
});
await server.RunAsync();
```

#### Implementation
`OpenSession` will open a new session and attach a `Set-Cookie` header to the associated response. 
This header's value contains the token used for authentication. 
The token is generated using the `RandomNumberGenerator` from `System.Security.Cryptography`, 
so it shouldn't be too easy to "guess" other tokens, even with knowledge of some tokens.


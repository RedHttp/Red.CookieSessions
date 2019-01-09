# Cookie Sessions for RedHttpServer
Simple session management middleware for Red. 

### Usage
After installing and referencing this library, the `Red.Request` has the extension methods `OpenSession(sessionData)` and `GetSession()`.

`OpenSession(sessionData)` will open a new session and add a header to the response associated with the request.

`GetSession<TSession>()` will return the `CookieSession` object wrapping the `TSession`-data, which has two methods: `Renew()` and `Close()`, and the field `Data`, which holds the session-data object


### Example
```csharp
class MySession 
{
    public string Username;
}
...

var server = new RedHttpServer();
server.Use(new CookieSessions<MySession>(new CookieSessionSettings<MySession>(TimeSpan.FromDays(1))
{
    Secure = false
}));

async Task Auth(Request req, Response res)
{
    if (req.GetSession<MySession>() == null)
    {
        await res.SendStatus(HttpStatusCode.Unauthorized);
    }
}

server.Get("/login", async (req, res) =>
{
    await req.OpenSession(new MySession {Username = "test"});
    await res.SendStatus(HttpStatusCode.OK);
});

server.Post("/login", async (req, res) =>
{
    var form = await req.GetFormDataAsync();
    if (ValidForm(form) && Login(form["username"], form["password"]))
    {
        await req.OpenSession(new MySession {Username = form["username"]});
        await res.SendStatus(HttpStatusCode.OK);
    }
    else 
        await res.SendStatus(HttpStatusCode.BadRequest);
});

// Only authenticated users are allowed to /friends
server.Get("/friends", Auth, async (req, res) => 
{
    var session = req.GetSession<MySession>();
    var friends = GetFriendsOfUser(session.Data.Username);
    await res.SendJson(friends);
});

server.Post("/logout", async (req, res) => 
{
    await req.GetSession<MySession>().Close(req);
    await res.SendStatus(HttpStatusCode.OK);
});

await server.RunAsync();
```

#### Implementation
`OpenSession` will open a new session and attach a `Set-Cookie` header to the associated response. 
This header's value contains the token used for authentication. 
The token is generated using the `RandomNumberGenerator` from `System.Security.Cryptography`, 
so it shouldn't be too easy to "guess" other tokens, even with knowledge of some tokens.


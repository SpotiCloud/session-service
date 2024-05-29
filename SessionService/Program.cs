using Microsoft.Extensions.Configuration;
using SessionService.Objects;
using SessionService.Services.AzureBlob;
using SessionService.Services.Event;
using SessionService.Services.Test;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

IConfigurationRoot Configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddHostedService<TestService>();

var connectionString = Configuration.GetConnectionString("AzureStorage");
builder.Services.AddSingleton(new AzureBlobService(connectionString));

var app = builder.Build();

// Dictionary to store connected clients in each room
var rooms = new Dictionary<string, List<WebSocket>>();

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Add WebSocket middleware
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

            // Handle joining a room
            var roomName = context.Request.Query["room"];
            if (!string.IsNullOrEmpty(roomName))
            {
                if (!rooms.ContainsKey(roomName))
                {
                    rooms[roomName] = new List<WebSocket>();
                }
                rooms[roomName].Add(webSocket);
            }

            await HandleWebSocketAsync(webSocket, roomName);
        }
        else
        {
            context.Response.StatusCode = 400; // Bad Request
        }
    }
    else
    {
        await next();
    }
});

/*app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocketAsync(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400; // Bad Request
        }
    }
    else
    {
        await next();
    }
});*/

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; connect-src 'self' ws://localhost:5179");
    await next();
});

/*app.Run(async (context) =>
{
    await context.Response.WriteAsync("Hello World!");
});*/

app.Run();

async Task HandleWebSocketAsync(WebSocket webSocket, string roomName)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    Console.WriteLine(result);

    while (!result.CloseStatus.HasValue)
    {
        // Broadcast message to all clients in the same room
        if (!string.IsNullOrEmpty(roomName) && rooms.ContainsKey(roomName))
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var serverMsg = Encoding.UTF8.GetBytes(message);

            var tasks = rooms[roomName].Where(ws => ws != webSocket)
                                       .Select(ws => ws.SendAsync(new ArraySegment<byte>(serverMsg, 0, serverMsg.Length), result.MessageType, result.EndOfMessage, CancellationToken.None))
                                       .ToArray();

            await Task.WhenAll(tasks);
        }

        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }

    // Remove client from room when the connection is closed
    if (!string.IsNullOrEmpty(roomName) && rooms.ContainsKey(roomName))
    {
        rooms[roomName].Remove(webSocket);
    }

    //await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}

/*async Task HandleWebSocketAsync(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

    while (!result.CloseStatus.HasValue)
    {
        var serverMsg = Encoding.UTF8.GetBytes($"Server: Hello! Time: {DateTime.Now}");
        await webSocket.SendAsync(new ArraySegment<byte>(serverMsg, 0, serverMsg.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);

        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }

    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}*/
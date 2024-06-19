using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SessionService.Data;
using SessionService.Objects;
using SessionService.Services.Artist;
using SessionService.Services.AzureBlob;
using SessionService.Services.Event;
using SessionService.Services.Test;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add authentication services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{

    //authority runs behind reverse proxy
    o.RequireHttpsMetadata = false;

    o.Authority = builder.Configuration["Jwt:Authority"];
    o.Audience = builder.Configuration["Jwt:Audience"];


    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false
    };

});

// Configure DbContext with environment variables
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(
        $"Host={builder.Configuration["POSTGRES_HOST_NAME"]};Port=5432;Database=artist;Username={builder.Configuration["POSTGRES_USERNAME"]};Password={builder.Configuration["POSTGRES_PASSWORD"]}"
    )
);

// Add services to the container.
builder.Services.AddControllers();
builder.Configuration.AddEnvironmentVariables();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setup =>
{
    setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Music API v1.0", Version = "v1" });
    setup.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("http://localhost:8180/realms/SpotiCloud/protocol/openid-connect/auth"),
            }
        }
    });
    setup.AddSecurityRequirement(new OpenApiSecurityRequirement{
    {
        new OpenApiSecurityScheme{
            Reference = new OpenApiReference{
                Type = ReferenceType.SecurityScheme,
                Id = "OAuth2" //The name of the previously defined security scheme.
            }
        },
        new string[] {}
    }
    });
});

string accountName = builder.Configuration["AZURE_STORAGE_ACCOUNT_NAME"];
string accountKey = builder.Configuration["AZURE_STORAGE_ACCOUNT_KEY"];

var connectionString = $"DefaultEndpointsProtocol=http;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

builder.Services.AddSingleton(new AzureBlobService(connectionString));
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddHostedService<TestService>();

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
    if (context.Request.Path == "/session/ws")
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; connect-src 'self' ws://session-service:5179");
    await next();
});


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dbContext.Database.Migrate();
}

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
}
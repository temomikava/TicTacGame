using WebAPI.Core.Interface;
using WebAPI.Core.Services;
using WebAPI.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using WebAPI;
using Newtonsoft.Json;
using GameLibrary;
using Serilog;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
//builder.Services.AddSignalR();
builder.Services.AddSignalR()
    .AddNewtonsoftJsonProtocol(opts =>
        opts.PayloadSerializerSettings.TypeNameHandling = TypeNameHandling.Auto);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IDatabaseConnection, NpgsqlConnector>();
builder.Services.AddSingleton<IUserIdProvider, TicTacUserIdProvider>();

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.AllowAnyMethod()
                         .SetIsOriginAllowed(_ => true)
                         .AllowCredentials()
                         .AllowAnyHeader();
                      });
});
var app = builder.Build();
app.UseCors(MyAllowSpecificOrigins);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TicTacToe WebServices API V1");
        c.RoutePrefix = string.Empty;
    });

}
app.UseCors(MyAllowSpecificOrigins);
Guid guid = new Guid();
app.Use(async (context, next) =>
{

    if (context.Request.Path.StartsWithSegments("/signalr"))
    {
        
        var sessionId = context.Request.Headers.Authorization.ToString()?.Split()?.LastOrDefault();
        var isValidSessionId = Guid.TryParse(sessionId, out var validSessionId);
        if (isValidSessionId)
        {
            
            guid = validSessionId;
        }
        
        var dal = context.RequestServices.GetRequiredService<IDatabaseConnection>();
        var id = dal.GetUserId(guid);

        if (id == -1)
        {
            context.Response.StatusCode = 401;
            return;
        }

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.Name, id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, validSessionId.ToString()));
        context.User.AddIdentity(identity);
        await next.Invoke();
        return;
    }
    await next.Invoke();

});

app.MapHub<GameHub>("signalr");


app.MapControllers();


app.Run();

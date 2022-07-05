using WebAPI.Core.Interface;
using WebAPI.Core.Services;
using WebAPI.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using WebAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IDatabaseConnection, NpgsqlConnector>();


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

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/signalr"))
    {
        await next();
        return;
    }


    var connectionId = context.Request.Headers["Authorization"].ToString()?.Split()?.LastOrDefault();
    if (string.IsNullOrEmpty(connectionId))
    {
        context.Response.StatusCode = 401;
        return;
    }

    var identity = new ClaimsIdentity();
    identity.AddClaim(new Claim(ClaimTypes.Name, connectionId));
    context.User.AddIdentity(identity);

    await next();
});


app.MapHub<GameHub>("signalr");

app.UseCors(MyAllowSpecificOrigins);

//app.UseMiddleware<WebSocketsMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

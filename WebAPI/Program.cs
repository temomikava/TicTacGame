using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebAPI.Core.Interface;
using WebAPI.Core.Services;
using WebAPI.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ServiceStack.Text;

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
                          builder.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin();
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




app.MapHub<GameHub>("signalr/gamehub");
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();

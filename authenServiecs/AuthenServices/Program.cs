using System.Text;
using AuthenServices;
using AuthenServices.Helpers;
using CacheLite;

//using CacheLite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration.
                                        AddJsonFile(builder.Configuration["SettingFile"]
                                        ,false,
                                        reloadOnChange:true )
                                        .Build(); 

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();


void addSerilog()
{
    string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{TraceId}] [{UserId}] {Message:lj}{NewLine}{Exception}";

    Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .Enrich.FromLogContext()
                    .WriteTo.Async(a =>
                    {
                        a.Console(outputTemplate: template);
                    })
                    .WriteTo.Logger(lc =>
                    {
                        lc.WriteTo.Map("UserId", string.Empty, (name, wt) => wt.Async(a =>
                        {
                            a.File($"{Const.LOGS}/{DateTime.Now:yyyy}/{DateTime.Now:MM}/{DateTime.Now:dd}/{name}/.log",
                                    outputTemplate: template,
                                    rollingInterval: RollingInterval.Day,
                                    fileSizeLimitBytes: 10485760,
                                    rollOnFileSizeLimit: true,
                                    shared: true);
                        }));
                    })
                    .CreateLogger();
}

void AddAuthentication()
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])),
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = false
            };

            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token = context.Request.Cookies[Const.ACCESS_TOKEN];
                    if (string.IsNullOrWhiteSpace(token) == false)
                    {
                        var cache = context.Request.HttpContext.RequestServices.GetKeyedService<Cache>(Const.JWT_CACHE);
                        if (cache.GetValue(token, out string userId))
                        {
                            context.Token = token;

                            using var scope = context.Request.HttpContext.RequestServices.CreateScope();
                            var configs = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<Configs>>().Value;
                            cache.UpdateAsync(token, userId, configs.ExprireToken).GetAwaiter().GetResult();
                        }
                    }
                    return Task.CompletedTask;
                },
            };
        });
}
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VerbIt.Backend.Models;
using VerbIt.Backend.Repositories;
using VerbIt.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// -- ConfigureServices --

IConfigurationSection? jwtConfigSection = builder.Configuration.GetSection(JwtSettings.Key);
builder.Services.AddOptions<JwtSettings>().Bind(jwtConfigSection);
JwtSettings jwtSettings = jwtConfigSection.Get<JwtSettings>();

builder.Services
    .AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
            ),
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
        };
    });
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IVerbitRepository, VerbitRepository>();
builder.Services.AddTransient<IVerbitAuthService, VerbitAuthService>();

var app = builder.Build();

// -- Configure --

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseBlazorFrameworkFiles();

app.UseHttpsRedirection();

app.UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
    {
        // Web API endpoints
        endpoints.MapControllers();

        // Blazor WASM endpoint
        endpoints.MapFallbackToFile("index.html");
    });

app.Run();

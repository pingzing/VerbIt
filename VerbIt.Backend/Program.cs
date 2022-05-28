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

// Auth
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
        };
    });

// MVC and Blazor
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Local services
builder.Services.AddTransient<IVerbitRepository, VerbitRepository>();
builder.Services.AddTransient<IVerbitAuthService, VerbitAuthService>();

var app = builder.Build();

// -- Configure --

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

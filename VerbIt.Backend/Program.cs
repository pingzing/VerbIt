using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VerbIt.Backend.Models;
using VerbIt.Backend.Repositories;
using VerbIt.Backend.Services;
using Microsoft.Extensions.Azure;
using Microsoft.AspNetCore.Diagnostics;
using VerbIt.Backend;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

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
builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddRazorPages();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo { Title = "VerbIt Backend", Version = "v1" });
    opts.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        }
    );
    opts.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        }
    );
});

// Setting up Azure SDKs
builder.Services.AddAzureClients(clientBuilder =>
{
    TableStorageSettings tableOptions = builder.Configuration
        .GetSection(TableStorageSettings.ConfigKey)
        .Get<TableStorageSettings>();

    clientBuilder
        .AddTableServiceClient(tableOptions.ConnectionString)
        .ConfigureOptions(opts =>
        {
            // Add a policy that adds the "Prefer: return-content" header so that we
            // get inserted entities back in the response.
            opts.AddPolicy(new PreferReturnContentPolicy(), Azure.Core.HttpPipelinePosition.PerRetry);
        });
});

// Local services
builder.Services.AddSingleton<IVerbitAuthService, VerbitAuthService>();
builder.Services.AddSingleton<IVerbitRepository, VerbitRepository>();
builder.Services.AddSingleton<IMasterListService, MasterListService>();

var app = builder.Build();

// -- Configure --

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
    app.UseHttpLogging();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        IExceptionHandlerPathFeature? exceptionHandler = context.Features.Get<IExceptionHandlerPathFeature>()!;
        if (exceptionHandler?.Error is StatusCodeException ex)
        {
            // This exception is one we throw on purpose, to do a fail-fast from any layer of the app.
            context.Response.StatusCode = ex.StatusCode;
            if (ex.Message != null)
            {
                await context.Response.WriteAsync(ex.Message);
            }
        }
        else
        {
            // If it's a real uncaught exception, hide all details, and just return blank 500.
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    });
});
app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

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
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

// -- ConfigureServices --

// Set up logging

// Bootstrap logger
Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.Debug().CreateBootstrapLogger();
Log.Information("VerbIt backend starting up...");

try
{
    // Register Serilog
    // csharpier-ignore
    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Options
    IConfigurationSection? jwtConfigSection = builder.Configuration.GetSection(JwtSettings.Key);
    builder.Services.AddOptions<JwtSettings>().Bind(jwtConfigSection);
    builder.Services
        .AddOptions<TableStorageSettings>()
        .Bind(builder.Configuration.GetRequiredSection(TableStorageSettings.ConfigKey));

    // Auth
    builder.Services
        .AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(opts =>
        {
            JwtSettings jwtSettings = jwtConfigSection.Get<JwtSettings>();
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
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

        clientBuilder.AddTableServiceClient(tableOptions.ConnectionString);
    });

    // Local services
    builder.Services.AddSingleton<IVerbitAuthService, VerbitAuthService>();
    builder.Services.AddSingleton<IVerbitRepository, VerbitRepository>();
    builder.Services.AddSingleton<IMasterListService, MasterListService>();
    builder.Services.AddSingleton<ITestsService, TestsService>();

    var app = builder.Build();

    // -- Configure --

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseWebAssemblyDebugging();
        app.UseSerilogRequestLogging();
    }
    else
    {
        app.UseHsts();
    }

    app.UseExceptionHandler(handler =>
    {
        handler.Run(async context =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger>();
            IExceptionHandlerPathFeature? exceptionHandler = context.Features.Get<IExceptionHandlerPathFeature>()!;

            logger.LogError(exceptionHandler.Error, "Returning error to caller.");

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

    app.UseBlazorFrameworkFiles().UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorPages();
    app.MapControllers();
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to start up.");
}
finally
{
    Log.CloseAndFlush();
}

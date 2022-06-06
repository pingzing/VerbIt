using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.ComponentModel;
using VerbIt.Client;
using VerbIt.Client.Authentication;
using VerbIt.Client.Converters;
using VerbIt.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

TypeDescriptor.AddAttributes(typeof(List<string>), new TypeConverterAttribute(typeof(StringCollectionToStringConverter)));

// Create and add an HttpClient that uses the custom RedirectingAuthHandler.
builder.Services.AddScoped<RedirectingAuthHandler>();
builder.Services
    .AddHttpClient("RedirectingAuthClient", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<RedirectingAuthHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("RedirectingAuthClient"));

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<INetworkService, NetworkService>();
builder.Services.AddSingleton<ICsvImporterService, CsvImporterService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<DashboardTokenWatcherService>();

var host = builder.Build();

// Start up the singleton event watcher
var watcherService = host.Services.GetRequiredService<DashboardTokenWatcherService>();

await host.RunAsync();

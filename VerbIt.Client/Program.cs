using Blazor.Extensions.Logging;
using Blazored.LocalStorage;
using Blazored.Modal;
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

builder.Services.AddScoped(sp =>
{
    return new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});

if (!builder.HostEnvironment.IsProduction())
{
    builder.Services.AddLogging(builder => builder.AddBrowserConsole().SetMinimumLevel(LogLevel.Error));
}
else
{
    builder.Services.AddLogging(builder => builder.AddBrowserConsole().SetMinimumLevel(LogLevel.Debug));
}
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredModal();
builder.Services.AddScoped<INetworkService, NetworkService>();
builder.Services.AddSingleton<ICsvImporterService, CsvImporterService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<DashboardTokenWatcherService>();

var host = builder.Build();

await host.RunAsync();

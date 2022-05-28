using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VerbIt.Client;
using VerbIt.Client.Authentication;
using VerbIt.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
{
    return new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<INetworkService, NetworkService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    provider => provider.GetRequiredService<JwtAuthStateProvider>()
);

await builder.Build().RunAsync();

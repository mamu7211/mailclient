using System.Globalization;
using Feirb.Web;
using Feirb.Web.Http;
using Feirb.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddLocalization();
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
});

builder.Services.AddSingleton<NotificationService>();
builder.Services.AddScoped<ToolbarStateService>();
builder.Services.AddScoped<BreadcrumbOverrideService>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());

builder.Services.AddTransient<CultureDelegatingHandler>();
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddTransient<ErrorNotificationDelegatingHandler>();
builder.Services.AddHttpClient("FeirbApi", client =>
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<ErrorNotificationDelegatingHandler>()
    .AddHttpMessageHandler<CultureDelegatingHandler>()
    .AddHttpMessageHandler<AuthDelegatingHandler>();
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("FeirbApi"));

var host = builder.Build();

var jsInterop = host.Services.GetRequiredService<IJSRuntime>();
var storedCulture = await jsInterop.InvokeAsync<string?>("blazorCulture.get");
var cultureName = storedCulture ?? CultureInfo.CurrentCulture.Name;
if (string.IsNullOrEmpty(cultureName))
    cultureName = "en-US";

var culture = new CultureInfo(cultureName);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();

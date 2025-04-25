using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SimpleWebWorkersExample;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.WebWorkers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBlazorJSRuntime();
builder.Services.AddWebWorkerService();

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().BlazorJSRunAsync();

# SimpleWebWorkersExample

This is a simple C# Blazor WebAssembly project that demonstrates how to use [SpawnDev.BlazorJS.WebWorkers](https://github.com/LostBeard/SpawnDev.BlazorJS.WebWorkers) to run a background task.

Below is are the modfied files in `Blazor WebAssembly Standalone App` template.

## Add SpawnDev.BlazorJS.WebWorkers reference
SimpleWebWorkersExample.csproj
```xml
...
  <ItemGroup>
    <PackageReference Include="SpawnDev.BlazorJS.WebWorkers" Version="2.12.0" />
  </ItemGroup>
...
```

## Add services
Program.cs  
```cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SimpleWebWorkersExample;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.WebWorkers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add SpawnDev.BlazorJS.BlazorJSRuntime
builder.Services.AddBlazorJSRuntime();

// Add SpawnDev.BlazorJS.WebWorkers.WebWorkerService
builder.Services.AddWebWorkerService();

// build and Init using BlazorJSRunAsync (instead of RunAsync)
await builder.Build().BlazorJSRunAsync();
```

## Use WebWorkerService
Home.razor
```razor
@page "/"
@using SpawnDev.BlazorJS
@using SpawnDev.BlazorJS.WebWorkers

<PageTitle>WebWorker Example</PageTitle>

<h1>WebWorker Example</h1>

<p>This WebWorker example will call the method 'CancellableMethod()' in a web worker which will simply loop for 10 seconds or until cancelled using a cancellation token via a button press.</p>
<p>The CancellableMethod method running in the web worker will report its progress back to the caller via an Action progress callback.</p>

<button disabled="@(cancellationTokenSource != null)" class="btn btn-primary" @onclick="StartBackgroundTask">Start backgound Task</button>
<button disabled="@(cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)" class="btn btn-primary" @onclick="CancelBackgroundTask">Cancel backgound Task</button>

<p role="status">Current count: @currentCount</p>

<p>
    @foreach (var msg in log)
    {
        <div>@msg</div>
    }
</p>

@code {
    [Inject]
    BlazorJSRuntime JS { get; set; } = default!;

    [Inject]
    WebWorkerService WebWorkerService { get; set; } = default!;
    CancellationTokenSource? cancellationTokenSource = null;
    private int currentCount = 0;
    List<string> log = new List<string>();

    void CancelBackgroundTask()
    {
        cancellationTokenSource?.Cancel();
    }
    private async Task StartBackgroundTask()
    {
        log.Clear();
        log.Add($"StartBackgroundTask called in scope: {JS.GlobalScope.ToString()}");
        cancellationTokenSource = new CancellationTokenSource();
        var maxRunTimeMS = 10000;
        var token = cancellationTokenSource.Token;
        StateHasChanged();
        // call method in a web worker using TaskPool
        var cancelled = await WebWorkerService.TaskPool.Run(() => CancellableMethod(null!, maxRunTimeMS, ProgressCallbackTask, LogCallback, token));
        log.Add($"StartBackgroundTask returned cancelled: '{cancelled}' to scope: {JS.GlobalScope.ToString()}");
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
    }
    /// <summary>
    /// This method will be called by the web worker scope and run in the window scope
    /// </summary>
    /// <param name="i"></param>
    void ProgressCallbackTask(int i)
    {
        currentCount = i;
        StateHasChanged();
    }
    /// <summary>
    /// This method will be called by the web worker scope and run in the window scope
    /// </summary>
    /// <param name="msg">log message</param>
    void LogCallback(string msg)
    {
        log.Add(msg);
        StateHasChanged();
    }
    /// <summary>
    /// This method will run in a web worker
    /// </summary>
    /// <param name="maxRunTimeMS">callback to report progress to the caller</param>
    /// <param name="progressCallback">callback to report status messages to the caller</param>
    /// <param name="token"></param>
    /// <returns>Returns true if the method was cancelled</returns>
    private static async Task<bool> CancellableMethod([FromServices] BlazorJSRuntime JS, double maxRunTimeMS, Action<int> progressCallback, Action<string> logCallback, CancellationToken token)
    {
        logCallback($"CancellableMethod called in scope: {JS.GlobalScope.ToString()}");
        var startTime = DateTime.Now;
        var maxRunTime = TimeSpan.FromMilliseconds(maxRunTimeMS);
        var i = 0;
        while (DateTime.Now - startTime < maxRunTime)
        {
            i++;
            progressCallback(i);
            // do some work ...
            // check if cancelled using IsCancellationRequestedAsync extension method
            // internally calls Task.Delay(1) to allow event handlers time to receive a cancel message
            if (await token.IsCancellationRequestedAsync())
            {
                logCallback($"CancellableMethod cancelled in scope: {JS.GlobalScope.ToString()}");
                return true;
            }
        }
        logCallback($"CancellableMethod finished in scope: {JS.GlobalScope.ToString()}");
        return false;
    }
}
```
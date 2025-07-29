using DummyMCP.Options;
using DummyMCP.Tools;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenWeatherMap.NetClient;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add configuration from environment variables prefixed with "DUMMY_".
builder.Configuration.AddEnvironmentVariables("DUMMY_");
// Add configuration for OpenWeatherMap API.
builder.Services.AddOptions<OpenWeatherMapApiOptions>()
    .Bind(builder.Configuration.GetSection("OWM"))
    .ValidateDataAnnotations();

// Register the OpenWeatherMap API client as a singleton service.
// This client will be used to interact with the OpenWeatherMap API.
builder.Services.AddSingleton<IOpenWeatherMap>(sp =>
    {
        // Create a configuration object for OpenWeatherMap API options.
        var config = sp.GetRequiredService<IOptions<OpenWeatherMapApiOptions>>();

        // Return a new instance of OpenWeatherMapClient with the logger and options.
        return new OpenWeatherMapClient(config.Value.ApiKey);
    });

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<WeatherTools>();

await builder.Build().RunAsync();

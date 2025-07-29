using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace DummyMCP.Options;

internal class OpenWeatherMapApiOptions
{
    [ConfigurationKeyName("API_KEY")]
    [RegularExpression("^[a-f0-9]{32}$", ErrorMessage = "Invalid API key format. It should be a 32-character hexadecimal string.")]
    public required string ApiKey { get; set; }
}

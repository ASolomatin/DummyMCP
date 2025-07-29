using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using OpenWeatherMap.NetClient;
using OpenWeatherMap.NetClient.Enums;
using OpenWeatherMap.NetClient.Models;

using Refit;

namespace DummyMCP.Tools;

/// <summary>
/// WeatherTools provides MCP server tools for interacting with the OpenWeatherMap API.
/// These tools allow users to query current weather, forecasts, and weather alerts for specified cities.
/// </summary>
[McpServerToolType]
public partial class WeatherTools(
    ILogger<WeatherTools> logger,
    IOpenWeatherMap apiClient
)
{
    [McpServerTool]
    [Description("Gets current weather conditions for the specified city.")]
    public async ValueTask<string> GetCurrentWeather(
        [Description("The city name to get weather for")] string city,
        [Description("Optional: Country code (e.g., 'US', 'UK')")] string? countryCode = null
    )
    {
        return await QueryApi(apiClient.CurrentWeather.QueryAsync, city, countryCode);
    }

    [McpServerTool]
    [Description("Gets weather forecast for the specified city.")]
    public async Task<string> GetWeatherForecast(
        [Description("The city name to get forecast for")] string city,
        [Description("Optional: Country code (e.g., 'US', 'UK')")] string? countryCode = null
    )
    {
        return await QueryApi(q => apiClient.Forecast5Days.QueryAsync(q), city, countryCode);
    }

    [McpServerTool]
    [Description("Gets weather alerts for the specified city.")]
    public async Task<string> GetWeatherAlerts(
        [Description("The city name to get alerts for")] string city,
        [Description("Optional: Country code (e.g., 'US', 'UK')")] string? countryCode = null
    )
    {
        IEnumerable<OneCallCategory> excludeCategories =
        [
            OneCallCategory.Current,
            OneCallCategory.Minutely,
            OneCallCategory.Hourly,
            OneCallCategory.Daily,
        ];

        return await QueryApi(q => apiClient.OneCall.QueryAsync(q, excludeCategories), city, countryCode);
    }

    private async ValueTask<string> QueryApi<TResponse>(Func<string, Task<TResponse>> apiCall, string city, string? countryCode)
    {
        // Validate the input parameters.
        try
        {
            ValidateQuery(city, countryCode);
        }
        catch (ArgumentException ex)
        {
            // Log the validation error and return a user-friendly message.
            logger.LogWarning(ex, "Invalid input for GetCurrentWeather: {Message}", ex.Message);
            return ex.Message;
        }

        logger.LogInformation("Getting current weather for {City}, {CountryCode}", city, countryCode);
        var queryString = FormatQuery(city, countryCode);
        logger.LogDebug("Formatted query string: {QueryString}", queryString);

        try
        {
            // Attempt to query the OpenWeatherMap API.
            var weather = await apiCall(queryString);

            if (weather is null)
            {
                // If the API response indicates an invalid request, log a warning and return an error message.
                logger.LogError("Null response received for city {City}", city);
                // Return a user-friendly error message.
                return $"Error retrieving weather data for {city}.";
            }

            // Log the successful retrieval of weather data.
            logger.LogInformation("Successfully retrieved weather data for {City}", city);

            return FormatResponse(weather);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Log a warning if the city is not found.
            logger.LogWarning("City {City} not found: {Message}", city, ex.Message);
            // Return a user-friendly error message.
            return $"City '{city}' not found. Please check the name and try again.";
        }
        catch (ApiException ex)
        {
            // Log a warning if the request is invalid.
            logger.LogError("Invalid request for city {City}: {Message}", city, ex.Message);
            // Return a user-friendly error message.
            return $"Invalid request for city '{city}'. Response code: {ex.StatusCode} Error details: {ex.Message}";
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur during the API call.
            logger.LogError(ex, "Failed to retrieve weather data for {City}", city);
            // Return a user-friendly error message.
            return $"Error retrieving weather data for {city}. Error details: {ex.Message}";
        }
    }

    private static void ValidateQuery(string city, string? countryCode)
    {
        // Validate that the city name is not empty or whitespace.
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("Input error: City name cannot be empty or whitespace.", nameof(city));

        // Validate that the city name does not contain special characters.
        if (!CityNameRegex().IsMatch(city))
            throw new ArgumentException("Input error: City name can only contain letters, spaces, and hyphens.", nameof(city));

        // If a country code is provided, validate that it is not empty or whitespace.
        if (countryCode is not null && string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("Input error: Country code cannot be empty or whitespace.", nameof(countryCode));

        // If a country code is provided, validate that it contains only uppercase letters.
        if (countryCode is not null && !CountryCodeRegex().IsMatch(countryCode))
            throw new ArgumentException("Input error: Country code must be a 2-letter uppercase code (e.g., 'US', 'UK').", nameof(countryCode));
    }

    /// <summary>
    /// Formats the query string for the OpenWeatherMap API.
    /// </summary>
    private static string FormatQuery(string city, string? countryCode)
    {
        return countryCode is null ? city : $"{city},{countryCode}";
    }

    /// <summary>
    /// Generic method to format the response from the OpenWeatherMap API into a user-friendly string.
    /// </summary>
    /// <typeparam name="TResponse">The type of the API response.</typeparam>
    /// <param name="response">The API response to format.</param>
    /// <returns>A user-friendly string representation of the API response.</returns>
    /// <exception cref="NotSupportedException">Thrown when the response type is not supported.</exception>
    private static string FormatResponse<TResponse>(TResponse response) => response switch
    {
        CurrentWeather tResponse => FormatResponseImpl(tResponse),
        Forecast5Days tResponse => FormatResponseImpl(tResponse),
        OneCallCurrentWeather tResponse => FormatResponseImpl(tResponse),
        // Add other response types here as needed.
        _ => throw new NotSupportedException($"Response type {typeof(TResponse).Name} is not supported.")
    };

    /// <summary>
    /// Formats current weather response from the OpenWeatherMap API into a user-friendly string.
    /// </summary>
    private static string FormatResponseImpl(CurrentWeather response)
    {
        StringBuilder sb = new();
        sb.AppendLine($"Current weather in {response.CityName}:");

        sb.AppendLine($"Temperature: {response.Temperature.DegreesCelsius}°C");
        sb.AppendLine($"Humidity: {response.Humidity.Percent}%");
        sb.AppendLine($"Pressure: {response.Pressure.MillimetersOfMercury} mmHg");
        sb.AppendLine($"Wind Speed: {response.WindSpeed.MetersPerSecond} m/s");
        sb.AppendLine($"Wind Direction: {response.WindDirection.Degrees}°");
        sb.AppendLine($"Cloudiness: {response.Cloudiness.Percent}%");
        sb.AppendLine($"Visibility: {response.Visibility.Meters} m");

        if (response.RainLastThreeHours is not null)
            sb.AppendLine($"Rain: {response.RainLastThreeHours?.Millimeters ?? 0} mm (last 3 hours)");

        if (response.SnowLastThreeHours is not null)
            sb.AppendLine($"Snow: {response.SnowLastThreeHours?.Millimeters ?? 0} mm (last 3 hours)");

        sb.AppendLine($"Sunrise: {response.Sunrise:HH:mm:ss}");
        sb.AppendLine($"Sunset: {response.Sunset:HH:mm:ss}");

        sb.AppendLine($"Condition: {response.WeatherCondition}");
        sb.AppendLine($"Description: {response.WeatherDescription}");

        return sb.ToString();
    }

    /// <summary>
    /// Formats 5-Day forecast response from the OpenWeatherMap API into a user-friendly string.
    /// </summary>
    private static string FormatResponseImpl(Forecast5Days response)
    {
        StringBuilder sb = new();
        sb.AppendLine($"5-Day Weather Forecast for {response.CityName}:");

        foreach (var forecast in response.Forecast)
        {
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"Time: {forecast.ForecastTimeStamp:yyyy-MM-dd HH:mm}");

            sb.AppendLine($"Temperature: {forecast.Temperature.DegreesCelsius}°C");
            sb.AppendLine($"Humidity: {forecast.Humidity.Percent}%");
            sb.AppendLine($"Pressure: {forecast.Pressure.MillimetersOfMercury} mmHg");
            sb.AppendLine($"Wind Speed: {forecast.WindSpeed.MetersPerSecond} m/s");
            sb.AppendLine($"Wind Direction: {forecast.WindDirection.Degrees}°");
            sb.AppendLine($"Cloudiness: {forecast.Cloudiness.Percent}%");
            sb.AppendLine($"Visibility: {forecast.Visibility.Meters} m");

            if (forecast.RainLastThreeHours is not null)
                sb.AppendLine($"Rain: {forecast.RainLastThreeHours?.Millimeters ?? 0} mm (last 3 hours)");

            if (forecast.SnowLastThreeHours is not null)
                sb.AppendLine($"Snow: {forecast.SnowLastThreeHours?.Millimeters ?? 0} mm (last 3 hours)");

            sb.AppendLine($"Condition: {forecast.WeatherCondition}");
            sb.AppendLine($"Description: {forecast.WeatherDescription}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats One Call alerts response from the OpenWeatherMap API into a user-friendly string.
    /// </summary>
    private static string FormatResponseImpl(OneCallCurrentWeather response)
    {
        StringBuilder sb = new();
        sb.AppendLine($"Weather Alerts:");

        foreach (var alert in response.Alerts)
        {
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"Start: {alert.Start:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"End: {alert.End:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Event: {alert.Event}");
            sb.AppendLine($"Description: {alert.Description}");
            sb.AppendLine($"Sender: {alert.Sender}");
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"^[\p{L}\s\-]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex CityNameRegex();

    [GeneratedRegex(@"^[A-Z]{2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex CountryCodeRegex();
}

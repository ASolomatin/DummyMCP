using System.Net;

using DummyMCP.Tests.Extensions;
using DummyMCP.Tools;

using Microsoft.Extensions.Logging;

using Moq;

using OpenWeatherMap.NetClient;
using OpenWeatherMap.NetClient.Enums;
using OpenWeatherMap.NetClient.Models;

using Refit;

using SparkyTestHelpers.NonPublic;

using UnitsNet;

namespace DummyMCP.Tests;

public class WeatherToolsTests
{
    private readonly Mock<ILogger<WeatherTools>> _loggerMock;
    private readonly Mock<IOpenWeatherMap> _apiClientMock;
    private readonly WeatherTools _weatherTools;

    public WeatherToolsTests()
    {
        _loggerMock = new Mock<ILogger<WeatherTools>>();
        _apiClientMock = new Mock<IOpenWeatherMap>();
        _weatherTools = new WeatherTools(_loggerMock.Object, _apiClientMock.Object);
    }

    // Helper method to create a sample CurrentWeather response
    private static CurrentWeather CreateSampleCurrentWeather(string city)
    {
        var weatherSample = new CurrentWeather();

        weatherSample.NonPublic().Property(nameof(weatherSample.CityName)).Set(city);
        weatherSample.NonPublic().Property(nameof(weatherSample.Temperature)).Set(Temperature.FromDegreesCelsius(20.5));
        weatherSample.NonPublic().Property(nameof(weatherSample.Humidity)).Set(RelativeHumidity.FromPercent(65));
        weatherSample.NonPublic().Property(nameof(weatherSample.Pressure)).Set(Pressure.FromMillimetersOfMercury(760));
        weatherSample.NonPublic().Property(nameof(weatherSample.WindSpeed)).Set(Speed.FromMetersPerSecond(5.5));
        weatherSample.NonPublic().Property(nameof(weatherSample.WindDirection)).Set(Angle.FromDegrees(180));
        weatherSample.NonPublic().Property(nameof(weatherSample.Cloudiness)).Set(Ratio.FromPercent(50));
        weatherSample.NonPublic().Property(nameof(weatherSample.Visibility)).Set(Length.FromMeters(10000));
        weatherSample.NonPublic().Property(nameof(weatherSample.RainLastThreeHours)).Set(Length.FromMillimeters(2.5));
        weatherSample.NonPublic().Property(nameof(weatherSample.SnowLastThreeHours)).Set(null);
        weatherSample.NonPublic().Property(nameof(weatherSample.Sunrise)).Set(DateTimeOffset.Parse("2025-07-28T06:00:00"));
        weatherSample.NonPublic().Property(nameof(weatherSample.Sunset)).Set(DateTimeOffset.Parse("2025-07-28T20:00:00"));
        weatherSample.NonPublic().Property(nameof(weatherSample.WeatherCondition)).Set("Cloudy");
        weatherSample.NonPublic().Property(nameof(weatherSample.WeatherDescription)).Set("Scattered clouds");

        return weatherSample;
    }

    // Helper method to create a sample Forecast5Days response
    private static Forecast5Days CreateSampleForecast(string city)
    {
        var forecastSample = new Forecast5Days();

        forecastSample.NonPublic().Property(nameof(forecastSample.CityName)).Set(city);
        forecastSample.NonPublic().Property(nameof(forecastSample.Forecast)).Set(new[] { CreateSampleWeather() });

        return forecastSample;

        static Forecast5Days.Weather CreateSampleWeather()
        {
            var weatherSample = new Forecast5Days.Weather();

            weatherSample.NonPublic().Property(nameof(weatherSample.ForecastTimeStamp)).Set(DateTimeOffset.Parse("2025-07-28T12:00:00"));
            weatherSample.NonPublic().Property(nameof(weatherSample.Temperature)).Set(Temperature.FromDegreesCelsius(22.0));
            weatherSample.NonPublic().Property(nameof(weatherSample.Humidity)).Set(RelativeHumidity.FromPercent(70));
            weatherSample.NonPublic().Property(nameof(weatherSample.Pressure)).Set(Pressure.FromMillimetersOfMercury(758));
            weatherSample.NonPublic().Property(nameof(weatherSample.WindSpeed)).Set(Speed.FromMetersPerSecond(6.0));
            weatherSample.NonPublic().Property(nameof(weatherSample.WindDirection)).Set(Angle.FromDegrees(190));
            weatherSample.NonPublic().Property(nameof(weatherSample.Cloudiness)).Set(Ratio.FromPercent(60));
            weatherSample.NonPublic().Property(nameof(weatherSample.Visibility)).Set(Length.FromMeters(9000));
            weatherSample.NonPublic().Property(nameof(weatherSample.WeatherCondition)).Set("Rain");
            weatherSample.NonPublic().Property(nameof(weatherSample.WeatherDescription)).Set("Light rain");

            return weatherSample;
        }
    }

    // Helper method to create a sample OneCallCurrentWeather response
    private static OneCallCurrentWeather CreateSampleOneCallWeather()
    {
        var oneCallSample = new OneCallCurrentWeather();

        oneCallSample.NonPublic().Property(nameof(oneCallSample.Alerts)).Set(new[] { CreateSampleAlert() });

        return oneCallSample;

        static OneCallCurrentWeather.Alert CreateSampleAlert()
        {
            var alertSample = new OneCallCurrentWeather.Alert();

            alertSample.NonPublic().Property(nameof(alertSample.Start)).Set(DateTimeOffset.Parse("2025-07-28T10:00:00"));
            alertSample.NonPublic().Property(nameof(alertSample.End)).Set(DateTimeOffset.Parse("2025-07-28T14:00:00"));
            alertSample.NonPublic().Property(nameof(alertSample.Event)).Set("Storm Warning");
            alertSample.NonPublic().Property(nameof(alertSample.Description)).Set("Severe thunderstorm expected");
            alertSample.NonPublic().Property(nameof(alertSample.Sender)).Set("National Weather Service");
           
            return alertSample;
        }
    }

    private static ApiException CreateApiException(string message, HttpStatusCode statusCode)
    {
        return ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, (string?)null),
            HttpMethod.Get,
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent($"{{\"message\":\"{message}\"}}")
            },
            new RefitSettings()
        ).Result;
    }

    [Fact]
    public async Task GetCurrentWeather_ValidCity_ReturnsFormattedWeather()
    {
        // Arrange
        string city = "London";
        var weather = CreateSampleCurrentWeather(city);
        _apiClientMock.Setup(x => x.CurrentWeather.QueryAsync(city)).ReturnsAsync(weather);

        // Act
        var result = await _weatherTools.GetCurrentWeather(city);

        // Assert
        Assert.Contains($"Current weather in {city}", result);
        Assert.Contains("Temperature: 20.5°C", result);
        Assert.Contains("Humidity: 65%", result);
        Assert.Contains("Rain: 2.5 mm (last 3 hours)", result);
        _loggerMock.VerifyLog(LogLevel.Information, $"Getting current weather for {city}, ", Times.Once());
        _loggerMock.VerifyLog(LogLevel.Information, $"Successfully retrieved weather data for {city}", Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_WithCountryCode_FormatsQueryCorrectly()
    {
        // Arrange
        string city = "London";
        string countryCode = "UK";
        var weather = CreateSampleCurrentWeather(city);
        _apiClientMock.Setup(x => x.CurrentWeather.QueryAsync($"{city},{countryCode}")).ReturnsAsync(weather);

        // Act
        var result = await _weatherTools.GetCurrentWeather(city, countryCode);

        // Assert
        Assert.Contains($"Current weather in {city}", result);
        _apiClientMock.Verify(x => x.CurrentWeather.QueryAsync($"{city},{countryCode}"), Times.Once());
        _loggerMock.VerifyLog(LogLevel.Information, $"Getting current weather for {city}, {countryCode}", Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_InvalidCityName_ReturnsErrorMessage()
    {
        // Arrange
        string city = "London123!";
        var weather = CreateSampleCurrentWeather(city);
        _apiClientMock.Setup(x => x.CurrentWeather.QueryAsync($"{city}")).ReturnsAsync(weather);

        // Act
        var result = await _weatherTools.GetCurrentWeather(city);

        // Assert
        Assert.Equal("Input error: City name can only contain letters, spaces, and hyphens. (Parameter 'city')", result);
        _loggerMock.VerifyLog(LogLevel.Warning, "Invalid input for GetCurrentWeather: Input error: City name can only contain letters, spaces, and hyphens. (Parameter 'city')", Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_EmptyCity_ReturnsErrorMessage()
    {
        // Arrange
        string city = "";
        var weather = CreateSampleCurrentWeather(city);
        _apiClientMock.Setup(x => x.CurrentWeather.QueryAsync($"{city}")).ReturnsAsync(weather);

        // Act
        var result = await _weatherTools.GetCurrentWeather(city);

        // Assert
        Assert.Equal("Input error: City name cannot be empty or whitespace. (Parameter 'city')", result);
        _loggerMock.VerifyLog(LogLevel.Warning, "Invalid input for GetCurrentWeather: Input error: City name cannot be empty or whitespace. (Parameter 'city')", Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_CityNotFound_ReturnsNotFoundMessage()
    {
        // Arrange
        string city = "UnknownCity";
        _apiClientMock.Setup(x => x.CurrentWeather.QueryAsync(city))
            .ThrowsAsync(CreateApiException("City not found", HttpStatusCode.NotFound));

        // Act
        var result = await _weatherTools.GetCurrentWeather(city);

        // Assert
        Assert.Equal($"City '{city}' not found. Please check the name and try again.", result);
        _loggerMock.VerifyLog(LogLevel.Warning, $"City {city} not found: Response status code does not indicate success: 404 (Not Found).", Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_ApiError_ReturnsErrorMessage()
    {
        // Arrange
        string city = "London";

        var apiExceptionMock = new Mock<ApiException>("API error", HttpStatusCode.BadRequest);

        _apiClientMock.Setup(x => x.CurrentWeather.QueryAsync(city))
            .ThrowsAsync(CreateApiException("API error", HttpStatusCode.BadRequest));

        // Act
        var result = await _weatherTools.GetCurrentWeather(city);

        // Assert
        Assert.Contains($"Invalid request for city '{city}'. Response code: BadRequest", result);
        _loggerMock.VerifyLog(LogLevel.Error, $"Invalid request for city {city}: Response status code does not indicate success: 400 (Bad Request).", Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_UnexpectedException_ReturnsErrorMessage()
    {
        // Arrange
        string city = "London";
        _apiClientMock.Setup(x => x.CurrentWeather.QueryAsync(city))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _weatherTools.GetCurrentWeather(city);

        // Assert
        Assert.Contains($"Error retrieving weather data for {city}. Error details: Unexpected error", result);
        _loggerMock.VerifyLog(LogLevel.Error, $"Failed to retrieve weather data for {city}", Times.Once());
    }

    [Fact]
    public async Task GetWeatherForecast_ValidCity_ReturnsFormattedForecast()
    {
        // Arrange
        string city = "London";
        var forecast = CreateSampleForecast(city);
        _apiClientMock.Setup(x => x.Forecast5Days.QueryAsync(city, 2147483647)).ReturnsAsync(forecast);

        // Act
        var result = await _weatherTools.GetWeatherForecast(city);

        // Assert
        Assert.Contains($"5-Day Weather Forecast for {city}", result);
        Assert.Contains("Temperature: 22°C", result);
        Assert.Contains("Condition: Rain", result);
        _loggerMock.VerifyLog(LogLevel.Information, $"Getting current weather for {city}, ", Times.Once());
        _loggerMock.VerifyLog(LogLevel.Information, $"Successfully retrieved weather data for {city}", Times.Once());
    }

    [Fact]
    public async Task GetWeatherAlerts_ValidCity_ReturnsFormattedAlerts()
    {
        // Arrange
        string city = "London";
        var oneCallWeather = CreateSampleOneCallWeather();
        _apiClientMock.Setup(x => x.OneCall.QueryAsync(city, It.IsAny<IEnumerable<OneCallCategory>>()))
            .ReturnsAsync(oneCallWeather);

        // Act
        var result = await _weatherTools.GetWeatherAlerts(city);

        // Assert
        Assert.Contains("Weather Alerts:", result);
        Assert.Contains("Event: Storm Warning", result);
        Assert.Contains("Description: Severe thunderstorm expected", result);
        _loggerMock.VerifyLog(LogLevel.Information, $"Getting current weather for {city}, ", Times.Once());
        _loggerMock.VerifyLog(LogLevel.Information, $"Successfully retrieved weather data for {city}", Times.Once());
    }

    [Fact]
    public async Task GetWeatherAlerts_InvalidCountryCode_ReturnsErrorMessage()
    {
        // Arrange
        string city = "London";
        string countryCode = "U123";

        // Act
        var result = await _weatherTools.GetWeatherAlerts(city, countryCode);

        // Assert
        Assert.Equal("Input error: Country code must be a 2-letter uppercase code (e.g., 'US', 'UK'). (Parameter 'countryCode')", result);
        _loggerMock.VerifyLog(LogLevel.Warning, "Invalid input for GetCurrentWeather: Input error: Country code must be a 2-letter uppercase code (e.g., 'US', 'UK').", Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_NullResponse_ReturnsErrorMessage()
    {
        // Arrange
        string city = "London";
        _apiClientMock.Setup(x => x.CurrentWeather.QueryAsync(city)).ReturnsAsync((CurrentWeather?)null);

        // Act
        var result = await _weatherTools.GetCurrentWeather(city);

        // Assert
        Assert.Equal($"Error retrieving weather data for {city}.", result);
        _loggerMock.VerifyLog(LogLevel.Error, $"Null response received for city {city}", Times.Once());
    }
}

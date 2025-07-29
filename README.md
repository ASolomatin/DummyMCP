[test-icon]:            https://github.com/ASolomatin/DummyMCP/actions/workflows/tests.yml/badge.svg?branch=main
[test-url]:             https://github.com/ASolomatin/DummyMCP/actions/workflows/tests.yml

[packaging-icon]:       https://github.com/ASolomatin/DummyMCP/actions/workflows/publish.yml/badge.svg
[packaging-url]:        https://github.com/ASolomatin/DummyMCP/actions/workflows/publish.yml

[license-icon]:         https://img.shields.io/github/license/ASolomatin/DummyMCP
[license-url]:          https://github.com/ASolomatin/DummyMCP/blob/master/LICENSE

[nuget-icon]:           https://img.shields.io/nuget/v/DummyMCP.svg
[nuget-url]:            https://www.nuget.org/packages/DummyMCP

[nuget-downloads-icon]: https://img.shields.io/nuget/dt/DummyMCP.svg
[nuget-downloads-url]:  https://www.nuget.org/stats/packages/DummyMCP?groupby=Version

# Dummy MCP Server

[![NuGet][nuget-icon]][nuget-url]
[![NuGet downloads][nuget-downloads-icon]][nuget-downloads-url]
[![Tests][test-icon]][test-url]
[![Publish][packaging-icon]][packaging-url]
[![GitHub][license-icon]][license-url]

----------------------------------------

## Overview

The **MCP Weather Server Example** is a dummy Model Context Protocol (MCP) server designed to demonstrate MCP possibilities. It provides real-time weather information by integrating with the [OpenWeatherMap API](https://openweathermap.org/). The server exposes tools for retrieving current weather, weather forecasts, and weather alerts, making it compatible with AI assistants like Claude, GitHub Copilot, and other MCP-compatible clients.

## Features

- **Current Weather:** Retrieves current weather conditions for a specified city, including temperature, humidity, wind, and more.
- **Weather Forecast:** Provides a 5-day weather forecast for a specified city with detailed hourly data.
- **Weather Alerts:** Fetches active weather alerts for a specified city, including event details and durations.
- **Configurable API Key:** Supports configuration of the OpenWeatherMap API key via environment variables.
- **Cross-Platform:** Built on .NET 8.0, ensuring compatibility across multiple platforms.
- **Integration:** Seamlessly works with VS Code, Visual Studio, and other MCP-compatible environments.

## Installation

### From NuGet (Recommended)

The server is available as a NuGet package at [nuget.org/packages/DummyMCP.Tools](https://nuget.org/packages/DummyMCP.Tools).

#### Global Installation

Install the tool globally using:

```bash
dotnet tool install --global DummyMCP.Tools
```

After installation, you can run it directly with the command `DummyMCP.Tools`.

#### Local Installation

Install the tool locally within your project:

```bash
dotnet new tool-manifest
dotnet tool install --local DummyMCP.Tools
```

Run it using `dotnet DummyMCP.Tools` from the project directory.

### From Source

To build and run from the source code:

1. **Clone the repository:**

```bash
git clone https://github.com/ASolomatin/DummyMCP.git
cd DummyMCP
```

2. **Build and run:**

```bash
dotnet build
dotnet run
```

## Configuration

### VS Code Setup

Create a `.vscode/mcp.json` file in your workspace with the following content (assuming global installation):

```json
{
  "servers": {
    "DummyMcpServer": {
      "type": "stdio",
      "command": "DummyMCP.Tools",
      "args": [],
      "env": {
        "DUMMY_OWM__API_KEY": "Your OpenWeatherMap API Key here"
      }
    }
  }
}
```

### Visual Studio Setup

Create a `.mcp.json` file in your solution directory:

```json
{
  "servers": {
    "DummyMcpServer": {
      "type": "stdio",
      "command": "DummyMCP.Tools",
      "args": [],
      "env": {
        "DUMMY_OWM__API_KEY": "Your OpenWeatherMap API Key here"
      }
    }
  }
}
```

### Local Development Setup

For testing directly from the source code:

```json
{
  "servers": {
    "DummyMcpServer": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "."],
      "env": {
        "DUMMY_OWM__API_KEY": "Your OpenWeatherMap API Key here"
      }
    }
  }
}
```

**Note:** You must obtain an API key from [OpenWeatherMap](https://openweathermap.org/) to use this server. Sign up on their website and generate an API key.

## Environment Variables

| Variable           | Description                      |
|--------------------|----------------------------------|
| `DUMMY_OWM__API_KEY` | Your OpenWeatherMap API key      |

## Available Tools

The server provides the following tools:

| Tool Name            | Description                                             |
|----------------------|---------------------------------------------------------|
| `get_current_weather` | Gets current weather conditions for the specified city. Returns details like temperature, humidity, wind speed, and weather conditions. |
| `get_weather_forecast` | Gets a 5-day weather forecast for the specified city, including hourly data points. |
| `get_weather_alerts`  | Gets active weather alerts for the specified city, including event type, duration, and description. |

### Tool Parameters

Each tool accepts the following parameters:
- **`city`**: The name of the city (required). Must contain only letters, spaces, or hyphens.
- **`countryCode`**: Optional two-letter country code (e.g., "US", "UK") in uppercase.

### Error Handling

The tools include validation and error handling:
- Invalid city names or country codes return descriptive error messages.
- If a city is not found, the response indicates "City not found."
- API errors or connectivity issues are logged and returned as user-friendly messages.

## Additional Notes

- The server is built using .NET 8.0 and leverages the OpenWeatherMap .NET client library for robust API interactions.
- Responses are formatted as readable strings, making them suitable for both human users and AI clients.
- Logging is implemented to track requests and errors, aiding in debugging during development.
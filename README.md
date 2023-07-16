# Windows Event Log Plugin for ChatGPT

This repository contains the demo code for a plugin that allows ChatGPT to interact with the Windows Event Log.

## Overview

The Windows Event Log plugin provides an API that can retrieve specific events from the Windows Event Log using the XPath query language. The plugin is built with .NET 7.0 and uses the minimal API model.

## Features

- Retrieve specific events from the Windows Event Log using XPath queries.
- Supports all major log names: Application, Security, Setup, System, and ForwardedEvents.
- Use it to solve problems and get information about your Windows system status

## Getting Started

### Prerequisites

- .NET 7.0 SDK
- A Windows system with access to the Event Log

### Installation

1. Clone the repository:
 ```
git clone https://github.com/yourusername/WindowsEventLogChatGPTPlugIn.git
 ```
2. Navigate to the project directory:
 ```
 cd WindowsEventLogChatGPTPlugIn
 ```
 3. Run the application:
  ```
dotnet run
 ```


The API will be available at `http://localhost:5000`.

## Usage
Once the plugin is running, you can use it with ChatGPT by following these steps:

1. Open the ChatGPT UI.
2. Navigate to the plugin section and click on "Add Plugin".
3. Enter the URL of the plugin's url, which should be `http://localhost:5000/`.
4. Click "Add". The plugin should now be available for use with ChatGPT.

You can then interact with the plugin by asking ChatGPT to retrieve events from the Windows Event Log. For example, you could ask "What are the latest events in the System log?" and ChatGPT will use the plugin to retrieve this information.

## Directory Structure

The project has the following directory structure:

- `WindowsEventLogChatGPTPlugIn.sln`: The solution file for the project.
- `appsettings.Development.json` and `appsettings.json`: Configuration settings for the application.
- `Program.cs`: The main entry point for the application.
- `WindowsEventLogChatGPTPlugIn.csproj`: The project file for the application.
- `wwwroot`: The default directory for static files. It contains the `logo.png` file and the `.well-known` directory.
- `.well-known`: This directory contains the `ai-plugin.json` file, which is the manifest file for the plugin.


## License

This project is licensed under the terms of the MIT license. See the [LICENSE](LICENSE) file for details.
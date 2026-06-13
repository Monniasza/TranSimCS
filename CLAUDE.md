# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 🚀 Development Commands

- **Build Project**: `dotnet build`
- **Run Application**: `dotnet run --project TranSimCS\TranSimCS.csproj`
- **Custom Build Script**: `./TranSimCS/build.ps1` (Requires NSIS at `C:\Program Files (x86)\NSIS\makensis.exe`)

## 🏗️ Architecture Overview

TranSimCS is a performance-oriented city builder built with C# and MonoGame. The project follows a modular architecture designed for high-performance simulation:

### Core Engine Modules
- **`Roads`**: Handles the complex road network system, including different road types (Highway, Avenue, Street, Alley), traffic management (lights, signs), and dynamic traffic simulation.
- **`SceneGraph`**: Manages the hierarchy of all objects in the world, handling transformations and scene organization.
- **`Render`**: The primary rendering subsystem using MonoGame's DesktopGL framework.
- **`Geometry` & `Maths`**: Provides foundational geometric calculations, spatial partitioning (using NetOctree), and custom math utilities.

### Gameplay & Tools
- **`Tools`**: Contains the "Move It" tool system for precise object manipulation, including multi-select, alignment, rotation, scaling, and snapping capabilities.
- **`Collections`**: A set of custom collection types (e.g., `ObservableList`) used throughout the engine for specialized behavior like UI updates or specific iteration patterns.
- **`Save2`**: The persistent storage system for city saves and world data.

### Configuration & Data
- **Road Definitions**: Managed via `road.txt`.
- **Game Config**: General settings are stored in `config.ini`.
- **Content Assets**: Managed through the MonoGame content pipeline (`Content/bin/DesktopGL`).

## 💡 Development Notes
- **LanguageExt**: The project uses functional programming patterns where appropriate (e.g., for data transformations).
- **Logging**: Uses NLog; ensure logs are checked in the `logs/` directory when debugging runtime issues.
- **Spatial Partitioning**: NetOctree is used for efficient spatial queries of objects and traffic.
- **Coordinate System**: Pay attention to the distinction between world coordinates and local object transformations within the SceneGraph.

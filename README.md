# TranSimCS

**The ultimate performance semi-realistic city builder!**

[![Language](https://img.shields.io/badge/language-C%23-blue.svg)](https://github.com/Monniasza/TranSimCS)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Issues](https://img.shields.io/github/issues/Monniasza/TranSimCS.svg)](https://github.com/Monniasza/TranSimCS/issues)

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Getting Started](#getting-started)
- [Road System Documentation](#road-system-documentation)
- [Move It Tool](#move-it-tool)
- [Controls Reference](#controls-reference)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [Support](#support)
- [License](#license)

## 🌟 Overview

TranSimCS is a high-performance, semi-realistic city building simulation game built with C#. The game focuses on creating efficient transportation networks and managing urban development with realistic traffic simulation and road management systems.

## ✨ Features

- **Advanced Road System**: Comprehensive road network management with realistic traffic simulation
- **Move It Tool**: Powerful object manipulation and positioning system
- **Performance Optimized**: Built for smooth gameplay even with large cities
- **Semi-Realistic Simulation**: Balance between realism and playability
- **Extensible Architecture**: Modular design for easy customization and expansion

## 🚀 Installation

### Prerequisites

- .NET Framework 4.7.2 or higher
- Visual Studio 2019 or later (for development)
- Windows 10/11 (recommended)

### Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/Monniasza/TranSimCS.git
   cd TranSimCS
   ```

2. Open the solution file:
   ```bash
   TranSimCS.sln
   ```

3. Build the project:
   - Press `Ctrl+Shift+B` in Visual Studio
   - Or use the command line: `dotnet build`

4. Run the application:
   - Press `F5` in Visual Studio
   - Or run the executable from the build output directory

## 🎮 Getting Started

### First Launch

1. Start TranSimCS
2. Create a new city or load an existing save
3. Use the tutorial (see `The Tutorial.odt`) to learn basic mechanics
4. Begin building your transportation network

### Basic Workflow

1. **Plan Your Layout**: Start with a basic road grid
2. **Build Infrastructure**: Add roads, intersections, and traffic management
3. **Use Move It Tool**: Fine-tune positioning and alignment
4. **Monitor Traffic**: Observe and optimize traffic flow
5. **Expand**: Grow your city systematically

## 🛣️ Road System Documentation

### Road Types

The road system supports multiple road types with different characteristics:

#### Primary Roads
- **Highway**: High-capacity, high-speed arterial roads
- **Avenue**: Medium-capacity urban arterials
- **Street**: Standard residential and commercial roads
- **Alley**: Low-capacity service roads

#### Specialized Roads
- **One-Way Streets**: Directional traffic control
- **Pedestrian Paths**: Walking-only zones
- **Service Roads**: Utility and emergency access

### Road Management

#### Creating Roads
1. Select the Road Tool from the toolbar
2. Choose road type from the submenu
3. Click and drag to create road segments
4. Roads automatically connect at intersections

#### Road Properties
- **Speed Limit**: Configurable per road segment
- **Lane Count**: 1-6 lanes depending on road type
- **Traffic Direction**: Bidirectional or one-way
- **Surface Type**: Affects vehicle speed and maintenance

#### Traffic Management
- **Traffic Lights**: Automatic or manual intersection control
- **Stop Signs**: Simple intersection management
- **Yield Signs**: Priority-based traffic flow
- **Speed Zones**: Variable speed limit areas

### Road Configuration File

Roads are managed through the `road.txt` configuration file:

```
# Road Configuration Format
# Type,Speed,Lanes,Cost,Maintenance
Highway,80,4,1000,50
Avenue,50,3,600,30
Street,30,2,300,15
Alley,15,1,100,5
```

#### Configuration Parameters
- **Type**: Road classification name
- **Speed**: Maximum speed limit (km/h)
- **Lanes**: Number of traffic lanes
- **Cost**: Construction cost per unit
- **Maintenance**: Ongoing maintenance cost

### Advanced Road Features

#### Dynamic Traffic Simulation
- Real-time traffic density calculation
- Congestion detection and routing
- Emergency vehicle priority
- Public transport integration

#### Road Upgrade System
- Upgrade existing roads to higher capacity
- Preserve existing traffic patterns
- Gradual construction simulation
- Cost-benefit analysis tools

## 🔧 Move It Tool

The Move It tool is a powerful system for precise object manipulation and positioning within your city.

### Accessing Move It

1. **Toolbar Access**: Click the Move It icon in the main toolbar
2. **Keyboard Shortcut**: Press `M` to activate
3. **Context Menu**: Right-click on objects for Move It options

### Move It Modes

#### Selection Mode
- **Single Select**: Click on individual objects
- **Multi-Select**: Hold `Ctrl` and click multiple objects
- **Area Select**: Click and drag to select all objects in an area
- **Filter Select**: Use filters to select specific object types

#### Movement Operations

##### Basic Movement
- **Drag**: Click and drag objects to new positions
- **Precision Mode**: Hold `Shift` for fine-grained movement
- **Grid Snap**: Hold `Ctrl` to snap to grid points
- **Axis Lock**: Hold `Alt` + arrow key to lock movement to specific axis

##### Advanced Positioning
- **Align Tools**: Align multiple objects to edges or centers
- **Distribute**: Evenly space selected objects
- **Mirror**: Create mirrored copies across axes
- **Array**: Create patterns and repetitive layouts

#### Rotation and Scaling
- **Rotate**: Use rotation handles or `R` key + mouse movement
- **Scale**: Use scale handles or `S` key + mouse movement
- **Uniform Scale**: Hold `Shift` while scaling to maintain proportions
- **Pivot Point**: Set custom rotation/scale center points

### Move It Features

#### Object Types Supported
- **Roads and Intersections**: Precise road network adjustments
- **Buildings**: Residential, commercial, and industrial structures
- **Props and Decorations**: Trees, signs, and aesthetic elements
- **Utilities**: Power lines, water pipes, and service infrastructure
- **Transportation**: Bus stops, train stations, and transit infrastructure

#### Precision Tools
- **Coordinate Display**: Real-time X, Y, Z position feedback
- **Measurement Tools**: Distance and angle measurements
- **Snap Options**: Snap to objects, grid, or custom points
- **Undo/Redo**: Full operation history with unlimited undo

#### Advanced Features
- **Copy/Paste**: Duplicate objects with positioning
- **Save Selections**: Store frequently used object groups
- **Batch Operations**: Apply changes to multiple objects simultaneously
- **Import/Export**: Share object arrangements between cities

### Move It Workflow

#### Basic Object Movement
1. Activate Move It tool (`M` key)
2. Select target object(s)
3. Drag to new position or use precision controls
4. Confirm placement with `Enter` or click elsewhere

#### Precision Alignment
1. Select multiple objects to align
2. Choose alignment type (left, center, right, top, middle, bottom)
3. Objects automatically align to the reference point
4. Use distribute tools for even spacing

#### Complex Arrangements
1. Use area selection for large object groups
2. Apply filters to select specific object types
3. Use array tools for repetitive patterns
4. Save arrangements as templates for reuse

## 🎮 Controls Reference

### General Controls

#### Camera Controls
- **WASD**: Move camera (pan)
- **Mouse Wheel**: Zoom in/out
- **Middle Mouse Button**: Pan camera (hold and drag)
- **Right Mouse Button**: Rotate camera (hold and drag)
- **Home**: Reset camera to default position
- **End**: Focus camera on selected object

#### Selection Controls
- **Left Click**: Select object/tool
- **Ctrl + Left Click**: Multi-select objects
- **Shift + Left Click**: Add to selection
- **Alt + Left Click**: Remove from selection
- **Ctrl + A**: Select all visible objects
- **Ctrl + D**: Deselect all objects
- **Delete**: Delete selected objects

#### General Interface
- **Escape**: Cancel current operation/close menus
- **Tab**: Cycle through interface panels
- **F1**: Open help/tutorial
- **F11**: Toggle fullscreen mode
- **Ctrl + S**: Quick save
- **Ctrl + L**: Quick load
- **Ctrl + Z**: Undo last action
- **Ctrl + Y**: Redo last undone action

### Tool-Specific Controls

#### Road Building
- **R**: Activate Road tool
- **Shift + R**: Road upgrade mode
- **Ctrl + Click**: Force road connection
- **Alt + Click**: Create intersection
- **1-6**: Select lane count (while road tool active)
- **Q/E**: Cycle through road types

#### Move It Tool
- **M**: Activate Move It tool
- **Shift + Drag**: Precision movement mode
- **Ctrl + Drag**: Grid snap mode
- **Alt + Arrow Keys**: Axis-locked movement
- **R**: Rotation mode
- **S**: Scale mode
- **Ctrl + C**: Copy selected objects
- **Ctrl + V**: Paste copied objects
- **Ctrl + G**: Group selected objects
- **Ctrl + U**: Ungroup selected objects

#### Building Placement
- **B**: Activate Building tool
- **Shift + Mouse Wheel**: Rotate building before placement
- **Ctrl + Click**: Force building placement (ignore zoning)
- **Alt + Click**: Demolish building
- **Space**: Confirm building placement

### Advanced Controls

#### Traffic Management
- **T**: Traffic overlay toggle
- **Shift + T**: Traffic light management mode
- **Ctrl + T**: Traffic flow analysis
- **Alt + T**: Public transport overlay

#### Utility Management
- **U**: Utility overlay toggle
- **Shift + U**: Power grid view
- **Ctrl + U**: Water system view
- **Alt + U**: Sewage system view

#### Information Overlays
- **I**: Toggle information panels
- **Shift + I**: Population density overlay
- **Ctrl + I**: Economic activity overlay
- **Alt + I**: Environmental impact overlay

### Customizable Hotkeys

Most controls can be customized through the Settings menu:

1. Open Settings (`Ctrl + ,`)
2. Navigate to "Controls" tab
3. Click on any action to reassign hotkey
4. Press new key combination
5. Click "Apply" to save changes

#### Recommended Custom Hotkeys
- **F**: Follow selected vehicle
- **G**: Toggle grid display
- **H**: Hide/show UI elements
- **J**: Jump to city center
- **K**: Toggle construction guides
- **L**: Toggle lot boundaries
- **N**: Toggle name labels
- **P**: Pause/unpause simulation

## ⚙️ Configuration

### Game Settings

#### Performance Settings
- **Simulation Speed**: Adjust game speed multiplier
- **Graphics Quality**: Low/Medium/High/Ultra presets
- **View Distance**: Rendering distance for objects
- **Traffic Density**: Maximum vehicles in simulation
- **Auto-Save Interval**: Automatic save frequency

#### Gameplay Settings
- **Difficulty Level**: Easy/Normal/Hard economic settings
- **Disaster Frequency**: Natural disaster occurrence rate
- **Growth Rate**: City development speed
- **Budget Starting Amount**: Initial city funds
- **Loan Availability**: Enable/disable municipal loans

### File Locations

#### Configuration Files
- `config.ini`: Main game configuration
- `road.txt`: Road type definitions
- `controls.cfg`: Custom control mappings
- `graphics.cfg`: Graphics and performance settings

#### Save Files
- `saves/`: City save files directory
- `templates/`: Building and layout templates
- `exports/`: Exported city data and screenshots

#### Logs and Debug
- `logs/`: Game logs and error reports
- `debug/`: Debug output and performance metrics
- `crash_dumps/`: Crash report files

## 🤝 Contributing

We welcome contributions to TranSimCS! Here's how you can help:

### Development Setup

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes and test thoroughly
4. Commit your changes: `git commit -m 'Add amazing feature'`
5. Push to the branch: `git push origin feature/amazing-feature`
6. Open a Pull Request

### Contribution Guidelines

- Follow C# coding standards and conventions
- Include unit tests for new features
- Update documentation for any API changes
- Ensure all tests pass before submitting PR
- Write clear, descriptive commit messages

### Areas for Contribution

- **Performance Optimization**: Improve simulation efficiency
- **New Features**: Add gameplay mechanics and tools
- **Bug Fixes**: Resolve issues and improve stability
- **Documentation**: Enhance guides and API documentation
- **Localization**: Translate interface to other languages

## 📞 Support

### Getting Help

- **Issues**: Report bugs and request features on [GitHub Issues](https://github.com/Monniasza/TranSimCS/issues)
- **Discussions**: Join community discussions and ask questions
- **Wiki**: Check the project wiki for detailed guides
- **Tutorial**: See `The Tutorial.odt` for comprehensive gameplay guide

### Common Issues

#### Performance Problems
- Reduce graphics quality in settings
- Lower traffic density limits
- Disable unnecessary overlays
- Close other applications while playing

#### Road Building Issues
- Check road.txt configuration for errors
- Ensure proper road type definitions
- Verify intersection connectivity
- Use Move It tool for fine adjustments

#### Save/Load Problems
- Check file permissions in save directory
- Verify save file integrity
- Clear temporary files if corruption occurs
- Use backup saves when available

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Contributors and community members
- Open source libraries and frameworks used
- Beta testers and feedback providers
- City building simulation community

---

**Happy City Building!** 🏙️

For the latest updates and releases, visit the [TranSimCS GitHub repository](https://github.com/Monniasza/TranSimCS).

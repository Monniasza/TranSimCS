# JSON Converter Reference Guide

## Overview
This document provides a reference for all JSON converters used in the TranSimCS save/load system. The project supports both **Newtonsoft.Json** (legacy) and **System.Text.Json** (new) serialization libraries.

## Converter Locations

### Newtonsoft.Json Converters
Located in: `TranSimCS/Roads/`
- `Vector3Converter.cs`
- `ColorConverter.cs`
- `ObjPosConverter.cs`
- `LaneSpecConverter.cs`
- `LaneConverter.cs`
- `LaneEndConverter.cs`
- `RoadNodeConverter.cs`
- `RoadNodeEndConverter.cs`
- `LaneStripConverter.cs`
- `RoadStripConverter.cs`

### System.Text.Json Converters
Located in: `TranSimCS/Save2/`
- `Vector3Converter.cs`
- `ColorConverter.cs`
- `ObjPosConverter.cs`
- `LaneSpecConverter.cs`
- `LaneConverter.cs`
- `LaneEndConverter.cs`
- `RoadNodeConverter.cs`
- `RoadNodeEndConverter.cs`
- `LaneStripConverter.cs`
- `RoadStripConverter.cs`
- `TSWorldConverter.cs`

## Data Type Serialization Formats

### Vector3
**JSON Format:**
```json
{
  "x": 10.5,
  "y": 20.0,
  "z": 30.75
}
```

### Color
**JSON Format (Hex String):**
```json
"#FF0000FF"
```
- Format: `#RRGGBBAA`
- Also supports 6-character format: `#RRGGBB` (assumes alpha = 255)

**Alternative Object Format (Newtonsoft.Json only):**
```json
{
  "r": 255,
  "g": 0,
  "b": 0,
  "a": 255
}
```

### ObjPos
**JSON Format:**
```json
{
  "position": {
    "x": 10.5,
    "y": 20.0,
    "z": 30.75
  },
  "azimuth": 45.0,
  "inclination": 0.0,
  "tilt": 0.0
}
```

### LaneSpec
**JSON Format:**
```json
{
  "color": "#808080FF",
  "vehicleTypes": 1,
  "flags": 1,
  "width": 3.5,
  "speedLimit": 50.0
}
```

### Lane
**JSON Format:**
```json
{
  "left": 0.0,
  "right": 3.5,
  "spec": {
    "color": "#808080FF",
    "vehicleTypes": 1,
    "flags": 1,
    "width": 3.5,
    "speedLimit": 50.0
  }
}
```

### RoadNode
**JSON Format:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "pos": {
    "position": {"x": 10.5, "y": 20.0, "z": 30.75},
    "azimuth": 45.0,
    "inclination": 0.0,
    "tilt": 0.0
  },
  "lanes": [
    {
      "left": 0.0,
      "right": 3.5,
      "spec": {...}
    }
  ],
  "name": "Node 1"
}
```

### LaneEnd
**JSON Format:**
```json
[
  ["550e8400-e29b-41d4-a716-446655440000", 0],
  2
]
```
- First element: RoadNodeEnd (array with node GUID and end index)
- Second element: Lane index

### RoadNodeEnd
**JSON Format:**
```json
["550e8400-e29b-41d4-a716-446655440000", 0]
```
- First element: Node GUID
- Second element: NodeEnd enum value (0 or 1)

### LaneStrip
**JSON Format:**
```json
{
  "start": [["node-guid", 0], 2],
  "end": [["node-guid", 1], 1],
  "spec": {...}
}
```

### RoadStrip
**JSON Format:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "start": ["start-node-guid", 0],
  "end": ["end-node-guid", 1],
  "lanes": [
    {
      "start": [["start-node-guid", 0], 0],
      "end": [["end-node-guid", 1], 0],
      "spec": {...}
    }
  ]
}
```

### TSWorld
**JSON Format:**
```json
{
  "nodes": [
    {...}
  ],
  "segments": [
    {...}
  ]
}
```

## Converter Dependencies

The converters have the following dependency hierarchy:

```
TSWorld
├── RoadNode
│   ├── ObjPos
│   │   └── Vector3
│   └── Lane
│       └── LaneSpec
│           └── Color
└── RoadStrip
    ├── RoadNodeEnd
    └── LaneStrip
        ├── LaneEnd
        │   └── RoadNodeEnd
        └── LaneSpec
            └── Color
```

## Usage

### Newtonsoft.Json (Old Method)
```csharp
// Save
TSWorld world = new TSWorld();
world.SaveToFile("world.json");

// Load
TSWorld world = TSWorld.Load("world.json");
```

### System.Text.Json (New Method)
```csharp
// Save
TSWorld world = new TSWorld();
world.SaveToFileJson("world.json");

// Load
TSWorld world = TSWorld.LoadJson("world.json");
```

## Adding New Converters

If you need to add a new data type that requires custom serialization:

1. **For Newtonsoft.Json:**
   - Create a new converter class in `TranSimCS/Roads/` that inherits from `JsonConverter<T>`
   - Implement `ReadJson()` and `WriteJson()` methods
   - Register the converter in `TSWorld.CreateSerializer()`

2. **For System.Text.Json:**
   - Create a new converter class in `TranSimCS/Save2/` that inherits from `JsonConverter<T>`
   - Implement `Read()` and `Write()` methods
   - Register the converter in `TSWorld.CreateJsonOptions()`

3. **Important:** Ensure both converters produce the same JSON format for compatibility!

## Troubleshooting

### Common Issues

1. **JsonSerializationException: "No converter found for type X"**
   - Solution: Add a converter for type X and register it in the serializer setup

2. **Circular reference errors**
   - Solution: Use reference-based serialization (GUID references) instead of direct object references

3. **Missing properties during deserialization**
   - Solution: Check that property names match exactly (case-sensitive for System.Text.Json by default)

4. **Different JSON output between old and new methods**
   - Solution: Ensure both converters use the same property names and formats

## Performance Comparison

| Operation | Newtonsoft.Json | System.Text.Json | Improvement |
|-----------|----------------|------------------|-------------|
| Serialization | Baseline | 2-3x faster | +200-300% |
| Deserialization | Baseline | 1.5-2x faster | +50-100% |
| Memory Usage | Baseline | 30-40% less | -30-40% |

## See Also
- `README.md` - Main documentation for the save/load system
- `CHANGELOG_SAVE_SYSTEM.md` - Change history
- `FIX_SERIALIZATION_EXCEPTION.md` - Fix documentation for missing converters
- `Examples.cs` - Usage examples

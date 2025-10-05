# Fix for Newtonsoft.Json Serialization Exception

## Problem
The application was throwing a `Newtonsoft.Json.JsonSerializationException` when trying to save or load the game world. This was caused by missing JSON converters for several data types used in the road network serialization.

## Root Cause
The `CreateSerializer()` method in `TSWorld.Serializer.cs` was missing converters for the following types:
- `Vector3` (Microsoft.Xna.Framework)
- `Color` (Microsoft.Xna.Framework)
- `ObjPos` (custom type)
- `LaneSpec` (custom type)

When the `RoadNodeConverter` tried to deserialize a `RoadNode`, it would attempt to deserialize the `pos` property (of type `ObjPos`), which in turn needed to deserialize a `Vector3` for the position. Without these converters registered, Newtonsoft.Json couldn't properly serialize/deserialize these types, resulting in the exception.

## Solution
Created four new Newtonsoft.Json converters in the `TranSimCS/Roads/` namespace:

1. **Vector3Converter.cs** - Handles serialization of `Microsoft.Xna.Framework.Vector3`
   - Serializes as JSON object with `x`, `y`, `z` properties
   - Supports both uppercase and lowercase property names during deserialization

2. **ColorConverter.cs** - Handles serialization of `Microsoft.Xna.Framework.Color`
   - Serializes as hex string format (`#RRGGBBAA`) for compactness
   - Deserializes from both hex string and object format (`{R, G, B, A}`)
   - Supports both 6-character (RGB) and 8-character (RGBA) hex formats

3. **ObjPosConverter.cs** - Handles serialization of `ObjPos`
   - Serializes position, azimuth, inclination, and tilt properties
   - Uses `Vector3Converter` for the position property

4. **LaneSpecConverter.cs** - Handles serialization of `LaneSpec`
   - Serializes color, vehicle types, flags, width, and speed limit
   - Uses `ColorConverter` for the color property
   - Stores enums as integers

## Changes Made

### New Files Created
- `TranSimCS/Roads/Vector3Converter.cs`
- `TranSimCS/Roads/ColorConverter.cs`
- `TranSimCS/Roads/ObjPosConverter.cs`
- `TranSimCS/Roads/LaneSpecConverter.cs`

### Modified Files
- `TranSimCS/Worlds/TSWorld.Serializer.cs`
  - Updated `CreateSerializer()` method to register the new converters
  - Converters are added in the correct order (base types first, then dependent types)

## Converter Registration Order
The converters are registered in this specific order to ensure dependencies are resolved correctly:

```csharp
settings.Converters.Add(new Roads.Vector3Converter());      // Base type
settings.Converters.Add(new Roads.ColorConverter());        // Base type
settings.Converters.Add(new Roads.ObjPosConverter());       // Depends on Vector3
settings.Converters.Add(new Roads.LaneSpecConverter());     // Depends on Color
settings.Converters.Add(new Roads.LaneConverter());         // Depends on LaneSpec
settings.Converters.Add(new Roads.RoadNodeConverter(this)); // Depends on ObjPos, Lane
// ... other converters
```

## Testing
After applying this fix:
1. The application should start without throwing serialization exceptions
2. Save files should be created successfully
3. Load operations should work correctly
4. Both old (Newtonsoft.Json) and new (System.Text.Json) serialization methods should work

## Compatibility
- This fix maintains backward compatibility with existing save files
- The JSON format remains consistent with the System.Text.Json implementation
- Both serialization systems now have feature parity

## Notes
- The converters follow the same pattern as the existing converters in the `Roads` namespace
- They use the `JsonProcessor` helper class for consistent JSON reading
- Error handling is included for missing properties and invalid data
- The hex color format matches the System.Text.Json implementation for consistency

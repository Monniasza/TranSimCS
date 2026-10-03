# Refactor Settings storage to SettingsData
## Understanding
Centralize runtime settings in one `SettingsData` value, expose individual settings as ref-returning members for direct use, and add validation in debug builds as requested. Update UI call sites to use refs while preserving settings serialization and defaults.
## Assumptions
- Existing public scalar setting names remain available as writable ref-returning properties.
- `SettingsData` remains the serialized shape and keeps its existing defaults and JSON converter.
- Direct ref writes cannot be intercepted; debug validation will run on the update path that changes settings from UI and on bulk settings application.
## Approach
Inspect the settings UI helper overloads and current validation patterns, then replace the individual `Property<T>` backing instances with a single `SettingsData` store. Make the scalar accessors return refs into that store and add a debug-only validation method. Adapt settings UI to write through ref overloads and perform validation at the end of the UI interaction; validate bulk-loaded settings before storing them. Build and run relevant tests if available.
## Key Files
- `TranSimCS/Setting/Settings.cs` - canonical settings storage, ref members, and validation.
- `TranSimCS/SilkNet/GameWindow.UI.cs` - settings UI currently consumes `Property<T>` wrappers.
- `TranSimCS/SilkNet/DearUI.cs` - existing ref-based ImGui helpers.
## Risks & Open Questions
- Ref-returning members permit direct callers to bypass debug validation until the validation loop is invoked; this is inherent to the selected API.
- Existing event-based property change notifications for settings will be removed unless another usage requires them.
## Steps
1. Inspect settings UI helper signatures and existing debug validation conventions.
2. Replace the settings storage and APIs with SettingsData-backed refs and debug validation.
3. Update settings UI to use ref-based members and invoke validation.
4. Build and run relevant tests.
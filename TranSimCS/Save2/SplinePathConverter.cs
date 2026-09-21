using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Roads;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Paths;

namespace TranSimCS.Save2 {
    /// <summary>
    /// Serializes a <see cref="SplinePath"/> as a reference to the lane strip that owns it, together
    /// with the path's GUID.
    /// <para>
    /// The path GUID is what makes a path survive a save and load cycle. On load the GUID is looked up
    /// in the world's <see cref="PathSystem"/>; if a path with that GUID already exists it is reused,
    /// and only if it does not exist is a new path created. This means a path referenced from several
    /// places in a save file resolves to a single instance after loading, rather than to several
    /// duplicates.
    /// </para>
    /// <para>
    /// The lane strip is written as a reference rather than as a full copy, so that the strip itself is
    /// only serialized once, by the road segment that contains it.
    /// </para>
    /// </summary>
    public class SplinePathConverter : JsonConverter<SplinePath> {
        private readonly TSWorld _world;

        /// <summary>
        /// Creates a converter bound to the world being serialized.
        /// </summary>
        /// <param name="world">The world the paths belong to.</param>
        public SplinePathConverter(TSWorld world) {
            _world = world;
        }

        /// <summary>
        /// Reads a path reference from JSON.
        /// <para>
        /// The path is resolved by GUID first. If a path with the saved GUID is already registered, that
        /// instance is returned and the strip reference is only used to validate the match. Otherwise the
        /// strip is resolved and its path is created with the saved GUID, so that the same path is reused
        /// after loading.
        /// </para>
        /// </summary>
        /// <param name="reader">The JSON reader positioned at the path value.</param>
        /// <param name="typeToConvert">The type being converted.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The resolved path, or <see langword="null"/> when the JSON value is null.</returns>
        public override SplinePath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) return null;

            Guid? guid = null;
            LaneStrip? strip = null;

            var stripRefConverter = new StripRefConverter(_world);

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref Utf8JsonReader reader0, string propertyName) => {
                switch (propertyName.ToLower()) {
                    case "guid":
                        reader0.Read();
                        if (reader0.TokenType != JsonTokenType.String)
                            JsonProcessor.FailTokenTypes(ref reader0, JsonTokenType.String);
                        guid = Guid.Parse(reader0.GetString()!);
                        break;
                    case "strip":
                        strip = stripRefConverter.Read(ref reader0, typeof(LaneStrip), options);
                        break;
                }
            });

            if (guid == null) JsonProcessor.Fail(reader, "Missing guid property");

            //Reuse the existing path when one with this GUID is already registered. This is what makes
            //the same path usable again after loading, instead of a duplicate being created.
            var existing = _world.Paths.FindPath(guid.Value);
            if (existing != null) return existing;

            if (strip == null) JsonProcessor.Fail(reader, "Missing strip property");

            //No path with this GUID exists yet, so create the strip's path and give it the saved GUID.
            //The GUID has to be passed at construction time: Obj.Guid is set-once, so assigning it after
            //the path has been created would be silently ignored and the saved GUID would be lost.
            var path = strip.GetOrCreatePath(guid.Value);
            if (path == null) JsonProcessor.Fail(reader, "The referenced lane strip does not belong to a world");
            return path;
        }

        /// <summary>
        /// Writes a path reference to JSON, consisting of the path GUID and a reference to the lane
        /// strip that owns it.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The path to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, SplinePath value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteString("guid", value.Guid.ToString());

            //Write the owning lane strip as a reference, so the strip itself is not duplicated.
            var strip = (value.Claimant as LaneStripPathClaim)?.Strip;
            if (strip != null) {
                writer.WritePropertyName("strip");
                var stripRefConverter = new StripRefConverter(_world);
                stripRefConverter.Write(writer, strip, options);
            }

            writer.WriteEndObject();
        }
    }
}

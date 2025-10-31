using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Save2;

namespace TranSimCS.Worlds.Stack {
    internal class ObjectStackConverter<TObj, TStack> : JsonConverter<TStack>
    where TStack : ObjectStack<TObj, TStack>
    where TObj : Obj {
        private readonly Func<TStack> constructor;
        private readonly TSWorld world;

        public ObjectStackConverter(TSWorld world, Func<TStack> stack) {
            this.constructor = stack;
            this.world = world;
        }

        public override TStack? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var stack = constructor();
            stack.ReadFromJson(ref reader, options);
            return stack;
        }

        public override void Write(Utf8JsonWriter writer, TStack value, JsonSerializerOptions options) {
            value.SaveToJson(writer, options);
        }
    }
}

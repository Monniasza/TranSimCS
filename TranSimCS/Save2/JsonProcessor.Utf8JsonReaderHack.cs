using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TranSimCS.Save2 {
    public delegate T JsonReaderFunction<T> (ref Utf8JsonReader reader);

    public partial class JsonProcessor {
        private static JsonReaderFunction<long>? getLineNumber = null;
        private static JsonReaderFunction<long>? getColumnNumber = null;

        public static long GetLineNumber(this Utf8JsonReader reader) {
            if (getLineNumber == null) throw new NotInitializedException("GetLineNumber not initialized");
            return getLineNumber(ref reader);
        }
        public static long GetColumnNumber(this Utf8JsonReader reader) {
            if (getColumnNumber == null) throw new NotInitializedException("GetColumnNumber not initialized");
            return getColumnNumber(ref reader);
        }

        public static void Init() {
            if (getLineNumber == null) getLineNumber = CreateDelegate<long>("_lineNumber");
            if (getColumnNumber == null) getColumnNumber = CreateDelegate<long>("_bytePositionInLine");
        }

        private unsafe static JsonReaderFunction<T> CreateDelegate<T>(string name) {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var type = typeof(Utf8JsonReader);
            var field = type.GetField(name, flags);
            var fieldOffset = GetFieldOffsetCrossRuntime(field);

            if (field == null) throw new MissingFieldException($"The field {name} is not a part of Utf8JsonReader.");
            if (!field.FieldType.IsSubclassOrEqual(typeof(T))) throw new InvalidCastException($"The field must be a subtype {field.FieldType} of the desired return type {typeof(T)}");
            return (ref reader) => {
                T result;
                fixed (Utf8JsonReader* ptr = &reader) {
                    var newPtr = ptr + fieldOffset;
                    var castedPtr = (T*)ptr;
                    result = *castedPtr;
                }
                return result;
            };
        }

        public unsafe static Func<TSource, TField> CreateDelegate<TSource, TField>(int offset) {
            var type = typeof(TSource);
            var fieldOffset = offset;

            return (element) => {
                var reader = element;
                var ptr = &element;
                TField result;
                var newPtr = ptr + fieldOffset;
                var castedPtr = (TField*)newPtr;
                result = *castedPtr;
                return result;
            };
        }
        public unsafe static Setter<TSource, TField> CreateSetter<TSource, TField>(int offset) {
            var type = typeof(TSource);
            var fieldOffset = offset;

            return (ref element, newValue) => {
                var tmp = element;
                var ptr = &tmp;
                var newPtr = ptr + fieldOffset;
                var castedPtr = (TField*)newPtr;
                *castedPtr = newValue;
                element = tmp;
            };
        }

        public static bool IsSubclassOrEqual(this Type a, Type b) {
            return a == b || a.IsSubclassOf(b);
        }

        public static unsafe bool IsMonoRuntime() {
            return sizeof(TypedReference) == IntPtr.Size * 3;
        }

        public static unsafe int GetFieldOffset(FieldInfo field) {
            var rhv = (IntPtr*)field.FieldHandle.Value; // pointer to MonoClassField
            rhv += 3; // skip three pointers.
            return *(int*)rhv - IntPtr.Size * 2; //load the value of a pointer (4 bytes, int32), then subtracting 16 bytes from it.
        }

        public static unsafe int GetFieldOffsetRealDotnet(FieldInfo field) {
            var rhv = (byte*)field.FieldHandle.Value;
            // 0x3FFFFF masks 22 bits. 
            // https://github.com/dotnet/runtime/blob/62d33ee48d57feba67b261b55db666bdc202b1c1/src/coreclr/vm/field.h#L36
            return *(int*)(rhv + IntPtr.Size + sizeof(int)) & 0x3FFFFF;
        }

        public static unsafe int GetFieldOffsetCrossRuntime(FieldInfo fieldInfo) {
            var offset = 0;
            if (IsMonoRuntime())
                offset = GetFieldOffset(fieldInfo);
            else
                offset = GetFieldOffsetRealDotnet(fieldInfo);

            if (fieldInfo.DeclaringType.IsClass)
                offset += IntPtr.Size * 2;

            return offset;
        }
    }
}

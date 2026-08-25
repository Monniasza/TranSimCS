using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace TranSimCS.Terrain {
    public static class TerrainDataBlobs {
        public const string VertexBlobID =
            "TranSimCS.Include.heightmapVertex.blob";

        public const string IndexBlobID =
            "TranSimCS.Include.heightmapIndex.blob";
        internal const int vertexBytes = 133128;
        internal const int indexBytes = 196608;

        internal static byte[] VertexBlobData;
        internal static byte[] IndexBlobData;

        internal static void Init() {
            if (VertexBlobData == null) using (var stream = OpenEmbeddedResource(VertexBlobID)) {
                VertexBlobData = new byte[vertexBytes];
                stream.ReadExactly(VertexBlobData);
            }
            if (IndexBlobData == null) using (var stream = OpenEmbeddedResource(IndexBlobID)) {
                IndexBlobData = new byte[indexBytes];
                stream.ReadExactly(IndexBlobData);
            }
        }

        public static Stream OpenEmbeddedResource(string name){
            var assembly = typeof(TerrainBuffer).Assembly;

            return assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException(
                    $"Embedded resource '{name}' was not found.");
        }
        public static string ReadEmbeddedResource(string name) {
            using var stream = OpenEmbeddedResource(name);
            var reader = new StreamReader(stream);
            var result = reader.ReadToEnd();
            return result;
        }
    }
}

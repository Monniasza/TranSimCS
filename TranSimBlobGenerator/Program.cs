// A single-file program to generate vertex and index blobs for TranSim heightmap renderer

// See https://aka.ms/new-console-template for more information
using System.Diagnostics;

Console.WriteLine("Hello, World!");

//Generate position data
var vertexDataArray = new float[129 * 129 * 2];
int index = 0;
for (int y = 0; y < 129; y++) {
    for (int x = 0; x < 129; x++) {
        vertexDataArray[index++] = x / 128.0f;
        vertexDataArray[index++] = y / 128.0f;
    }
}
Debug.Assert(index == vertexDataArray.Length, "Not all vertices filled");

//Generate index data
var indexDataArray = new ushort[128 * 128 * 6];
index = 0;
for (int x = 0; x < 128; x++) {
    for (int y = 0; y < 128; y++) {
        ushort i0 = (ushort)(129 * y + x);
        ushort i1 = (ushort)(129 * y + x + 1);
        ushort i2 = (ushort)(129 * y + x + 130);
        ushort i3 = (ushort)(129 * y + x + 129);
        indexDataArray[index++] = i0;
        indexDataArray[index++] = i1;
        indexDataArray[index++] = i2;
        indexDataArray[index++] = i1;
        indexDataArray[index++] = i2;
        indexDataArray[index++] = i3;
    }
}
Debug.Assert(index == indexDataArray.Length, "Not all indices filled");

//Write data to files
string vertexBlobName = "heightmapVertex.blob";
string indexBlobName = "heightmapIndex.blob";

using(FileStream vertexStream = File.Create(vertexBlobName))
    using(BinaryWriter vertexWriter = new BinaryWriter(vertexStream))
        foreach(var value in vertexDataArray)
            vertexWriter.Write(value);

using (FileStream indexStream = File.Create(indexBlobName))
    using (BinaryWriter indexWriter = new BinaryWriter(indexStream))
        foreach (var value in indexDataArray)
            indexWriter.Write(value);
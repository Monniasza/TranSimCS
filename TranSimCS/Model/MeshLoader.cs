using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ObjLoader.Loader.Data;
using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Loaders;

namespace TranSimCS.Model {
    public class MeshLoader {
        private readonly ObjLoaderFactory olf;
        private readonly IObjLoader loader;
        private readonly Dictionary<string, LoadResult> results;
        private readonly IMaterialStreamProvider objLoader;
        public MeshLoader(Func<string, Stream> objLoader, Func<string, Stream> mtlLoader) : this(new MaterialStreamAdapter(objLoader), new MaterialStreamAdapter(mtlLoader)) { }
        public MeshLoader(IMaterialStreamProvider objLoader, IMaterialStreamProvider mtlLoader) {
            olf = new ObjLoaderFactory();
            loader = olf.Create(mtlLoader);
            results = [];
            this.objLoader = objLoader;
        }

        public LoadResult Load(string path) {
            if(results.TryGetValue(path, out LoadResult result)) { return result; }
            LoadResult lr = null; 
            using (var objStream = objLoader.Open(path)) {
                lr = loader.Load(objStream);
            }
            results[path] = lr;
            return lr;
        }
        

        public class MaterialStreamAdapter(Func<string, Stream> stream) : IMaterialStreamProvider {
            public Stream Open(string materialFilePath) => stream(materialFilePath);
        }
    }
}

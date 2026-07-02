using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;

namespace TranSimCS.Tools {
    public struct LaneSpecToolsFlag<T> where T: struct, Enum {
        public string Title;
        public string Texture;
        public string? SecondaryTexture;
        public T Flag;
        public LaneSpecToolsFlag(string title, string texture, string? secondaryTexture, T flag) {
            this.Title = title;
            this.Texture = texture;
            this.SecondaryTexture = secondaryTexture;
            this.Flag = flag;
        }
    }
}

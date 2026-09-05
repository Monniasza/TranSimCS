using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;

namespace TranSimCS.SilkNet.RoadConstruction {
    public struct LaneSpecToolsFlag<T> where T: struct, Enum {
        public string Title;
        public string CheckTexture;
        public string? UncheckTexture;
        public T Flag;
        public LaneSpecToolsFlag(string title, string checkTexture, string? uncheckTexture, T flag) {
            Title = title;
            CheckTexture = checkTexture;
            UncheckTexture = uncheckTexture;
            Flag = flag;
        }
    }
}

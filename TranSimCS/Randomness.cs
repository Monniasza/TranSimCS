using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS {
    public static class Randomness {
        public static int NextIntFullRange(this Random random) {
            var bytes = new byte[4];
            random.NextBytes(bytes);
            return bytes[3] + 256 * bytes[2] + 65536 * bytes[1] + 16777216 * bytes[0];
        }
        public static Color NextColor(this Random random) {
            var bytes = new byte[4];
            random.NextBytes(bytes);
            return new Color(bytes[0], bytes[1], bytes[2], bytes[3]);
        }
    }
}

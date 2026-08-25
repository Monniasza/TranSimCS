using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TranSimCS {
    public sealed class FPS: IDisposable {
        public int FrameRate;
        public int Count;
        private readonly Timer timer;
        public FPS() {
            timer = new Timer(ResetCount, null, 0, 1000);
        }
        private void ResetCount(object? state) {
            FrameRate = Count;
            Count = 0;
        }
        public void Dispose() {
            timer.Dispose();
        }
    }
}

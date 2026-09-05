using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Mode;

namespace TranSimCS.SilkNet {
    // Modes for the OpenGL window
    public partial class SilkNetTest {
        public ImmutableArray<IMode> AvailableModes;
        private IMode _mode;
        public IMode Mode {
            get => _mode;
            set {
                ArgumentNullException.ThrowIfNull(value, nameof(Mode));
                _mode.OnClose();
                _mode = value;
                _mode.OnOpen();
            }
        }
    }
}

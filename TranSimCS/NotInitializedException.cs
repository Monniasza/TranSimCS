using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS {
    public class NotInitializedException: Exception {
        public NotInitializedException(string? message = null, Exception? inner = null): base(message, inner) { }
    }
}

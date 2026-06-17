using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Property;

namespace TranSimCS.Roads {
    public interface IRoadFinish {
        public Property<RoadFinish> FinishProperty { get; }
    }
}

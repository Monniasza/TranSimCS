using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Worlds.ECS {
    /// <summary>
    /// Assigns a GUID for the entity, allowing persistent references to it across reloads
    /// </summary>
    public class GUIDComponent{
        public Guid Guid { get; init; } = Guid.NewGuid();
    }
}

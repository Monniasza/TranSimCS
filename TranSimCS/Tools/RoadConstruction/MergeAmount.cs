using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Tools.RoadConstruction {
    ///Represents a change in the number of lanes of a starting lane in a safe manner.
    public struct MergeAmount {
        /// Marks lanes that are to be merged.
        public static MergeAmount Merge => new(MERGE);
        public static MergeAmount StraightOn => new(0);

        ///The special raw value determining a merge
        public const byte MERGE = 255;

        ///The internal value of this MergeAmount object
        public byte AmountRaw;

        ///Actual change in lane count
        public int Amount => (AmountRaw == MERGE) ? -1 : AmountRaw;

        ///Is this MergeAmount a merge?
        public bool IsMerge => AmountRaw == MERGE;

        ///Is this MergeAmount a straight-on continuation?
        public bool IsStraight => AmountRaw == 0;

        ///Is this MergeAmount an exit?
        public bool IsExpand => AmountRaw is not 0 or MERGE;

        ///Creates a MergeAmount with a specified internal value. <see cref="MERGE"> for a merge.
        public MergeAmount(byte amountRaw) {
            AmountRaw = amountRaw;
        }

        /// <summary>
        /// Clamps a supplied integer to between -1 and 254 (one merge and 254 exits) as a <see cref="MergeAmount"/>
        /// </summary>
        /// <param name="amount">amount to be clamped</param>
        /// <returns>a clamped <see cref="MergeAmount"/></returns>
        public static MergeAmount Clamp(int amount) {
            var clamped = int.Clamp(amount, -1, 254);
            return new((byte)clamped);
        }

        ///Returns a MergeAmount with one fewer lane. If this is a merge, it returns the same value.
        public MergeAmount Merged => IsMerge ? Merge : new((byte)(AmountRaw - 1));

        ///Returns a MergeAmount with one more lane. If it is already +254 lanes, it returns the same value.
        public MergeAmount Expanded => (AmountRaw == 254) ? this : new((byte)(AmountRaw + 1));
    }
}

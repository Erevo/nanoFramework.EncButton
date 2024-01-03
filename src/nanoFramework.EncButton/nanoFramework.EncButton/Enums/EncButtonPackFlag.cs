using System;

namespace nanoFramework.EncButton.Enums
{
    [Flags]
    public enum EncButtonPackFlag
    {
        CLKS_R = 1 << 0,
        PRS_R = 1 << 1,
        HLD_R = 1 << 2,
        STP_R = 1 << 3,
        REL_R = 1 << 4,

        PRS = 1 << 5,
        HLD = 1 << 6,
        STP = 1 << 7,
        REL = 1 << 8,

        BUSY = 1 << 9,
        DEB = 1 << 10,
        TOUT = 1 << 11,
        INV = 1 << 12,
        BOTH = 1 << 13,
        BISR = 1 << 14,

        EHLD = 1 << 15,
    }
}
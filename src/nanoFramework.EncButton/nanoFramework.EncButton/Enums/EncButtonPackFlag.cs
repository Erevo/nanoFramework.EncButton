using System;

namespace nanoFramework.EncButton.Enums
{
    [Flags]
    public enum EncButtonPackFlag
    {
        ClicksRelease = 1 << 0,
        PressRelease = 1 << 1,
        HoldRelease = 1 << 2,
        StepRelease = 1 << 3,
        REL_R = 1 << 4,

        Press = 1 << 5,
        Hold = 1 << 6,
        Step = 1 << 7,
        Release = 1 << 8,

        Busy = 1 << 9,
        DEB = 1 << 10,
        Timeout = 1 << 11,
        INV = 1 << 12,
        Both = 1 << 13,
        BISR = 1 << 14,

        EHLD = 1 << 15,
    }
}
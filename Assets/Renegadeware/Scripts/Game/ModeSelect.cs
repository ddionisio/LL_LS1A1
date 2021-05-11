
namespace Renegadeware.LL_LS1A1 {
    public enum ModeSelect {
        None = -1,

        Environment,
        Edit,
        Play,

        Retry,

        NextLevel
    }

    [System.Flags]
    public enum ModeSelectFlags {
        None = 0x0,
        Environment = 0x1,
        Edit = 0x2,
        Play = 0x4
    }
}
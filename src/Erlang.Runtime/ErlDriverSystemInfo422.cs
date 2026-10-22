namespace Erlang;

// Values are the actual owner's configured capabilities, never guessed ERTS
// host statistics. The private child receives the bound parent configuration.
internal sealed record ErlDriverSystemInfo422(
    string ErtsVersion,
    string OtpRelease,
    int AsyncThreads,
    int SchedulerThreads,
    int NifMajorVersion,
    int NifMinorVersion,
    bool DirtySchedulerSupport
);

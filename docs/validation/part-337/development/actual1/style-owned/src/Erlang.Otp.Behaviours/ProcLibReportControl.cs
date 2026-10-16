namespace Erlang.Otp;

internal sealed record ProcLibReportControl(
    char Directive,
    Term Argument,
    Term Depth,
    bool Unicode,
    int? Width,
    int? Column
);

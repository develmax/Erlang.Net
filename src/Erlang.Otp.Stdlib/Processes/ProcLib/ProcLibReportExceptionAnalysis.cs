namespace Erlang.Otp;

internal sealed record ProcLibReportExceptionAnalysis(
    Term Reason,
    IReadOnlyList<TupleTerm> ReasonFrames,
    IReadOnlyList<TupleTerm> StackFrames
);

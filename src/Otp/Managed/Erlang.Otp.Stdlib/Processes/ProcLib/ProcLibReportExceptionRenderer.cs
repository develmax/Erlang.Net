namespace Erlang.Otp;

internal delegate ValueTask<Term> ProcLibReportExceptionRenderer(
    Term exceptionClass,
    Term reason,
    Term stackTrace,
    Term encoding,
    Term depth,
    Term charsLimit
);

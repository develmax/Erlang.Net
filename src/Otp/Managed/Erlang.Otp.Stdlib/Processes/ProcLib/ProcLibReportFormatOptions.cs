namespace Erlang.Otp;

internal sealed record ProcLibReportFormatOptions(
    Term Encoding,
    Term Depth,
    Term SingleLine,
    Term CharsLimit
)
{
    public bool IsSingleLine => SingleLine.Equals(ProcLibReportFormatAtoms.True)
        ? true
        : SingleLine.Equals(ProcLibReportFormatAtoms.False)
            ? false
            : throw new ErlangException(ErlangErrorReasons.FunctionClause);

    public static ProcLibReportFormatOptions Defaults => new(
        ProcLibReportFormatAtoms.Utf8,
        ProcLibReportFormatAtoms.Unlimited,
        ProcLibReportFormatAtoms.False,
        ProcLibReportFormatAtoms.Unlimited
    );

    public static ProcLibReportFormatOptions Merge(MapTerm extra)
    {
        var defaults = Defaults;

        return new(
            extra.TryGet(ProcLibReportFormatAtoms.Encoding, out var encoding) ? encoding! : defaults.Encoding,
            extra.TryGet(ProcLibReportFormatAtoms.Depth, out var depth) ? depth! : defaults.Depth,
            extra.TryGet(ProcLibReportFormatAtoms.SingleLine, out var single) ? single! : defaults.SingleLine,
            extra.TryGet(ProcLibReportFormatAtoms.CharsLimit, out var limit) ? limit! : defaults.CharsLimit
        );
    }
}

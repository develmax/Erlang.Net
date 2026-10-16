using System.Numerics;

namespace Erlang.Otp;

internal sealed class ProcLibReportPrimitives(
    IProcLibReportTermPrinter printer,
    ProcLibReportExceptionRenderer renderException
) : IProcLibReportPrimitives
{
    private readonly ProcLibReportFormatControls controls = new(printer);

    public Term Format(string control, IReadOnlyList<Term> arguments, Term charsLimit)
        => controls.Format(control, arguments, charsLimit);

    public Term WriteAtom(Term atom, Term encoding)
        => ProcLibReportAtomWriter.Write(atom, encoding);

    public BigInteger Measure(Term chars, Term encoding)
        => ProcLibReportCharacters.Measure(chars, encoding);

    public ValueTask<Term> FormatException(
        Term exceptionClass,
        Term reason,
        Term stackTrace,
        Term encoding,
        Term depth,
        Term charsLimit
    ) => renderException(
        exceptionClass,
        reason,
        stackTrace,
        encoding,
        depth,
        charsLimit
    );
}

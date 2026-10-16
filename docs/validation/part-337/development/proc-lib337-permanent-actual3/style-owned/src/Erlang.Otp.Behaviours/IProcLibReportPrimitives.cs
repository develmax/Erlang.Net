using System.Numerics;

namespace Erlang.Otp;

internal interface IProcLibReportPrimitives
{
    Term Format(string control, IReadOnlyList<Term> arguments, Term charsLimit);

    Term WriteAtom(Term atom, Term encoding);

    BigInteger Measure(Term chars, Term encoding);

    ValueTask<Term> FormatException(
        Term exceptionClass,
        Term reason,
        Term stackTrace,
        Term encoding,
        Term depth,
        Term charsLimit
    );
}

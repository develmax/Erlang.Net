namespace Erlang.Otp;

internal interface IProcLibReportTermPrinter
{
    Term Render(
        Term term,
        Term depth,
        Term encoding,
        Term charsLimit,
        int width,
        int column
    );

    Term Write(Term term, Term encoding);
}

using System.Globalization;
namespace Erlang;

internal sealed partial class TermDisplayWriter
{
    private Func<byte[], int>? printfSink791;
    private int printfCount791;

    internal int WritePrintfTo791(Term term, long precision, Func<byte[], int> sink)
    {
        printfSink791 = sink;
        limited = true;
        remaining = precision;
        Write(term);

        return printfCount791;
    }

    private int EmitPrintf791(byte[] bytes)
    {
        int copied = printfSink791!(bytes);
        if (copied < 0) throw new PrintfTermSinkFailure791(copied);
        printfCount791 = checked(printfCount791 + copied);

        return copied;
    }

    // PIN PRINT_SWORD/fmt_fld counts the numeric field, independently of
    // the sink result. PRINT_STRING/PRINT_CHAR count only returned bytes.
    private void EmitPrintfNumber791(string text)
    {
        int copied = EmitPrintf791(System.Text.Encoding.UTF8.GetBytes(text));
        printfCount791 = checked(printfCount791 + text.Length - copied);
    }
}

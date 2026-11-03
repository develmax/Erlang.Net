using System.Text;
namespace Erlang.NifNativeBackend;

internal static class NifPrintf791StreamingChecks
{
    internal static void Check(ProcessContext context)
    {
        Term term = Term.Tuple(Term.A(NifPrintf791HostNames.Ok), Term.I(42));
        var full = Capture(
            context,
            term,
            NifPrintf791HostNames.DefaultTermBudget,
            int.MaxValue
        );
        Require(full == (NifPrintf791HostNames.TupleText, NifPrintf791HostNames.TupleText.Length));
        Require(Capture(
            context,
            term,
            NifPrintf791HostNames.DefaultTermBudget,
            0
        ) == (string.Empty, 2));
        Require(Capture(
            context,
            term,
            NifPrintf791HostNames.DefaultTermBudget,
            3
        ) == (NifPrintf791StreamingNames.Prefix, 5));
        Require(Capture(
            context,
            term,
            2,
            int.MaxValue
        ) == (NifPrintf791StreamingNames.PrecisionTwo, NifPrintf791StreamingNames.PrecisionTwo.Length));
        Require(Capture(
            context,
            new Integer(System.Numerics.BigInteger.One << 80),
            NifPrintf791HostNames.DefaultTermBudget,
            0
        ) == (string.Empty, 0));
    }

    private static (string, int) Capture(
        ProcessContext context,
        Term term,
        long precision,
        int capacity
    )
    {
        var collected = new List<byte>();
        int rendered = new TermDisplayWriter(context.Runtime).WritePrintfTo791(
            term,
            precision,
            bytes =>
        {
            int copied = Math.Min(bytes.Length, capacity - collected.Count);
            collected.AddRange(bytes.AsSpan(0, copied).ToArray());

            return copied;
        }
        );

        return (Encoding.UTF8.GetString(collected.ToArray()), rendered);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NifPrintf791HostNames.Failure);
    }
}

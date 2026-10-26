namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal BitString ReadDebugUnaligned421(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        BitString source;
        int offset;
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this))
                throw new ArgumentException(DebugUnaligned421Names.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (arguments.Count != 2)
                throw new ArgumentException(DebugUnaligned421Names.Arguments);
            if (arguments[0] is not BitString bits || arguments[1] is not Integer integer
                || integer.Value <= 0 || integer.Value >= 8)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            source = bits;
            offset = (int)integer.Value;
        }

        // Copy outside the runtime gate; real actor cancellation is checked throughout.
        return source.DebugUnalignedCopy421(offset, context.Cancellation);
    }
}

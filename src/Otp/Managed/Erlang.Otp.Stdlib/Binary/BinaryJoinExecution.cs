namespace Erlang;

internal static class BinaryJoinExecution
{
    private static readonly object continuation = new();

    internal static async ValueTask<Term> Run(ProcessContext context, Term value, Term separator)
    {
        if (separator is not BitString { IsBinary: true } delimiter)
            throw BinaryWrapperTrace.Public(context, BinaryModuleNames.Join, [value, separator]);
        if (value is Nil)
            return BitString.FromOwnedBytes([]);
        if (value is Cons { Head: BitString { IsBinary: true } single, Tail: Nil })
            return single;
        try
        {
            if (value is not Cons { Head: BitString { IsBinary: true } first } initial)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            using var owner = context.EnterSourceLocation(BinaryWrapperTrace.Location(BinaryJoinSource.PublicCallLine));
            var chunks = new List<Term> { first };
            int total = first.ByteLength;
            int capacity = Grow(BinaryJoinSource.InitialWritableBytes, total);
            using (context.EnterGeneratedFunction(
                BinaryModuleNames.Module,
                BinaryModuleNames.Join,
                BinaryJoinSource.RecursiveArity,
                BinaryWrapperTrace.Location(BinaryJoinSource.RecursiveAppendLine),
                continuation
            ))
            {
                Term list = initial.Tail;
                while (list is Cons cell)
                {
                    await context.ReduceAsync();
                    if (cell.Head is not BitString { IsBinary: true } current)
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                    long requested = (long)total + delimiter.ByteLength + current.ByteLength;
                    if (requested > BinaryWrapperSource.MaximumBinaryBytes)
                        throw new ErlangException(ErlangErrorReasons.SystemLimit);
                    total = (int)requested;
                    capacity = Grow(capacity, total);
                    chunks.Add(delimiter);
                    chunks.Add(current);
                    list = cell.Tail;
                }
                if (list is not Nil)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                var complete = (BitString)await BinaryWrapperOutput.Flatten(context, Cons.From(chunks));

                return context.AppendGuestBinary(BitString.InitializeWritable(capacity), complete);
            }
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
        {
            throw BinaryWrapperTrace.Public(context, BinaryModuleNames.Join, [value, separator]);
        }
    }

    private static int Grow(int capacity, int used)
    {
        if (used <= capacity)
            return capacity;
        double next = used <= BinaryBackingParameters.LargeGrowthThreshold
            ? (double)used * 2
            : used * BinaryBackingParameters.LargeGrowthFactor;
        if (next > Array.MaxLength)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);

        return (int)next;
    }
}

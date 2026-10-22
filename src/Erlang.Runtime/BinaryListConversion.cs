namespace Erlang;

internal static class BinaryListConversion
{
    internal static async ValueTask<Term> ToList(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        try
        {
            var binary = BinaryPrimitiveOperations.Binary(arguments[0]);
            if (arguments.Count == 2)
                binary = (BitString)BinaryPrimitiveOperations.Part(binary, arguments[1]);
            else if (arguments.Count == 3)
                binary = (BitString)ErlangIoData.Part(binary, arguments[1], arguments[2]);
            Term result = Nil.Value;
            for (int index = binary.ByteLength - 1; index >= 0; index--)
            {
                if (index % BinaryPrimitiveParameters.ListCheckpointItems == 0)
                    await context.ReduceAsync();
                result = new Cons(Term.I(binary.ByteSpan[index]), result);
            }

            return result;
        }
        catch (ErlangException error)
        {
            var caller = context.CaptureExecutionChain();
            if (caller is { SourceWrapper: true, Module: BinaryModuleNames.Module, Function: BinaryModuleNames.BinToList })
                caller = caller.Parent;
            throw RuntimeExceptionCapture.Call(
                context,
                error,
                caller,
                BinaryModuleNames.Module,
                BinaryModuleNames.BinToList,
                arguments,
                true,
                BinaryWrapperTrace.ErrorMetadata(ErrorLine(arguments))
            );
        }
    }

    private static int ErrorLine(IReadOnlyList<Term> arguments)
    {
        if (arguments.Count == 1)
            return BinaryWrapperSource.ErrorInfo;
        if (arguments.Count == 2)
            return arguments[1] is TupleTerm { Items.Count: 2 } ? BinaryWrapperSource.ErrorInfo : BinaryWrapperSource.BadArgumentInfo;
        if (arguments[0] is not BitString { IsBinary: true } || arguments[1] is not Integer || arguments[2] is not Integer count || count.Value.IsZero)
            return BinaryWrapperSource.BadArgumentInfo;

        return BinaryWrapperSource.ErrorInfo;
    }

    internal static async ValueTask<Term> FromList(ProcessContext context, Term value)
    {
        if (value is not Nil and not Cons)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (value is Cons { Head: BitString { IsBinary: true } singleton, Tail: Nil })
            return singleton;

        return await BinaryWrapperOutput.Flatten(context, value);
    }
}

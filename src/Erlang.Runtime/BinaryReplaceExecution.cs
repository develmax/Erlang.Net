namespace Erlang;

internal static class BinaryReplaceExecution
{
    internal static async ValueTask<Term> Run(
        ProcessContext context,
        Term subject,
        Term pattern,
        Term replacement,
        Term options,
        bool three
    )
    {
        try
        {
            if (replacement is not BitString { IsBinary: true } && replacement is not FunctionTerm { Arity: 1 })
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            BinaryReplaceOptions settings = await BinaryReplaceOptions.Read(context, options);
            Term matches = await context.Runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                settings.Global ? BinaryModuleNames.Matches : BinaryModuleNames.Match,
                [subject, pattern, settings.Scope]
            );
            if (!settings.Global)
                matches = matches is Atom { Name: BinaryWrapperNames.NoMatch } ? Nil.Value : Term.List(matches);
            BinaryReplacePlan plan = await BinaryReplacePlan.Create(context, replacement, settings.Insert);
            var chunks = new List<Term>();
            int offset = 0;
            bool pending = false;
            using var sourceLocation = context.EnterSourceLocation(BinaryWrapperTrace.Location(
                three ? BinaryWrapperSource.ReplaceThreeCall : BinaryWrapperSource.ReplaceBody));
            using var inner = three ? context.EnterSourceWrapper(BinaryModuleNames.Module, BinaryModuleNames.Replace, 4) : null;
            using var innerLocation = three ? context.EnterSourceLocation(BinaryWrapperTrace.Location(BinaryWrapperSource.ReplaceBody)) : null;
            foreach (Term found in Cons.Items(matches))
            {
                await context.ReduceAsync();
                var pair = (TupleTerm)found;
                int start = (int)((Integer)pair.Items[0]).Value;
                int length = (int)((Integer)pair.Items[1]).Value;
                chunks.Add(await Part(
                    context,
                    subject,
                    offset,
                    start - offset
                ));
                Term matched = await Part(
                    context,
                    subject,
                    start,
                    length
                );
                chunks.Add(await plan.Apply(context, matched, pending));
                offset = start + length;
                pending = true;
            }
            var source = (BitString)subject;
            chunks.Add(await Part(
                context,
                subject,
                offset,
                source.ByteLength - offset
            ));

            return await BinaryWrapperOutput.Flatten(context, Cons.From(chunks));
        }
        catch (ErlangException error)
        {
            bool badOption = !three && error.ExceptionClass == ErlangExceptionClasses.Throw
                && error.Reason is Atom { Name: BinaryWrapperNames.BadOption };

            throw BinaryWrapperTrace.Public(
                context,
                BinaryModuleNames.Replace,
                three ? [subject, pattern, replacement] : [subject, pattern, replacement, options],
                badOption,
                three ? BinaryWrapperSource.ErrorInfo : BinaryWrapperSource.BadArgumentInfo
            );
        }
    }

    private static ValueTask<Term> Part(
        ProcessContext context,
        Term value,
        int start,
        int length
    )
        => context.Runtime.Modules.Call(
            context,
            BinaryModuleNames.Module,
            BinaryModuleNames.Part,
            [value, Term.Tuple(Term.I(start), Term.I(length))]
        );
}

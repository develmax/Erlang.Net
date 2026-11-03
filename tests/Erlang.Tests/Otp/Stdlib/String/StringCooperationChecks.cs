namespace Erlang.Tests;

internal static class StringCooperationChecks
{
    internal static async Task Run(bool binary, bool reverse)
    {
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
            StringBackendChecks.Selection(true),
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        Term input = binary
            ? new BitString(Enumerable.Repeat(StringCooperationNames.Character, StringCooperationNames.Characters).ToArray())
            : Cons.From(Enumerable.Repeat<Term>(Term.I(StringCooperationNames.Character), StringCooperationNames.Characters));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            long before = await ReadReductions(runtime, context);
            Term actual = await runtime.Modules.Call(
                context,
                StringModule.Name,
                reverse ? StringCooperationNames.Reverse : StringCooperationNames.Length,
                input
            );
            long reductions = await ReadReductions(runtime, context) - before;
            bool value = reverse ? Cons.Items(actual).Count() == StringCooperationNames.Characters
                : actual.Equals(Term.I(StringCooperationNames.Characters));
            if (!value || reductions < StringCooperationNames.Characters)
                throw new InvalidOperationException(StringCooperationNames.Failure);

            return true;
        });
    }

    internal static async Task RunSlice(bool keep)
    {
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
            StringBackendChecks.Selection(true),
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        Term input = new BitString(Enumerable.Repeat(StringCooperationNames.Character, StringCooperationNames.SliceInputCharacters).ToArray());
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            long before = await ReadReductions(runtime, context);
            Term[] arguments = keep ? [input, Term.I(0), Term.I(StringCooperationNames.Characters)]
                : [input, Term.I(StringCooperationNames.Characters)];
            Term actual = await runtime.Modules.Call(
                context,
                StringModule.Name,
                StringCooperationNames.Slice,
                arguments
            );
            long reductions = await ReadReductions(runtime, context) - before;
            if (actual is not BitString binary || binary.BitLength != StringCooperationNames.Characters * StringCooperationNames.BitsPerByte
                || reductions < StringCooperationNames.Characters)
            {
                throw new InvalidOperationException(StringCooperationNames.Failure);
            }

            return true;
        });
    }

    internal static async Task RunScan(bool trim, bool multipleOrComplement)
    {
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
            StringBackendChecks.Selection(true),
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        Term input = Cons.From(Enumerable.Repeat<Term>(Term.I(StringCooperationNames.Character), StringCooperationNames.Characters));
        Term separators = multipleOrComplement && trim
            ? Term.List(Term.I(StringCooperationNames.Separator), Term.I(StringCooperationNames.OtherSeparator))
            : Term.List(Term.I(StringCooperationNames.Separator));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term[] arguments = trim ? [input, Term.A(StringCooperationNames.Trailing), separators]
                : [input, separators, Term.A(multipleOrComplement ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
                    Term.A(multipleOrComplement ? StringCooperationNames.Leading : StringCooperationNames.Trailing)];
            long before = await ReadReductions(runtime, context);
            Term actual = await runtime.Modules.Call(
                context,
                StringModule.Name,
                trim ? StringCooperationNames.Trim : StringCooperationNames.Take,
                arguments
            );
            long reductions = await ReadReductions(runtime, context) - before;
            Term retained = trim ? actual : ((TupleTerm)actual).Items[0];
            if (Cons.Items(retained).Count() != StringCooperationNames.Characters || reductions < StringCooperationNames.Characters
                || !trim && ((TupleTerm)actual).Items[1] is not Nil)
            {
                throw new InvalidOperationException(StringCooperationNames.Failure);
            }

            return true;
        });
    }

    internal static async Task RunEqual(bool explicitCase)
    {
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
            StringBackendChecks.Selection(true),
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        Term left = Cons.From(Enumerable.Repeat<Term>(Term.I(StringCooperationNames.Character), StringCooperationNames.Characters));
        Term right = Cons.From(Enumerable.Repeat<Term>(Term.I(StringCooperationNames.Character), StringCooperationNames.Characters));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term[] arguments = explicitCase ? [left, right, Term.A(ErlangBooleanAtoms.False)] : [left, right];
            long before = await ReadReductions(runtime, context);
            Term actual = await runtime.Modules.Call(
                context,
                StringModule.Name,
                StringCooperationNames.Equal,
                arguments
            );
            long reductions = await ReadReductions(runtime, context) - before;
            if (!actual.Equals(Term.A(ErlangBooleanAtoms.True)) || reductions < StringCooperationNames.Characters)
            {
                throw new InvalidOperationException(StringCooperationNames.Failure);
            }

            return true;
        });
    }

    internal static async Task RunFragments(bool trim)
    {
        await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
            StringBackendChecks.Selection(true),
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        Term input = Cons.From(Enumerable.Repeat<Term>(new BitString([]), StringCooperationNames.Characters));
        Term separators = Term.List(Term.I(StringCooperationNames.Separator));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term[] arguments = trim ? [input, Term.A(StringCooperationNames.Leading), separators] : [input, separators];
            long before = await ReadReductions(runtime, context);
            Term actual = await runtime.Modules.Call(
                context,
                StringModule.Name,
                trim ? StringCooperationNames.Trim : StringCooperationNames.Lexemes,
                arguments
            );
            long reductions = await ReadReductions(runtime, context) - before;
            if (actual is not Nil || reductions < StringCooperationNames.Characters)
            {
                throw new InvalidOperationException(StringCooperationNames.Failure);
            }

            return true;
        });
    }

    private static async Task<long> ReadReductions(ProcessRuntime runtime, ProcessContext context)
    {
        var info = (TupleTerm)await runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            StringCooperationNames.ProcessInfo,
            context.Self,
            Term.A(StringCooperationNames.Reductions)
        );

        return (long)((Integer)info.Items[1]).Value;
    }
}

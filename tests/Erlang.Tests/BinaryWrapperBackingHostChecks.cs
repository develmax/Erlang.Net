namespace Erlang.Tests;

internal static class BinaryWrapperBackingHostChecks
{
    internal static async Task JoinBackingAndCallbackContext()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var parent = new BitString(Enumerable.Repeat((byte)7, 65537).ToArray());
            Term part = await Call(context, BinaryModuleNames.Part, [parent, Term.Tuple(Term.I(17), Term.I(257))]);
            Term singleton = await Call(context, BinaryModuleNames.Join, [Term.List(part), new BitString([])]);
            Term pair = await Call(context, BinaryModuleNames.Join, [Term.List(part, new BitString([])), new BitString([])]);
            Term before = await Call(context, BinaryModuleNames.ReferencedByteSize, [part]);
            Term singleBacking = await Call(context, BinaryModuleNames.ReferencedByteSize, [singleton]);
            Term pairBacking = await Call(context, BinaryModuleNames.ReferencedByteSize, [pair]);
            if (!part.Equals(pair) || !part.Equals(singleton)
                || !before.Equals(Term.I(65537)) || !singleBacking.Equals(before) || !pairBacking.Equals(Term.I(514)))
                throw new InvalidOperationException(BinaryWrapperBackingHostNames.Failure);
            await Callback(context, false);
            await Callback(context, true);

            return Nil.Value;
        });
    }

    private static async Task Callback(ProcessContext context, bool global)
    {
        int callbacks = 0;
        var function = new FunctionTerm(
            1,
            async (current, arguments) =>
        {
            try
            {
                var callbackContext = (ProcessContext)current;
                await callbackContext.Runtime.Modules.Call(
                    callbackContext,
                    ErlangModuleNames.Module,
                    BinaryWrapperBackingHostNames.Error,
                    [Term.A(BinaryWrapperBackingHostNames.Marker)]
                );
                throw new InvalidOperationException(BinaryWrapperBackingHostNames.Failure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(BinaryWrapperBackingHostNames.Marker)))
            {
                var frames = Cons.Items(error.StackTraceTerm).Cast<TupleTerm>()
                    .Where(frame => frame.Items[0].Equals(Term.A(BinaryModuleNames.Module))).ToArray();
                int index = 0;
                Check(
                    frames[index++],
                    BinaryWrapperBackingHostNames.DoReplace,
                    4,
                    BinaryWrapperBackingHostNames.RecursiveCallbackLine
                );
                if (global && callbacks > 0)
                    Check(
                        frames[index++],
                        BinaryWrapperBackingHostNames.DoReplace,
                        4,
                        BinaryWrapperBackingHostNames.RecursiveContinuationLine
                    );
                Check(
                    frames[index++],
                    BinaryModuleNames.Replace,
                    4,
                    BinaryWrapperBackingHostNames.ReplaceBodyLine
                );
                if (!global)
                    Check(
                        frames[index++],
                        BinaryModuleNames.Replace,
                        3,
                        BinaryWrapperBackingHostNames.ReplaceThreeLine
                    );
                if (index != frames.Length)
                    throw new InvalidOperationException(BinaryWrapperBackingHostNames.Failure);
            }
            callbacks++;

            return arguments[0];
        }
        );
        var subject = new BitString(new byte[] { 1, 1, 1 });
        Term[] arguments = global
            ? [subject, new BitString(new byte[] { 1 }), function, Term.List(Term.A(BinaryWrapperBackingHostNames.Global))]
            : [subject, new BitString(new byte[] { 1 }), function];
        Term result = await Call(context, BinaryModuleNames.Replace, arguments);
        if (!result.Equals(subject) || callbacks != (global ? 3 : 1))
            throw new InvalidOperationException(BinaryWrapperBackingHostNames.Failure);
    }

    private static void Check(
        TupleTerm frame,
        string function,
        int arity,
        int line
    )
    {
        if (!frame.Items[1].Equals(Term.A(function)) || !frame.Items[2].Equals(Term.I(arity)))
            throw new InvalidOperationException(BinaryWrapperBackingHostNames.Failure);
        var location = Cons.Items(frame.Items[3]).Cast<TupleTerm>()
            .Single(entry => entry.Items[0].Equals(Term.A(BinaryWrapperBackingHostNames.Line)));
        if (!location.Items[1].Equals(Term.I(line)))
            throw new InvalidOperationException(BinaryWrapperBackingHostNames.Failure);
    }

    private static ValueTask<Term> Call(ProcessContext context, string function, IReadOnlyList<Term> arguments)
        => context.Runtime.Modules.Call(
            context,
            BinaryModuleNames.Module,
            function,
            arguments.ToArray()
        );
}

namespace Erlang.Tests;

public static class TermDisplayHostChecks
{
    public static async Task Run()
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        using var streams = new RuntimeStandardStreams(output, error);
        await using var runtime = new ProcessRuntime(streams);
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                Term nested = Term.I(0);
                for (int i = 0; i < TermDisplayHostParameters.NestedTuples; i++)
                    nested = Term.Tuple(nested);
                Require((await Call(context, nested)).Equals(Term.A(ErlangBooleanAtoms.True)));
                byte[] bytes = output.ToArray();
                int depth = TermDisplayHostParameters.NestedTuples;
                Require(bytes.Length == depth * 2 + 3 && bytes[depth] == TermDisplayHostParameters.Zero && bytes[^2] == TermDisplayHostParameters.CarriageReturn && bytes[^1] == TermDisplayHostParameters.NewLine);
                Require(bytes.AsSpan(0, depth).IndexOfAnyExcept(TermDisplayHostParameters.OpenTuple) < 0 && bytes.AsSpan(depth + 1, depth).IndexOfAnyExcept(TermDisplayHostParameters.CloseTuple) < 0);
                output.SetLength(0);
                await Call(context, Term.List(Term.I(160), Term.I(255)));
                Require(output.ToArray().SequenceEqual(TermDisplayHostParameters.LatinString) && error.Length == 0);
                output.SetLength(0);
                Atom first = Term.A(TermDisplayHostParameters.FirstMapAtom);
                Atom second = Term.A(TermDisplayHostParameters.SecondMapAtom);
                var map = new MapTerm([new(second, Term.I(2)), new(first, Term.I(1)), new(Term.I(0), Term.I(0))]);
                Require(map.Entries[1].Key.Equals(second));
                await Call(context, map);
                Require(output.ToArray().SequenceEqual(TermDisplayHostParameters.InternalMapOrder));
                output.SetLength(0);
                var identity = new LocalFunctionIdentity(
                    Term.A(TermDisplayHostParameters.EmbeddedNewlineModule),
                    null,
                    0,
                    0,
                    new BitString(new byte[TermDisplayHostParameters.FunctionDigestBytes])
                );
                await Call(context, new FunctionTerm(
                    identity,
                    0,
                    [],
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                ));
                Require(output.ToArray().SequenceEqual(TermDisplayHostParameters.EmbeddedNewlineFunction));
                output.Dispose();
                Require((await Call(context, Term.I(0))).Equals(Term.A(ErlangBooleanAtoms.True)));
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Term.A(ErlangBooleanAtoms.True);
        });
        await process.Completion.WaitAsync(TermDisplayHostParameters.Timeout);
        if (failure is not null)
            throw failure;
    }

    private static ValueTask<Term> Call(ProcessContext context, Term value) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        TermDisplayHostNames.Function,
        value
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(TermDisplayHostNames.Failure);
    }
}

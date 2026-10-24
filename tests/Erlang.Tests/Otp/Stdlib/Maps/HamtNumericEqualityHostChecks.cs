namespace Erlang.Tests;

internal static class HamtNumericEqualityHostChecks
{
    internal static async Task Run()
    {
        await using var firstRuntime = new ProcessRuntime();
        await using var secondRuntime = new ProcessRuntime();
        await using var firstActor = new RuntimeNodeFoundationHostActor(firstRuntime);
        await using var secondActor = new RuntimeNodeFoundationHostActor(secondRuntime);

        FunctionTerm Local(byte unique, int oldUnique) => new(
            new LocalFunctionIdentity(
                Term.A(HamtNumericEqualityHostNames.Module),
                null,
                0,
                oldUnique,
                new BitString(Enumerable.Repeat(unique, HamtNumericEqualityHostNames.UniqueBytes).ToArray())
            ),
            0,
            Array.Empty<Term>(),
            (_, _) => ValueTask.FromResult<Term>(Term.I(0))
        );

        Term a = Local(byte.MinValue, HamtNumericEqualityHostNames.FirstOldUnique);
        Term b = Local(byte.MaxValue, HamtNumericEqualityHostNames.SecondOldUnique);
        Term c = Local(HamtNumericEqualityHostNames.ThirdUnique, HamtNumericEqualityHostNames.SecondOldUnique);
        Term d = Local(HamtNumericEqualityHostNames.FourthUnique, HamtNumericEqualityHostNames.FirstOldUnique);
        var prefix = Enumerable.Range(1, HamtNumericEqualityHostNames.HamtEntries)
            .Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(index)))
            .ToArray();
        var left = new MapTerm(prefix.Concat([new(a, Term.I(0)), new(b, Term.I(1))]));
        var right = new MapTerm(prefix.Concat([new(c, Term.I(1)), new(d, Term.I(0))]));
        var different = new MapTerm(prefix.Concat([new(c, Term.I(0)), new(d, Term.I(1))]));

        async Task Check(RuntimeNodeFoundationHostActor actor, ProcessRuntime runtime)
        {
            await actor.InvokeAsync(async context =>
            {
                Require(left.NumericEquals(right) && right.NumericEquals(left));
                Require(!left.NumericEquals(different));
                await Task.Yield();
                Require(left.NumericEquals(right));
                Require(ErlangOperatorValues.Binary(
                    HamtNumericEqualityHostNames.EqualOperator,
                    left,
                    right,
                    context
                ).Equals(CoreModules.Boolean(true)));
                Require((await runtime.Modules.Call(
                    context,
                    HamtNumericEqualityHostNames.Ordsets,
                    HamtNumericEqualityHostNames.IsEqual,
                    Term.List(left),
                    Term.List(right)
                )).Equals(CoreModules.Boolean(true)));

                return true;
            });
        }

        await Task.WhenAll(Check(firstActor, firstRuntime), Check(secondActor, secondRuntime));
        var ordinaryLeft = new MapTerm(prefix);
        var ordinaryRight = new MapTerm(prefix);
        var probe = new HamtNumericLayoutProbe(false);
        int allocated = 0;
        using (TermMapComparisonEnvironment.EnterThread(() =>
 {
     allocated++;

     return probe;
 }))
        {
            Require(Term.I(1).NumericEquals(new FloatTerm(1)) && allocated == 0);
            Require(ordinaryLeft.NumericEquals(ordinaryRight));
            Require(allocated == 1 && probe.ReadCount == 2 && probe.DisposeCount == 1);
        }
        Require(ordinaryLeft.NumericEquals(ordinaryRight));
        Require(allocated == 1);
        var cancelled = new HamtNumericLayoutProbe(true);
        using (TermMapComparisonEnvironment.EnterThread(() => cancelled))
        {
            bool failed = false;
            try
            {
                _ = ordinaryLeft.NumericEquals(ordinaryRight);
            }
            catch (OperationCanceledException)
            {
                failed = true;
            }
            Require(failed && cancelled.ReadCount == 1 && cancelled.DisposeCount == 1);
        }
        Require(ordinaryLeft.NumericEquals(ordinaryRight));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(HamtNumericEqualityHostNames.Failure);
    }
}

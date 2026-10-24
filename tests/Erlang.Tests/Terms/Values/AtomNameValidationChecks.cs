using System.Reflection;
using System.Text;

namespace Erlang.Tests;

internal static class AtomNameValidationChecks
{
    internal static async Task Run()
    {
        foreach (string name in AtomNameValidationTestNames.CachedNames)
            Check(ReferenceEquals(Term.A(name), Term.A(name)));
        Type primordial = typeof(Atom).Assembly.GetType(AtomNameValidationTestNames.PrimordialType)!;
        var nativeNames = (IReadOnlyList<string>)primordial.GetField(AtomNameValidationTestNames.PrimordialField, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var indices = new Dictionary<string, int>(StringComparer.Ordinal) { [ErlangBooleanAtoms.False] = 0 };
        foreach (string name in nativeNames)
            if (!indices.ContainsKey(name))
                indices.Add(name, indices.Count);
        foreach (var pair in indices)
            Check(AtomNameRegistry.IndexOf(pair.Key) == pair.Value);
        foreach (string name in AtomNameValidationTestNames.InvalidNames)
        {
            int before = AtomNameRegistry.Count;
            Reject(() => new Atom(name), ErlangErrorReasons.BadArgument);
            Reject(() => Term.A(name), ErlangErrorReasons.BadArgument);
            Reject(() => AtomNameRegistry.Observe(name), ErlangErrorReasons.BadArgument);
            Reject(() => AtomNameRegistry.IndexOf(name), ErlangErrorReasons.BadArgument);
            Check(!AtomNameRegistry.Exists(name) && AtomNameRegistry.Count == before);
        }
        int beforeParallel = AtomNameRegistry.Count;
        Parallel.For(
            0,
            AtomNameValidationTestParameters.ConcurrentRejections,
            _ =>
            Reject(() => AtomNameRegistry.Observe(AtomNameValidationTestNames.InvalidNames[0]), ErlangErrorReasons.BadArgument)
        );
        Check(AtomNameRegistry.Count == beforeParallel);
        string maximum = string.Concat(Enumerable.Repeat(AtomNameValidationTestNames.Supplementary, AtomNameValidationTestParameters.MaximumScalars));
        string over = maximum + AtomNameValidationTestNames.Supplementary;
        var valid = new Atom(maximum);
        Check(valid.Name == maximum && valid.Name.EnumerateRunes().Count() == AtomNameValidationTestParameters.MaximumScalars);
        Check(valid.Equals(new Atom(maximum)) && valid.GetHashCode() == new Atom(maximum).GetHashCode());
        int beforeOver = AtomNameRegistry.Count;
        Reject(() => new Atom(over), ErlangErrorReasons.SystemLimit);
        Reject(() => AtomNameRegistry.Observe(over), ErlangErrorReasons.SystemLimit);
        Check(!AtomNameRegistry.Exists(over) && AtomNameRegistry.Count == beforeOver);
        string asciiOver = new('x', AtomNameValidationTestParameters.OverMaximumScalars);
        Reject(() => new Atom(asciiOver), ErlangErrorReasons.SystemLimit);
        Check(!AtomNameRegistry.Exists(asciiOver) && AtomNameRegistry.Count == beforeOver);
        var replacement = Term.A(AtomNameValidationTestNames.Replacement);
        var supplementary = Term.A(AtomNameValidationTestNames.Supplementary);
        Check(!replacement.Equals(supplementary));
        var map = new MapTerm(new[]
        {
            new KeyValuePair<Term, Term>(valid, Term.I(1)),
            new KeyValuePair<Term, Term>(new Atom(maximum), Term.I(2)),
            new KeyValuePair<Term, Term>(replacement, Term.I(3)),
            new KeyValuePair<Term, Term>(supplementary, Term.I(4))
        });
        Check(map.Entries.Count == 3 && map.Get(valid).Equals(Term.I(2)));
        await using var runtime = new ProcessRuntime();
        var actor = runtime.Spawn(async context =>
        {
            Term reportedCount = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.SystemInfo,
                Term.A(AtomRegistryInfoNames.AtomCount)
            );
            Check(reportedCount.Equals(Term.I(AtomNameRegistry.Count)));
            string freshName = AtomNameValidationTestNames.FreshNamePrefix + Guid.NewGuid().ToString(AtomNameValidationTestNames.GuidFormat);
            Check(!AtomNameRegistry.Exists(freshName));
            Term firstCount = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.SystemInfo,
                Term.A(AtomRegistryInfoNames.AtomCount)
            );
            Term freshAtom = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                AtomNameValidationTestNames.ListToAtom,
                Term.String(freshName)
            );
            Term addedCount = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.SystemInfo,
                Term.A(AtomRegistryInfoNames.AtomCount)
            );
            Check(((Integer)addedCount).Value == ((Integer)firstCount).Value + 1);
            Term sameAtom = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                AtomNameValidationTestNames.BinaryToAtom,
                new BitString(Encoding.UTF8.GetBytes(freshName))
            );
            Check(sameAtom.Equals(freshAtom));
            Check((await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SequenceTraceNames.SystemInfo,
                Term.A(AtomRegistryInfoNames.AtomCount)
            )).Equals(addedCount));
            var list = Cons.From(Enumerable.Repeat<Term>(Term.I(AtomNameValidationTestParameters.SupplementaryScalar), AtomNameValidationTestParameters.MaximumScalars));
            Term fromList = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                AtomNameValidationTestNames.ListToAtom,
                list
            );
            Check(fromList.Equals(valid));
            var bytes = new BitString(Encoding.UTF8.GetBytes(maximum));
            Term fromBinary = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                AtomNameValidationTestNames.BinaryToAtom,
                bytes,
                Term.A(AtomNameValidationTestNames.Utf8)
            );
            Check(fromBinary.Equals(valid));
            Check((await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                AtomNameValidationTestNames.AtomToBinary,
                valid
            )).Equals(bytes));
            await RejectAsync(
                () => runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    AtomNameValidationTestNames.ListToAtom,
                    Term.List(Term.I(AtomNameValidationTestParameters.SurrogateScalar))
                ),
                ErlangErrorReasons.BadArgument
            );
            await RejectAsync(
                () => runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    AtomNameValidationTestNames.BinaryToAtom,
                    new BitString(new byte[] { 237, 160, 128 })
                ),
                ErlangErrorReasons.BadArgument
            );
            await RejectAsync(
                () => runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    AtomNameValidationTestNames.BinaryToAtom,
                    new BitString(Encoding.UTF8.GetBytes(over))
                ),
                ErlangErrorReasons.SystemLimit
            );

            return Nil.Value;
        });
        Check((await actor.Completion).Equals(Term.A(ProcessExitReasons.Normal)));
        foreach (var pair in indices)
            Check(AtomNameRegistry.IndexOf(pair.Key) == pair.Value);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(AtomNameValidationTestNames.Failure);
    }

    private static void Reject(Action action, string reason)
    {
        try
        {
            action();
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(reason)))
        {
            return;
        }

        throw new InvalidOperationException(AtomNameValidationTestNames.Accepted);
    }

    private static async Task RejectAsync(Func<ValueTask<Term>> action, string reason)
    {
        try
        {
            await action();
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(reason)))
        {
            return;
        }

        throw new InvalidOperationException(AtomNameValidationTestNames.Accepted);
    }
}

using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.SourceBinaryComposition511Names;

namespace Erlang.Tests;

internal static class SourceBinaryComposition511Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task RetirementAndHiddenMap()
    {
        Fallbacks();
        using var compiled = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        compiled.Definition.Register(runtime.Modules);
        Term? hiddenKey = null;
        runtime.Modules.Register(
            Probe,
            Make,
            0,
            (_, _) => ValueTask.FromResult<Term>(new BitString(new byte[PayloadBytes]))
        );
        runtime.Modules.Register(
            Probe,
            MakeMap,
            0,
            (_, _) =>
        {
            MapTerm map = HiddenMap(runtime, new BitString(new byte[PayloadBytes]), out Term key);
            hiddenKey = key;

            return ValueTask.FromResult<Term>(map);
        }
        );
        foreach (var entry in new[] { (Name: Live, Expected: (long)PayloadBytes), (Name: Dead, Expected: 0L) })
            runtime.Modules.Register(
                Probe,
                entry.Name,
                0,
                (context, _) =>
            {
                object heap = Get(context, NativeMapOwned510Names.Heap);
                Call(heap, NativeMapOwned510Names.Collect);
                Check((long)Get(Get(heap, BinarySnapshot), Bytes) == entry.Expected);

                return ValueTask.FromResult<Term>(Nil.Value);
            }
            );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Check((await Invoke(Discarded)).Equals(Term.A(Ok)));
        BitString closureValue = (BitString)await Invoke(Closure);
        Check(closureValue.ToArray().Length == PayloadBytes);
        MapTerm mapValue = (MapTerm)await Invoke(Map);
        Check(mapValue.TryGet(hiddenKey!, out Term? hidden) && hidden is BitString binary
            && binary.ToArray().Length == PayloadBytes);
        try
        {
            _ = await Invoke(Equality);
            throw new InvalidOperationException(Failure);
        }
        catch (ErlangException error)
        {
            Check(error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Term.I(2))));
        }
        ProcessContext owner = await actor.Ready;
        Check((long)Get(Get(Get(owner, NativeMapOwned510Names.Heap), BinarySnapshot), Bytes) == 0);

        async Task<Term> Invoke(string function) => await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(context, GuestModule, function));
    }

    private static void Fallbacks()
    {
        var assembly = typeof(Erlang.Compiler.Expr).Assembly;
        MethodInfo complete = assembly.GetType(ContinuationType, true)!.GetMethod(Complete, Members)!;
        var unknownBindings = new Dictionary<string, Term>(StringComparer.Ordinal)
        {
            [OuterName] = Term.I(1),
            [HiddenConstraint] = Term.I(2)
        };
        var unknown = new Erlang.Compiler.Expr.Sequence(
            new Erlang.Compiler.Expr[] { new Erlang.Compiler.Expr.Literal(Nil.Value), new UnknownSourceLiveness511Expr() });
        var kept = (Term[])complete.Invoke(null, new object[] { unknown, 0, unknownBindings })!;
        Check(kept.Length == 2 && unknownBindings.ContainsKey(OuterName) && unknownBindings.ContainsKey(HiddenConstraint));
        var constraints = (Dictionary<string, Term>)Activator.CreateInstance(
            assembly.GetType(ConstraintType, true)!,
            Members,
            null,
            new object[]
            {
                new Dictionary<string, Term> { [OuterName] = Term.I(1) },
                new Dictionary<string, Term> { [HiddenConstraint] = Term.I(2) }
            },
            null
        )!;
        var sequence = new Erlang.Compiler.Expr.Sequence(
            new Erlang.Compiler.Expr[] { new Erlang.Compiler.Expr.Literal(Nil.Value), new Erlang.Compiler.Expr.Literal(Nil.Value) });
        kept = (Term[])complete.Invoke(null, new object[] { sequence, 0, constraints })!;
        Check(kept.Contains(Term.I(1)) && kept.Contains(Term.I(2)) && constraints.ContainsKey(OuterName));
    }

    private static MapTerm HiddenMap(ProcessRuntime runtime, BitString binary, out Term hiddenKey)
    {
        var seed = new MapTerm(Enumerable.Range(0, 33).Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(0))));
        using var read = (IDisposable)Call(runtime, NativeMapOwned510Names.Acquire, seed)!;
        object index = Get(read, NativeMapOwned510Names.IndexProperty);
        object retention = New(NativeMapOwned510Names.RetentionType, Get(index, NativeMapOwned510Names.Ownership), null);
        object domain = Call(runtime, NativeMapOwned510Names.NativeDomain)!;
        object?[] args = [Get(index, NativeMapOwned510Names.Root), domain, retention, binary, false, null];
        object tree = typeof(NativeMapOwned510Checks).GetMethod(CloneTree, Members)!.Invoke(null, args)!;
        hiddenKey = (Term)args[5]!;
        KeyValuePair<Term, Term>[] entries = seed.Entries.ToArray();
        object layout = New(
            NativeMapOwned510Names.LayoutType,
            runtime,
            domain,
            entries,
            tree,
            retention
        );

        return (MapTerm)Call(
            Core(NativeMapOwned510Names.MaterializationType),
            NativeMapOwned510Names.Publish,
            runtime,
            layout,
            entries,
            tree,
            entries,
            entries,
            null
        )!;
    }

    private static Type Core(string name) => typeof(ProcessRuntime).Assembly.GetType(name, true)!;

    private static object New(string name, params object?[] args) => Activator.CreateInstance(
        Core(name),
        Members,
        null,
        args,
        null
    )!;

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static object? Call(object target, string name, params object?[] args)
    {
        Type type = target is Type selected ? selected : target.GetType();
        MethodInfo method = type.GetMethods(Members).Single(candidate => candidate.Name == name
            && candidate.GetParameters().Length == args.Length
            && candidate.GetParameters().Select((parameter, index) => args[index] is null
                || parameter.ParameterType.IsInstanceOfType(args[index])).All(value => value));
        try
        {
            return method.Invoke(target is Type ? null : target, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}

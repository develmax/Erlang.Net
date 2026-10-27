using System.Reflection;
using System.Globalization;
using System.Runtime.ExceptionServices;
using Erlang.Differential;
using static Erlang.Tests.PrivateSelfGc519Names;

namespace Erlang.Tests;

internal static class PrivateSelfGc519Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task RealBifReclaims()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            object heap = Property(context, Heap);
            using var frame = (IDisposable)Call(heap, Enter)!;
            int slot = (int)Call(frame, Reserve)!;
            Call(
                frame,
                Set,
                slot,
                Term.Tuple(Term.List(Term.I(1), Term.I(2)))
            );
            Term unrooted = (Term)Call(frame, Read, slot)!;
            long before = (long)Property(Property(heap, Snapshot), Words);
            Check(before > 0);
            Call(frame, Clear, slot);
            Term answer = await runtime.Modules.Call(context, ErlangModule, GarbageCollect);
            long after = (long)Property(Property(heap, Snapshot), Words);
            if (after != 0)
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    ReclaimDiagnostic,
                    Failure,
                    before,
                    after
                ));
            Check(answer.Equals(Term.A(ErlangBooleanAtoms.True)));
            bool retired = false;
            try
            {
                _ = ((TupleTerm)unrooted).Items[0];
            }
            catch (ObjectDisposedException)
            {
                retired = true;
            }
            Check(retired);

            return true;
        });
    }

    internal static async Task LiveHiddenAndBinary()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            object heap = Property(context, Heap), registry = Property(runtime, Registry);
            using var frame = (IDisposable)Call(heap, Enter)!;
            int input = (int)Call(frame, Reserve)!, mapped = (int)Call(frame, Reserve)!;
            var binary = new BitString(new byte[BackingBytes]);
            Call(
                frame,
                Set,
                input,
                Term.Tuple(Term.List(Term.I(1)), binary)
            );
            Term owned = (Term)Call(frame, Read, input)!;
            MapTerm map = HiddenMap(runtime, owned, out Term key);
            Call(
                frame,
                SetLocal,
                mapped,
                map
            );
            Call(frame, Clear, input);
            _ = await runtime.Modules.Call(context, ErlangModule, GarbageCollect);
            Check(map.TryGet(key, out Term? hidden) && hidden is TupleTerm tuple
                && tuple.Items[0].Equals(Term.List(Term.I(1)))
                && tuple.Items[1] is BitString actual && actual.ToArray().Length == BackingBytes);
            Check((long)Property(Property(heap, Snapshot), Words) > 0);
            Check((long)Property(Call(registry, BinarySnapshot, (object?)null)!, ReferenceBytes) == BackingBytes);
            Call(frame, Clear, mapped);
            _ = await runtime.Modules.Call(context, ErlangModule, GarbageCollect);
            Check((long)Property(Property(heap, Snapshot), Words) == 0);
            Check((long)Property(Call(registry, BinarySnapshot, (object?)null)!, ReferenceBytes) == 0);

            return true;
        });
    }

    internal static async Task OrdinaryNoHeap()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(false);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Check(typeof(ProcessContext).GetField(HeapField, Members)!.GetValue(context) is null);
            Term answer = await runtime.Modules.Call(context, ErlangModule, GarbageCollect);
            Check(answer.Equals(Term.A(ErlangBooleanAtoms.True)));
            Check(typeof(ProcessContext).GetField(HeapField, Members)!.GetValue(context) is null);

            return true;
        });
    }

    internal static async Task ExplicitCollection()
    {
        using var compiled = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        compiled.Definition.Register(runtime.Modules);
        var observed = new List<long>();
        runtime.Modules.Register(
            Probe,
            Sample,
            0,
            (context, _) =>
        {
            object heap = Property(context, Heap);
            long words = (long)Property(Property(heap, Snapshot), Words);
            observed.Add(words);

            return ValueTask.FromResult<Term>(Term.I(words));
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        _ = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            GuestModule,
            Discarded,
            Term.I(Count)
        ));
        Check(observed.Count == 2 && observed[0] > 0 && observed[1] == 0);
        Term kept = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            GuestModule,
            Kept,
            Term.I(Count)
        ));
        Check(kept.Equals(Term.Tuple(Term.I(Count), Term.I(1), Term.I(Count))));
    }

    private static MapTerm HiddenMap(ProcessRuntime runtime, Term owned, out Term key)
    {
        var seed = new MapTerm(Enumerable.Range(0, SeedMapSize).Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(0))));
        using var read = (IDisposable)Call(runtime, NativeMapOwned510Names.Acquire, seed)!;
        object index = Property(read, NativeMapOwned510Names.IndexProperty);
        object retention = New(NativeMapOwned510Names.RetentionType, Property(index, NativeMapOwned510Names.Ownership), null);
        object domain = Call(runtime, NativeMapOwned510Names.NativeDomain)!;
        object?[] arguments = [Property(index, NativeMapOwned510Names.Root), domain, retention, owned, false, null];
        object tree = typeof(NativeMapOwned510Checks).GetMethod(SourceBinaryComposition511Names.CloneTree, Members)!.Invoke(null, arguments)!;
        key = (Term)arguments[5]!;
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

    private static object? Call(object target, string name, params object?[] args)
    {
        Type type = target is Type selected ? selected : target.GetType();
        MethodInfo method = type.GetMethods(Members).Single(candidate => candidate.Name == name
            && candidate.GetParameters().Length == args.Length
            && candidate.GetParameters().Select((parameter, index) => args[index] is null
                || parameter.ParameterType.IsInstanceOfType(args[index])).All(match => match));
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

    private static object Property(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}

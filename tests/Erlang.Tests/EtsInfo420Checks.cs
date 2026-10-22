using System.Reflection;
using System.Collections;
using System.Numerics;
using Erlang;
namespace Erlang.Tests;

internal static class EtsInfo420Checks
{
    private static void Require(bool ok)
    {
        if (!ok) throw new InvalidOperationException(EtsInfo420Names.Failure);
    }

    private static void Install(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsFixation420.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsInfo420.Register(runtime, runtime.Modules);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        string function,
        params Term[] args
    ) => runtime.Modules.Call(
        context,
        EtsInfo420Names.Ets,
        function,
        args
    );

    private static async Task<Term> Create(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, string name) => await actor.InvokeAsync(async context => await Call(
        runtime,
        context,
        EtsInfo420Names.New,
        Term.A(name),
        Term.List(Term.A(EtsInfo420Names.Named), Term.A(EtsInfo420Names.Public))
    ));

    private static async Task<BigInteger> Memory(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, Term table) => ((Integer)await actor.InvokeAsync(async context => await Call(
        runtime,
        context,
        EtsInfo420Names.Info,
        table,
        Term.A(EtsInfo420Names.Memory)
    ))).Value;

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime(1);
        Require(!runtime.Modules.HasExport(EtsInfo420Names.Ets, EtsInfo420Names.Info, 1));
        RuntimeBootstrapEtsInfo420.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsInfo420.Register(runtime, runtime.Modules);
        Require(runtime.Modules.SourceOwner(EtsInfo420Names.Ets, EtsInfo420Names.Info, 1) is null && runtime.Modules.SourceOwner(EtsInfo420Names.Ets, EtsInfo420Names.Info, 2) is null);
        // The provider never substitutes the required original Erlang binary-inspection helper.
        Require(!runtime.Modules.HasExport(EtsInfo420Names.Internal, EtsInfo420Names.Function, 1));
        await using var foreign = new ProcessRuntime(1);
        bool rejected = false;
        try
        {
            RuntimeBootstrapEtsInfo420.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
        await using var rollback = new ProcessRuntime(1);
        var expected = new InvalidOperationException(EtsInfo420Names.Failure);
        try
        {
            await rollback.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapEtsInfo420.Register(rollback, registry);
                throw expected;
            });
        }
        catch (InvalidOperationException error) when (ReferenceEquals(error, expected)) { }
        Require(!rollback.Modules.HasExport(EtsInfo420Names.Ets, EtsInfo420Names.Info, 1) && !rollback.Modules.HasExport(EtsInfo420Names.Ets, EtsInfo420Names.Info, 2));
    }

    internal static async Task MemoryGraph()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term first = await Create(runtime, actor, EtsInfo420Names.Name), second = await Create(runtime, actor, EtsInfo420Names.Second);
        var baseline = await Memory(runtime, actor, first);
        Require(baseline > 0);
        BitString shared = new BitString(new byte[4096]);
        _ = await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInfo420Names.Insert,
            first,
            Term.Tuple(Term.I(1), shared, shared)
        ));
        _ = await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInfo420Names.Insert,
            second,
            Term.Tuple(Term.I(1), new BitString(new byte[4096]), new BitString(new byte[4096]))
        ));
        BigInteger one = await Memory(runtime, actor, first), two = await Memory(runtime, actor, second);
        Require(one > baseline && two - one >= 4096 / IntPtr.Size);
        _ = await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInfo420Names.Insert,
            first,
            Term.Tuple(Term.I(1), new BitString(new byte[65536]))
        ));
        _ = await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInfo420Names.Fix,
            first,
            Term.A(EtsInfo420Names.True)
        ));
        _ = await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInfo420Names.Delete,
            first,
            Term.I(1)
        ));
        BigInteger retained = await Memory(runtime, actor, first);
        Require(retained > one);
        _ = await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInfo420Names.Fix,
            first,
            Term.A(EtsInfo420Names.False)
        ));
        BigInteger released = await Memory(runtime, actor, first);
        Require(retained - released >= 65536 / IntPtr.Size);
    }

    internal static async Task CensusBoundaries()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term tableId = await Create(runtime, actor, EtsInfo420Names.Name);
        BitString data = new BitString(new byte[2048]);
        _ = await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsInfo420Names.Insert,
            tableId,
            Term.Tuple(
                Term.I(1),
                data,
                data,
                context.Self
            )
        ));
        object foundation = EtsFoundation405Access.Get(runtime);
        Type type = foundation.GetType();
        object gate = typeof(ProcessRuntime).GetField(EtsInfo420Names.Gate, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(runtime)!;
        var observed = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Type extent = typeof(ProcessRuntime).Assembly.GetType(EtsInfo420Names.ExtentType)!;
        var actual = extent.GetMethod(EtsInfo420Names.Measure, BindingFlags.NonPublic | BindingFlags.Static)!.CreateDelegate<Func<object, nuint>>();
        Func<object, nuint> reader = value =>
      {
          Require(observed.Add(value));

          return actual(value);
      };
        try
        {
            ulong bytes;
            lock (gate)
            {
                var names = (IDictionary)type.GetField(EtsInfo420Names.FoundationNames, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(foundation)!;
                object table = names[EtsInfo420Names.Name]!;
                object metadata = type.GetField(EtsInfo420Names.Options, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(foundation)!;
                object?[] args = [table, null];
                Require((bool)metadata.GetType().GetMethod(EtsInfo420Names.TryGet)!.Invoke(metadata, args)!);
                object census = type.GetMethod(EtsInfo420Names.Census, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [table, args[1]!, reader, CancellationToken.None])!;
                bytes = (ulong)census.GetType().GetProperty(EtsInfo420Names.Bytes, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(census)!;
            }
            Require((await Memory(runtime, actor, tableId)) * IntPtr.Size == bytes);
            Require(observed.Any(value => value.GetType().Name == EtsInfo420Names.OwnerType) && observed.Any(value => value.GetType().Name == EtsInfo420Names.IndexType) && observed.Any(value => value.GetType().Name == EtsInfo420Names.PayloadType));
            Require(!observed.Any(value => value is Term or string or ProcessRuntime or ProcessContext or ModuleRegistry));
            Require(observed.OfType<byte[]>().Count(buffer => buffer.Length == 2048) == 1);
        }
        finally
        {
            observed.Clear();
        }
    }
}

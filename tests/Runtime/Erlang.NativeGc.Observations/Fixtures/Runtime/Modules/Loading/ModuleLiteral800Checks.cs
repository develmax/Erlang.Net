using System.Reflection;
using static Erlang.Tests.ModuleLiteral800Names;

namespace Erlang.Tests;

internal static class ModuleLiteral800Checks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        Term source = Term.Tuple(
            Term.I(800),
            new FloatTerm(1.25),
            new MapTerm([]),
            new BitString(new byte[32])
        );
        byte[] bytes = BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(source));
        await using PreparedBeamImage prepared = runtime.Modules.PrepareBeam(bytes);
        object code = Get(prepared, Code);
        var table = (IReadOnlyList<Term>)Get(Get(code, Structure), Literals);
        TupleTerm literal = table.OfType<TupleTerm>().Single(value => value.Items.Count == 4);
        object storage = literal.Items;
        Require(storage.GetType().FullName == Sequence);
        object heap = Get(storage, Owner);
        Require(Get(heap, Domain) is not null && literal.Equals(source));
        ModuleCodeImage image = (ModuleCodeImage)Get(Get(prepared, Execution), Image);
        SourceCodeLease? admitted = null;
        IDisposable? derived = null;
        IDisposable? chained = null;
        Task? closing = null;
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            await actor.InvokeAsync(context =>
            {
                admitted = runtime.Modules.ReserveSourceCode(context, image);
                using IDisposable attached = admitted.Attach(prepared);
                MethodInfo fork = context.GetType().GetMethod(Fork, Members)!;
                derived = (IDisposable?)fork.Invoke(context, [literal]);
                Require(derived is not null);
                Require(fork.Invoke(context, [source]) is null);

                return Task.FromResult(true);
            });
            closing = prepared.DisposeAsync().AsTask();
            Require(!closing.IsCompleted && literal.Items[0].Equals(Term.I(800)));
            await actor.InvokeAsync(context =>
            {
                bool rejected = false;
                try
                {
                    using SourceCodeLease impossible = runtime.Modules.ReserveSourceCode(context, image);
                }
                catch (ObjectDisposedException)
                {
                    rejected = true;
                }
                Require(rejected);

                return Task.FromResult(true);
            });
            admitted!.Dispose();
            admitted = null;
            Require(!closing.IsCompleted && literal.Items[0].Equals(Term.I(800)));
            object?[] arguments = [literal, null];
            Require((bool)derived!.GetType().GetMethod(Derive, Members)!.Invoke(derived, arguments)!);
            chained = (IDisposable)arguments[1]!;
            derived.Dispose();
            derived = null;
            Require(!closing.IsCompleted && literal.Items[0].Equals(Term.I(800)));
            chained.Dispose();
            chained = null;
            await closing.WaitAsync(Timeout);
            bool stale = false;
            try
            {
                _ = literal.Items[0];
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale);
            GC.KeepAlive(source);
            GC.KeepAlive(literal);
        }
        finally
        {
            admitted?.Dispose();
            derived?.Dispose();
            chained?.Dispose();
            if (closing is not null) await closing.WaitAsync(Timeout);
        }
    }
}

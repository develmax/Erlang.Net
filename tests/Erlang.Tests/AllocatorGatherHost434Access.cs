using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;
namespace Erlang.Tests;

internal static class AllocatorGatherHost434Access
{
    internal const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(AllocatorGatherHost434Names.Failure);
    }

    internal static object Get(object target, string name) => target.GetType().GetProperty(name, Flags)?.GetValue(target) ?? target.GetType().GetField(name, Flags)!.GetValue(target)!;

    internal static object? Call(object target, string name, params object[] args)
    {
        try
        {
            return target.GetType().GetMethods(Flags).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(target, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static async Task<ProcessRuntime> Create()
    {
        var runtime = new ProcessRuntime(2);
        try
        {
            await runtime.InitializeModulesAsync(registry => RuntimeBootstrapAllocatorGather434.Register(runtime, registry));

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }

    internal static Task<Term> Query(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        string allocator,
        bool carriers = false
    )
    => actor.InvokeAsync(async context =>
    {
        ReferenceTerm reference = runtime.MakeReference();
        Term count = await runtime.Modules.Call(
            context,
            AllocatorGatherHost434Names.Module,
            carriers ? AllocatorGatherHost434Names.Carriers : AllocatorGatherHost434Names.Hist,
            Term.Tuple(
                Term.A(allocator),
                Term.I(0),
                Term.I(5),
                Term.I(8),
                Term.I(0),
                reference
            )
        );
        Require(count.Equals(Term.I(1)));
        Term? reply = await context.ReceiveAsync(value => value is TupleTerm tuple && tuple.Items[0].Equals(reference) ? value : null, AllocatorGatherHost434Names.Deadline);
        Require(reply is TupleTerm);

        return reply!;
    });

    internal static ulong[] Extents(Term message) => Cons.Items(((TupleTerm)message).Items[1]).Select(row => (ulong)((Integer)((TupleTerm)row).Items[2]).Value).Order().ToArray();
}

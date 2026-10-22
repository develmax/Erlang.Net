using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class InternalTrace425HostAccess
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(InternalTrace425HostNames.Failure);
    }

    internal static Task<Term> Call(RuntimeNodeFoundationHostActor actor, string name, params Term[] args)
        => actor.InvokeAsync(async context => await context.Runtime.Modules.Call(
            context,
            InternalTrace425HostNames.Module,
            name,
            args
        ));

    internal static Task<Term> Create(RuntimeNodeFoundationHostActor actor, Term tracer)
        => Call(
            actor,
            InternalTrace425HostNames.Create,
            Term.A(InternalTrace425HostNames.SessionName),
            tracer,
            Nil.Value
        );

    internal static Task<Term?> ReceiveTrace(RuntimeNodeFoundationHostActor actor, TimeSpan timeout)
        => actor.InvokeAsync(async context => await context.Mailbox.ReceiveAsync<Term>(
            term => term is TupleTerm tuple && tuple.Items.Count > 0 && tuple.Items[0].Equals(Term.A(InternalTrace425HostNames.Message)) ? term : null,
            timeout,
            context.Cancellation
        ));

    internal static int SessionCount(ProcessRuntime runtime)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object root = typeof(ProcessRuntime).GetProperty(InternalTrace425HostNames.TracingOwner, flags)!.GetValue(runtime)!;

        return ((ICollection)root.GetType().GetField(InternalTrace425HostNames.SessionDirectory, flags)!.GetValue(root)!).Count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static async Task<(Term Weak, WeakReference<ResourceReferenceTerm> Resource)> CreateWeak(RuntimeNodeFoundationHostActor actor)
        => await actor.InvokeAsync(async context =>
        {
            var session = (TupleTerm)await context.Runtime.Modules.Call(
                context,
                InternalTrace425HostNames.Module,
                InternalTrace425HostNames.Create,
                Term.A(InternalTrace425HostNames.SessionName),
                Term.A(InternalTrace425HostNames.Undefined),
                Nil.Value
            );

            return (session.Items[1], new WeakReference<ResourceReferenceTerm>((ResourceReferenceTerm)session.Items[0]));
        });
}

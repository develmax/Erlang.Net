using System.Runtime.CompilerServices;
using Erlang;

namespace Erlang.Tests;

internal static partial class EtsCompressed441Checks
{
    internal static async Task SourceRootsAndRelease()
    {
        foreach (EtsCompressedEnding442 ending in Enum.GetValues<EtsCompressedEnding442>())
        {
            await using var runtime = new ProcessRuntime(1);
            Install(runtime);
            PreparedBeamImage prepared = BeamStoredFunction406Access.Publish(runtime);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            object storage = EtsFoundation405Access.Get(runtime);
            Term table = await Create(runtime, actor, EtsCompressed441Names.Kinds[0]);
            var roots = await actor.InvokeAsync(context => StoreDetached442(context, table, prepared.Name));
            BeamStoredFunction406Access.RequireCollected(roots.Function);
            BeamStoredFunction406Access.RequireCollected(roots.Environment);
            Require(BeamStoredFunction406Access.Alive(roots.Binding) && BeamStoredFunction406Access.Alive(roots.Resource));
            await actor.InvokeAsync(context => ReadDetached442(context, table));
            Require(!actor.Process.Completion.IsCompleted && (int)EtsFoundation405Access.Snapshot(storage)[0]! == 1);
            if (ending == EtsCompressedEnding442.Delete)
                await actor.InvokeAsync(async context => await Call(
                    runtime,
                    context,
                    EtsCompressed441Names.Delete,
                    table
                ));
            else if (ending == EtsCompressedEnding442.OwnerExit)
                await actor.DisposeAsync();
            else
            {
                await runtime.DisposeAsync();
            }
            EtsFoundation405Access.Empty(storage, closed: ending == EtsCompressedEnding442.RuntimeClose);
            BeamStoredFunction406Access.RequireCollected(roots.Binding);
            BeamStoredFunction406Access.RequireCollected(roots.Resource);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference<object> Function, WeakReference<object> Environment,
        WeakReference<object> Binding, WeakReference<object> Resource)> StoreDetached442(
            ProcessContext context, Term table, string module)
    {
        ResourceReferenceTerm resource = context.Runtime.ReferenceResources.CreateBinary(BeamStoredFunction406Names.Bytes);
        var shared = Term.Tuple(new BitString(BeamStoredFunction406Names.Bytes));
        var environment = Term.Tuple(shared, shared, resource);
        var function = (FunctionTerm)await context.Runtime.Modules.Call(
            context,
            module,
            BeamClosureHostNames.Make,
            environment
        );
        object binding = BeamStoredFunction406Access.CallFactory(BeamStoredFunction406Names.CaptureBinding, context.Runtime, function);
        await Call(
            context.Runtime,
            context,
            EtsCompressed441Names.Insert,
            table,
            Term.Tuple(function, Term.I(EtsCompressed441Names.KeyField))
        );

        return (new(function), new(environment), new(binding), new(resource));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<bool> ReadDetached442(ProcessContext context, Term table)
    {
        var rows = (Cons)await Call(
            context.Runtime,
            context,
            EtsCompressed441Names.Lookup,
            table,
            Term.I(EtsCompressed441Names.KeyField)
        );
        var function = (FunctionTerm)((TupleTerm)rows.Head).Items[EtsCompressed441Names.ValueField - 1];
        var environment = (TupleTerm)function.Environment.Single();
        Require(ReferenceEquals(environment.Items[0], environment.Items[1]));
        using var binary = context.Runtime.ReferenceResources.AcquireBinary((ReferenceTerm)environment.Items[2]);
        Require(binary is not null && binary.Value.ToArray().SequenceEqual(BeamStoredFunction406Names.Bytes));
        bool raised = false;
        try
        {
            await function.Invoke(context, [Term.I(BeamClosureHostNames.Argument)]);
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArithmetic)))
        {
            raised = true;
        }
        Require(raised);

        return true;
    }
}

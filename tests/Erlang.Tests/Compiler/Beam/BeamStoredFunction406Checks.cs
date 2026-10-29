using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class BeamStoredFunction406Checks
{
    internal static async Task GraphAndAuthenticity()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        PreparedBeamImage prepared = BeamStoredFunction406Access.Publish(runtime);
        BeamStoredFunction406Access.Publish(foreign);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(prepared.Name)!;
        object store = EtsFoundation405Access.Get(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        Term table = await actor.Invoke(context => EtsFoundation405Access.Create(store, context));
        FunctionTerm foreignFunction = await foreignActor.InvokeAsync(async context => (FunctionTerm)await foreign.Modules.Call(
            context,
            prepared.Name,
            BeamClosureHostNames.Make,
            Term.I(BeamClosureHostNames.Capture)
        ));
        await actor.InvokeAsync(async context =>
        {
            FunctionTerm source = (FunctionTerm)await runtime.Modules.Call(
                context,
                prepared.Name,
                BeamClosureHostNames.Make,
                Term.I(BeamClosureHostNames.Capture)
            );
            long slot = EtsFoundation405Access.Store(
                store,
                context,
                table,
                Term.Tuple(source)
            );
            FunctionTerm copy = BeamStoredFunction406Access.Read(
                store,
                context,
                table,
                slot
            );
            var encoding = new EtfEncodingOptions { FunctionOwner = context.Self };
            BeamStoredFunction406Access.Require(!ReferenceEquals(source, copy) && source.Equals(copy)
                && source.GetHashCode() == copy.GetHashCode()
                && ExternalTermFormat.Encode(source, encoding).SequenceEqual(ExternalTermFormat.Encode(copy, encoding)));
            object binding = BeamStoredFunction406Access.CallFactory(BeamStoredFunction406Names.CaptureBinding, runtime, copy);
            object execution = BeamStoredFunction406Access.Property(binding, BeamStoredFunction406Names.Execution);
            object originalExecution = BeamStoredFunction406Access.Property(prepared, BeamStoredFunction406Names.ExecutionIdentity);
            BeamStoredFunction406Access.Require(ReferenceEquals(execution, originalExecution)
                && ReferenceEquals(BeamStoredFunction406Access.Property(execution, BeamStoredFunction406Names.Owner), prepared)
                && ReferenceEquals(BeamStoredFunction406Access.Property(execution, BeamStoredFunction406Names.Image), image)
                && ReferenceEquals(
                    BeamStoredFunction406Access.Property(execution, BeamStoredFunction406Names.NativePrimitives),
                    BeamStoredFunction406Access.Property(originalExecution, BeamStoredFunction406Names.NativePrimitives)
                ));
            _ = BeamStoredFunction406Access.CallFactory(
                BeamStoredFunction406Names.RequireKnown,
                copy,
                BeamStoredFunction406Access.Property(binding, BeamStoredFunction406Names.Code),
                BeamStoredFunction406Access.Property(binding, BeamStoredFunction406Names.TableIndex)
            );
            Term result = await copy.Invoke(context, [Term.I(BeamClosureHostNames.Argument)]);
            BeamStoredFunction406Access.Require(result.Equals(Term.I(BeamClosureHostNames.Result))
                && BeamStoredFunction406Access.Active(context, image) == 0);
            FunctionTerm[] rejected = [
                new FunctionTerm(
                    source.LocalIdentity!,
                    source.Arity,
                    source.Environment,
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                ),
                new FunctionTerm(0, (_, _) => ValueTask.FromResult<Term>(Nil.Value)),
                foreignFunction
            ];
            foreach (FunctionTerm invalid in rejected)
            {
                var before = EtsFoundation405Access.Snapshot(store);
                bool refused = false;
                try
                {
                    EtsFoundation405Access.Store(
                        store,
                        context,
                        table,
                        Term.Tuple(context.Self, invalid)
                    );
                }
                catch (NotSupportedException failure) when (failure.Message == EtsFoundation405Names.UnsupportedFunction)
                {
                    refused = true;
                }
                BeamStoredFunction406Access.Require(refused && before.Equals(EtsFoundation405Access.Snapshot(store)));
            }

            return true;
        });
        var failedResource = await actor.InvokeAsync(context => RejectNestedFunction(
            context,
            store,
            table,
            prepared.Name
        ));
        BeamStoredFunction406Access.RequireCollected(failedResource);
        var detached = await actor.InvokeAsync(context => StoreResourceClosure(
            context,
            store,
            table,
            prepared.Name
        ));
        BeamStoredFunction406Access.RequireCollected(detached.Function);
        BeamStoredFunction406Access.RequireCollected(detached.Environment);
        EtsFoundation405Access.Collect();
        BeamStoredFunction406Access.Require(BeamStoredFunction406Access.Alive(detached.Binding)
            && BeamStoredFunction406Access.Alive(detached.Resource));
        await actor.InvokeAsync(async context =>
        {
            FunctionTerm copied = BeamStoredFunction406Access.Read(
                store,
                context,
                table,
                detached.Slot
            );
            var env = (TupleTerm)copied.Environment.Single();
            BeamStoredFunction406Access.Require(ReferenceEquals(env.Items[0], env.Items[1]));
            using var resource = runtime.ReferenceResources.AcquireBinary((ReferenceTerm)env.Items[2]);
            BeamStoredFunction406Access.Require(resource is not null && resource.Value.ToArray().SequenceEqual(BeamStoredFunction406Names.Bytes));
            try
            {
                _ = await copied.Invoke(context, [Term.I(BeamClosureHostNames.Argument)]);
                throw new InvalidOperationException(BeamStoredFunction406Names.Failure);
            }
            catch (ErlangException failure)
            {
                BeamStoredFunction406Access.Require(failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArithmetic)));
            }
            EtsFoundation405Access.Invoke(
                store,
                EtsFoundation405Names.Destroy,
                context,
                table
            );

            return true;
        });
        EtsFoundation405Access.Empty(store);
        BeamStoredFunction406Access.RequireCollected(detached.Binding);
        BeamStoredFunction406Access.RequireCollected(detached.Resource);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference<object>> RejectNestedFunction(
        ProcessContext context,
        object store,
        Term table,
        string module
    )
    {
        ResourceReferenceTerm resource = context.Runtime.ReferenceResources.CreateBinary(BeamStoredFunction406Names.Bytes);
        var invalid = new FunctionTerm(0, (_, _) => ValueTask.FromResult<Term>(Nil.Value));
        FunctionTerm source = (FunctionTerm)await context.Runtime.Modules.Call(
            context,
            module,
            BeamClosureHostNames.Make,
            Term.Tuple(resource, invalid)
        );
        var before = EtsFoundation405Access.Snapshot(store);
        bool refused = false;
        try
        {
            EtsFoundation405Access.Store(
                store,
                context,
                table,
                Term.Tuple(source)
            );
        }
        catch (NotSupportedException failure) when (failure.Message == EtsFoundation405Names.UnsupportedFunction)
        {
            refused = true;
        }
        BeamStoredFunction406Access.Require(refused && before.Equals(EtsFoundation405Access.Snapshot(store)));

        return new(resource);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference<object> Function, WeakReference<object> Environment, WeakReference<object> Binding,
        WeakReference<object> Resource, long Slot)> StoreResourceClosure(
            ProcessContext context,
            object store,
            Term table,
            string module
        )
    {
        ResourceReferenceTerm resource = context.Runtime.ReferenceResources.CreateBinary(BeamStoredFunction406Names.Bytes);
        var shared = Term.Tuple(new BitString(BeamStoredFunction406Names.Bytes));
        var environment = Term.Tuple(shared, shared, resource);
        FunctionTerm function = (FunctionTerm)await context.Runtime.Modules.Call(
            context,
            module,
            BeamClosureHostNames.Make,
            environment
        );
        object binding = BeamStoredFunction406Access.CallFactory(BeamStoredFunction406Names.CaptureBinding, context.Runtime, function);
        long slot = EtsFoundation405Access.Store(
            store,
            context,
            table,
            Term.Tuple(function)
        );

        return (new(function), new(environment), new(binding), new(resource), slot);
    }

    internal static async Task OriginalImageAndPurge()
    {
        await using var runtime = new ProcessRuntime(1);
        PreparedBeamImage first = BeamStoredFunction406Access.Publish(runtime);
        ModuleCodeImage oldImage = runtime.Modules.CurrentCodeImage(first.Name)!;
        object store = EtsFoundation405Access.Get(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await actor.Invoke(context => EtsFoundation405Access.Create(store, context));
        FunctionTerm? original = null;
        long slot = await actor.InvokeAsync(async context =>
        {
            FunctionTerm function = (FunctionTerm)await runtime.Modules.Call(
                context,
                first.Name,
                BeamClosureHostNames.Make,
                Term.I(BeamClosureHostNames.Capture)
            );
            original = function;

            return EtsFoundation405Access.Store(
                store,
                context,
                table,
                Term.Tuple(function)
            );
        });
        PreparedBeamImage second = BeamStoredFunction406Access.Publish(runtime);
        ModuleCodeImage currentImage = runtime.Modules.CurrentCodeImage(second.Name)!;
        BeamStoredFunction406Access.Require(!ReferenceEquals(currentImage, oldImage));
        FunctionTerm copy = await actor.Invoke(context => BeamStoredFunction406Access.Read(
            store,
            context,
            table,
            slot
        ));
        object binding = BeamStoredFunction406Access.CallFactory(BeamStoredFunction406Names.CaptureBinding, runtime, copy);
        BeamStoredFunction406Access.Require(ReferenceEquals(
            BeamStoredFunction406Access.Property(binding, BeamStoredFunction406Names.Execution),
            BeamStoredFunction406Access.Property(first, BeamStoredFunction406Names.ExecutionIdentity)
        ));
        object session = NifAdmissionHostBridge.Call(runtime.Modules, BeamStoredFunction406Names.PreparePurge, first.Name)!;
        BeamStoredFunction406Access.Require(ReferenceEquals(BeamStoredFunction406Access.Property(session, BeamStoredFunction406Names.Image), oldImage));
        try
        {
            Task<Term> pending = await actor.Invoke(context => copy.Invoke(context, [Term.I(BeamClosureHostNames.Argument)]).AsTask());
            // Identical committed code shares the original fun entry. Purging
            // the previous image does not suspend its current-image dispatch.
            BeamStoredFunction406Access.Require((await pending.WaitAsync(BeamClosurePurgeHostNames.Deadline)).Equals(Term.I(BeamClosureHostNames.Result))
                && BeamStoredFunction406Access.Active(await actor.Ready, oldImage) == 0);
            NifAdmissionHostBridge.Call(
                runtime.Modules,
                BeamStoredFunction406Names.FinishPurge,
                session,
                false
            );
            BeamStoredFunction406Access.Require((await pending.WaitAsync(BeamClosurePurgeHostNames.Deadline)).Equals(Term.I(BeamClosureHostNames.Result)));
        }
        finally
        {
            if (runtime.Modules.PendingPurge(oldImage) is not null)
                NifAdmissionHostBridge.Call(
                    runtime.Modules,
                    BeamStoredFunction406Names.FinishPurge,
                    session,
                    false
                );
        }
        object retired = NifAdmissionHostBridge.Call(runtime.Modules, BeamStoredFunction406Names.PreparePurge, first.Name)!;
        NifAdmissionHostBridge.Call(
            runtime.Modules,
            BeamStoredFunction406Names.FinishPurge,
            retired,
            true
        );
        await actor.InvokeAsync(async context =>
        {
            Term copiedResult = await copy.Invoke(context, [Term.I(BeamClosureHostNames.Argument)]);
            Term originalResult = await original!.Invoke(context, [Term.I(BeamClosureHostNames.Argument)]);
            BeamStoredFunction406Access.Require(copiedResult.Equals(Term.I(BeamClosureHostNames.Result))
                && originalResult.Equals(Term.I(BeamClosureHostNames.Result))
                && BeamStoredFunction406Access.Active(context, oldImage) == 0);
            EtsFoundation405Access.Invoke(
                store,
                EtsFoundation405Names.Destroy,
                context,
                table
            );

            return true;
        });
    }
}

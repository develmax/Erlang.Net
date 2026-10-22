using System.Runtime.CompilerServices;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class GuestStoredFunction407Checks
{
    internal static async Task SourceAndExternal()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        new Parser(GuestStoredFunction407Names.Source).ParseModule().Register(runtime.Modules);
        object storage = EtsFoundation405Access.Get(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await actor.Invoke(context => EtsFoundation405Access.Create(storage, context));
        await actor.InvokeAsync(async context =>
        {
            var shared = Term.Tuple(Term.I(407));
            Term environment = Term.Tuple(shared, shared);
            foreach (string functionName in new[] { GuestStoredFunction407Names.Make, GuestStoredFunction407Names.Named, GuestStoredFunction407Names.External })
            {
                Term[] arguments = functionName == GuestStoredFunction407Names.Make ? [environment] : [];
                var function = (FunctionTerm)await runtime.Modules.Call(
                    context,
                    GuestStoredFunction407Names.ModuleName,
                    functionName,
                    arguments
                );
                FunctionTerm copy = GuestStoredFunction407Access.Copy(
                    storage,
                    context,
                    table,
                    function
                );
                GuestStoredFunction407Access.Equal(function, copy, context.Self);
                if (functionName == GuestStoredFunction407Names.External)
                {
                    GuestStoredFunction407Access.Require(runtime.Functions.IsExternalEntry(copy)
                        && (await copy.Invoke(context, [Cons.From([Term.I(1), Term.I(2)])])).Equals(Term.I(2)));
                    GuestStoredFunction407Access.Refused(
                        storage,
                        context,
                        table,
                        new FunctionTerm(copy.ExternalIdentity!, (_, _) => ValueTask.FromResult<Term>(Nil.Value))
                    );
                }
                else
                {
                    Term value = await copy.Invoke(context, [Term.A(GuestStoredFunction407Names.Marker)]);
                    Term expected = functionName == GuestStoredFunction407Names.Make
                        ? Term.Tuple(environment, Term.A(GuestStoredFunction407Names.Marker))
                        : Term.Tuple(Term.A(GuestStoredFunction407Names.Hidden), Term.A(GuestStoredFunction407Names.Marker));
                    GuestStoredFunction407Access.Require(value.Equals(expected));
                    if (functionName == GuestStoredFunction407Names.Make)
                    {
                        var copiedEnvironment = (TupleTerm)copy.Environment.Single();
                        GuestStoredFunction407Access.Require(!ReferenceEquals(environment, copiedEnvironment)
                            && ReferenceEquals(copiedEnvironment.Items[0], copiedEnvironment.Items[1]));
                    }
                    GuestStoredFunction407Access.Refused(
                        storage,
                        context,
                        table,
                        new FunctionTerm(
                            copy.LocalIdentity!,
                            copy.Arity,
                            copy.Environment,
                            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                        )
                    );
                }
                FunctionTerm restored = (FunctionTerm)runtime.ImportExternalTerm(ExternalTermFormat.Encode(function, new EtfEncodingOptions { FunctionOwner = context.Self })).Term;
                FunctionTerm restoredCopy = GuestStoredFunction407Access.Copy(
                    storage,
                    context,
                    table,
                    restored
                );
                GuestStoredFunction407Access.Equal(restored, restoredCopy, context.Self);
                Term argument = functionName == GuestStoredFunction407Names.External ? Cons.From([Term.I(1)]) : Term.A(GuestStoredFunction407Names.Marker);
                GuestStoredFunction407Access.Require((await restored.Invoke(context, [argument])).Equals(await restoredCopy.Invoke(context, [argument])));
            }
            FunctionTerm interpreted = (FunctionTerm)await Execution.EvaluateAsync(new Parser(GuestStoredFunction407Names.Interpreted).ParseExpression(), context);
            FunctionTerm interpretedCopy = GuestStoredFunction407Access.Copy(
                storage,
                context,
                table,
                interpreted
            );
            GuestStoredFunction407Access.Require((await interpretedCopy.Invoke(context, [Term.I(407)])).Equals(
                Term.Tuple(Term.A(GuestStoredFunction407Names.InterpretedAtom), Term.I(407))));
            FunctionTerm foreignExternal = foreign.Functions.External(foreign, new ExternalFunctionIdentity(
                Term.A(ErlangModuleNames.Module), Term.A(GuestStoredFunction407Names.Length), 1));
            GuestStoredFunction407Access.Refused(
                storage,
                context,
                table,
                foreignExternal
            );

            return true;
        });
        var detached = await actor.InvokeAsync(context => StoreResource(context, storage, table));
        GuestStoredFunction407Access.Collected(detached.Function);
        GuestStoredFunction407Access.Collected(detached.Environment);
        await actor.InvokeAsync(async context =>
        {
            FunctionTerm copy = BeamStoredFunction406Access.Read(
                storage,
                context,
                table,
                detached.Slot
            );
            var returned = (TupleTerm)await copy.Invoke(context, [Nil.Value]);
            var env = (TupleTerm)returned.Items[0];
            using var resource = runtime.ReferenceResources.AcquireBinary((ReferenceTerm)env.Items[0]);
            GuestStoredFunction407Access.Require(resource is not null
                && resource.Value.ToArray().SequenceEqual(GuestStoredFunction407Names.ResourceBytes));
            EtsFoundation405Access.Invoke(
                storage,
                EtsFoundation405Names.Destroy,
                context,
                table
            );

            return true;
        });
        EtsFoundation405Access.Empty(storage);
        GuestStoredFunction407Access.Collected(detached.Resource);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(long Slot, WeakReference<object> Function, WeakReference<object> Environment, WeakReference<object> Resource)> StoreResource(
        ProcessContext context, object storage, Term table
    )
    {
        ResourceReferenceTerm resource = context.Runtime.ReferenceResources.CreateBinary(GuestStoredFunction407Names.ResourceBytes);
        var environment = Term.Tuple(resource, new BitString(GuestStoredFunction407Names.ResourceBytes));
        FunctionTerm function = (FunctionTerm)await context.Runtime.Modules.Call(
            context,
            GuestStoredFunction407Names.ModuleName,
            GuestStoredFunction407Names.Make,
            environment
        );
        long slot = EtsFoundation405Access.Store(
            storage,
            context,
            table,
            Term.Tuple(function)
        );

        return (slot, new(function), new(environment), new(resource));
    }

    internal static async Task RestoredHasNoReadCallback()
    {
        await using var runtime = new ProcessRuntime(1);
        object storage = EtsFoundation405Access.Get(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await actor.Invoke(context => EtsFoundation405Access.Create(storage, context));
        int calls = 0;
        await actor.InvokeAsync(async context =>
        {
            var identity = new LocalFunctionIdentity(
                Term.A(GuestStoredFunction407Names.SdkModule),
                Term.A(GuestStoredFunction407Names.SdkName),
                0,
                0,
                new BitString(new byte[FunctionCodeLayout.UniqueBytes])
            );
            FunctionTerm restored = runtime.Functions.Restore(
                runtime,
                identity,
                0,
                [Term.Tuple(Term.I(407))]
            );
            runtime.Functions.Register(
                identity,
                0,
                1,
                _ =>
            {
                Interlocked.Increment(ref calls);

                return new FunctionTerm(0, (_, _) => ValueTask.FromResult<Term>(Term.A(GuestStoredFunction407Names.SdkResult)));
            }
            );
            FunctionTerm copy = GuestStoredFunction407Access.Copy(
                storage,
                context,
                table,
                restored
            );
            GuestStoredFunction407Access.Require(calls == 0);
            GuestStoredFunction407Access.Equal(restored, copy, context.Self);
            GuestStoredFunction407Access.Require((await copy.Invoke(context, [])).Equals(Term.A(GuestStoredFunction407Names.SdkResult)) && calls == 1);
            FunctionTerm opaque = runtime.Functions.Restore(
                runtime,
                identity,
                0,
                [Nil.Value]
            );
            GuestStoredFunction407Access.Require(calls == 2);
            GuestStoredFunction407Access.Refused(
                storage,
                context,
                table,
                opaque
            );
            GuestStoredFunction407Access.Require(calls == 2);
            EtsFoundation405Access.Invoke(
                storage,
                EtsFoundation405Names.Destroy,
                context,
                table
            );

            return true;
        });
    }

    internal static async Task OriginalBeamRestored()
    {
        await StubBeforeLoad();
        await using var runtime = new ProcessRuntime(1);
        PreparedBeamImage prepared = BeamStoredFunction406Access.Publish(runtime);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(prepared.Name)!;
        object storage = EtsFoundation405Access.Get(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await actor.Invoke(context => EtsFoundation405Access.Create(storage, context));
        FunctionTerm restored = await actor.InvokeAsync(async context =>
        {
            FunctionTerm function = (FunctionTerm)await runtime.Modules.Call(
                context,
                prepared.Name,
                BeamClosureHostNames.Make,
                Term.I(BeamClosureHostNames.Capture)
            );
            var options = new EtfEncodingOptions { FunctionOwner = context.Self };
            var forgedLegacy = new FunctionTerm(
                function.LocalIdentity! with { OldUnique = 1, LegacyIndex = int.MaxValue },
                function.Arity,
                function.Environment,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            FunctionTerm canonical = (FunctionTerm)runtime.ImportExternalTerm(ExternalTermFormat.Encode(forgedLegacy, options)).Term;
            GuestStoredFunction407Access.Equal(function, canonical, context.Self);
            GuestStoredFunction407Access.Require((await canonical.Invoke(context, [Term.I(BeamClosureHostNames.Argument)])).Equals(Term.I(BeamClosureHostNames.Result)));
            FunctionTerm decoded = (FunctionTerm)runtime.ImportExternalTerm(ExternalTermFormat.Encode(function, options)).Term;
            FunctionTerm copy = GuestStoredFunction407Access.Copy(
                storage,
                context,
                table,
                decoded
            );
            GuestStoredFunction407Access.Equal(decoded, copy, context.Self);
            GuestStoredFunction407Access.Require((await copy.Invoke(context, [Term.I(BeamClosureHostNames.Argument)])).Equals(Term.I(BeamClosureHostNames.Result)));
            foreach (FunctionTerm invalid in new[] {
                runtime.Functions.Restore(
                    runtime,
                    function.LocalIdentity!,
                    function.Arity + 1,
                    function.Environment
                ),
                runtime.Functions.Restore(
                    runtime,
                    function.LocalIdentity!,
                    function.Arity,
                    []
                ),
                runtime.Functions.Restore(
                    runtime,
                    function.LocalIdentity! with { Index = int.MaxValue },
                    function.Arity,
                    function.Environment
                )
            })
            {
                FunctionTerm invalidCopy = GuestStoredFunction407Access.Copy(
                    storage,
                    context,
                    table,
                    invalid
                );
                bool badfun = false;
                try
                {
                    _ = await invalidCopy.Invoke(context, Enumerable.Repeat<Term>(Nil.Value, invalidCopy.Arity).ToArray());
                }
                catch (ErlangException failure)
                {
                    badfun = failure.Reason is TupleTerm { Items.Count: 2 } pair && pair.Items[0].Equals(Term.A(ErlangErrorReasons.BadFunction));
                }
                GuestStoredFunction407Access.Require(badfun);
            }

            return copy;
        });
        GuestStoredFunction407Access.Require(runtime.Modules.DeleteModule(prepared.Name).Equals(Term.A(ErlangBooleanAtoms.True)));
        object session = NifAdmissionHostBridge.Call(runtime.Modules, BeamStoredFunction406Names.PreparePurge, prepared.Name)!;
        try
        {
            Task<Term> pending = await actor.Invoke(context => restored.Invoke(context, [Term.I(BeamClosureHostNames.Argument)]).AsTask());
            GuestStoredFunction407Access.Require(!pending.IsCompleted && BeamStoredFunction406Access.Active(await actor.Ready, image) == 0);
            NifAdmissionHostBridge.Call(
                runtime.Modules,
                BeamStoredFunction406Names.FinishPurge,
                session,
                false
            );
            GuestStoredFunction407Access.Require((await pending.WaitAsync(BeamClosurePurgeHostNames.Deadline)).Equals(Term.I(BeamClosureHostNames.Result)));
        }
        finally
        {
            if (runtime.Modules.PendingPurge(image) is not null)
                NifAdmissionHostBridge.Call(
                    runtime.Modules,
                    BeamStoredFunction406Names.FinishPurge,
                    session,
                    false
                );
        }
        object completed = NifAdmissionHostBridge.Call(runtime.Modules, BeamStoredFunction406Names.PreparePurge, prepared.Name)!;
        NifAdmissionHostBridge.Call(
            runtime.Modules,
            BeamStoredFunction406Names.FinishPurge,
            completed,
            true
        );
        GuestStoredFunction407Access.Require(!runtime.Modules.HasOldCode(prepared.Name));
        BeamStoredFunction406Access.Publish(runtime);
        await actor.InvokeAsync(async context =>
        {
            GuestStoredFunction407Access.Require((await restored.Invoke(context, [Term.I(BeamClosureHostNames.Argument)])).Equals(Term.I(BeamClosureHostNames.Result)));
            EtsFoundation405Access.Invoke(
                storage,
                EtsFoundation405Names.Destroy,
                context,
                table
            );

            return true;
        });
    }

    private static async Task StubBeforeLoad()
    {
        await using var runtime = new ProcessRuntime(1);
        object storage = EtsFoundation405Access.Get(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            GuestStoredFunction407Access.Require(!runtime.Modules.IsModuleLoaded(BeamClosureHostNames.Module));
            Term table = EtsFoundation405Access.Create(storage, context);
            var identity = new LocalFunctionIdentity(
                Term.A(BeamClosureHostNames.Module),
                null,
                0,
                1,
                new BitString(Convert.FromHexString(BeamClosureHostNames.Checksum)),
                byte.MaxValue
            );
            var wireOnly = new FunctionTerm(
                identity,
                1,
                [Term.I(BeamClosureHostNames.Capture)],
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            var options = new EtfEncodingOptions { FunctionOwner = context.Self };
            FunctionTerm stub = (FunctionTerm)runtime.ImportExternalTerm(ExternalTermFormat.Encode(wireOnly, options)).Term;
            FunctionTerm stubCopy = GuestStoredFunction407Access.Copy(
                storage,
                context,
                table,
                stub
            );
            BeamStoredFunction406Access.Publish(runtime);
            FunctionTerm direct = (FunctionTerm)await runtime.Modules.Call(
                context,
                BeamClosureHostNames.Module,
                BeamClosureHostNames.Make,
                Term.I(BeamClosureHostNames.Capture)
            );
            GuestStoredFunction407Access.Require(direct.LocalIdentity!.OldUnique == 1 && direct.LocalIdentity.OldIndex == byte.MaxValue);
            FunctionTerm directCopy = GuestStoredFunction407Access.Copy(
                storage,
                context,
                table,
                direct
            );
            GuestStoredFunction407Access.Equal(direct, stubCopy, context.Self);
            GuestStoredFunction407Access.Equal(direct, directCopy, context.Self);
            GuestStoredFunction407Access.Require((await stubCopy.Invoke(context, [Term.I(BeamClosureHostNames.Argument)])).Equals(Term.I(BeamClosureHostNames.Result))
                && (await directCopy.Invoke(context, [Term.I(BeamClosureHostNames.Argument)])).Equals(Term.I(BeamClosureHostNames.Result)));
            EtsFoundation405Access.Invoke(
                storage,
                EtsFoundation405Names.Destroy,
                context,
                table
            );

            return true;
        });
    }
}

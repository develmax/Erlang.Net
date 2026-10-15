using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

public static class NifBeamOwnerHostChecks
{
    public static async Task Run()
    {
        using JsonDocument projection = JsonDocument.Parse(RawOnLoadExportsHostChecks.Resource(RawOnLoadExportsHostNames.Projection));
        foreach (JsonElement beam in projection.RootElement.GetProperty(RawOnLoadExportsHostNames.BeamResources).EnumerateArray())
            Require(Convert.ToHexString(SHA256.HashData(RawOnLoadExportsHostChecks.Resource(beam.GetProperty(RawOnLoadExportsHostNames.Resource).GetString()!))) == beam.GetProperty(RawOnLoadExportsHostNames.Hash).GetString());
        JsonElement cases = projection.RootElement.GetProperty(RawOnLoadExportsHostNames.Cases);
        Require(cases.GetArrayLength() == RawOnLoadExportsHostParameters.OriginalCases);
        foreach (JsonElement test in cases.EnumerateArray())
        {
            string name = test.GetProperty(RawOnLoadExportsHostNames.Name).GetString()!;
            await Observe(name);
            Console.WriteLine(NifBeamOwnerHostNames.Passed + name);
        }
        CollectibleCycle();
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifBeamOwnerHostNames.Broken);
    }

    private static ModuleCodeImage Image(PreparedBeamImage prepared)
            {
        object execution = NifAdmissionHostBridge.Property(prepared, NifBeamOwnerHostNames.Execution);

        return (ModuleCodeImage)execution.GetType().GetProperty(NifAdmissionHostNames.Image, NifBeamOwnerHostNames.PropertyMembers)!.GetValue(execution)!;
    }

    private static async Task Observe(string name)
    {
        bool previous = !name.StartsWith(RawOnLoadExportsHostNames.Fresh, StringComparison.Ordinal);
        bool deleted = name.StartsWith(RawOnLoadExportsHostNames.DeletedOld, StringComparison.Ordinal);
        bool clear = !name.EndsWith(RawOnLoadExportsHostNames.DirectTrue, StringComparison.Ordinal);
        var runtime = new ProcessRuntime();
        var nativeOwner = new NifAdmissionTestOwner();
        PreparedBeamImage? prepared = null;
        ModuleCodeImage? image = null;
        SourceCodeEntry? callbackCaller = null;
        int actualFunEntries = 0;
        try
        {
            Require(!runtime.Modules.HasExport(RawOnLoadExportsHostNames.OsModule, RawOnLoadExportsHostNames.GetEnvironment, 1));
            ErlangFunction environment = async (context, arguments) =>
                {
                    Require(arguments.Count == 1 && arguments[0].Equals(Term.String(RawOnLoadExportsHostNames.EnvironmentKey)));
                    // Actual interpreted on_load callback, not a fabricated owner frame.
                    callbackCaller = (SourceCodeEntry)NifAdmissionHostBridge.Call(context, NifBeamOwnerHostNames.CaptureCaller)!;
                    Require(ReferenceEquals(callbackCaller.Owner, prepared) && ReferenceEquals(callbackCaller.Image, image));
                    object pending = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
                    Require(NifAdmissionHostBridge.Property(pending, NifAdmissionHostNames.Origin).ToString() == NifAdmissionHostNames.Pending);
                    await NifAdmissionHostBridge.Dispose(pending);

                    return Term.A(ErlangBooleanAtoms.False);
                };
            NifAdmissionHostBridge.Call(
                runtime.Modules,
                NifBeamOwnerHostNames.RegisterIntrinsic,
                RawOnLoadExportsHostNames.OsModule,
                RawOnLoadExportsHostNames.GetEnvironment,
                1,
                environment
            );
            await NifAdmissionHostBridge.Context(runtime, async context =>
            {
                if (previous)
                {
                    Require(runtime.Modules.PublishPreparedBeam(runtime.Modules.PrepareBeam(RawOnLoadExportsHostChecks.Resource(RawOnLoadExportsHostNames.OldRole + RawOnLoadExportsHostNames.BeamExtension))).Status == BeamPublicationStatus.Published);
                    if (deleted)
                        runtime.Modules.DeleteModule(RawOnLoadExportsHostNames.Module);
                }
                PreparedBeamImage actualPrepared = runtime.Modules.PrepareBeam(RawOnLoadExportsHostChecks.Resource(RawOnLoadExportsHostNames.NewRole + RawOnLoadExportsHostNames.BeamExtension));
                prepared = actualPrepared;
                ModuleCodeImage exactImage = Image(actualPrepared);
                image = exactImage;
                NifAdmissionHostBridge.Call(runtime.Modules, NifBeamOwnerHostNames.Stage, actualPrepared);
                var pendingTable = (IDictionary)typeof(ModuleRegistry).GetField(NifBeamOwnerHostNames.LoaderPending, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime.Modules)!;
                object ticket = pendingTable[RawOnLoadExportsHostNames.Module]!;
                await runtime.Modules.InvokeLoaderOnLoad(context, Term.A(RawOnLoadExportsHostNames.Module));
                SourceCodeEntry captured = callbackCaller ?? throw new InvalidOperationException(NifBeamOwnerHostNames.Broken);
                Require(ReferenceEquals(captured.Owner, actualPrepared) && !ReferenceEquals(captured.Owner, ticket));
                var function = (FunctionTerm)context.Dictionary[Term.A(RawOnLoadExportsHostNames.Held)];
                if (clear)
                {
                    await Finish(context, false);
                    await Finish(context, false);
                }
                await Finish(context, true);
                Require(ReferenceEquals(runtime.Modules.CurrentCodeImage(RawOnLoadExportsHostNames.Module), exactImage));
                Require(runtime.Modules.HasExport(RawOnLoadExportsHostNames.Module, RawOnLoadExportsHostNames.Body, 0) == !clear);
                Require((await function.Invoke(context, [])).Equals(Term.A(NifBeamOwnerHostNames.FallbackResult)));

                var foreignPrepared = new ModuleRegistry().PrepareBeam(RawOnLoadExportsHostChecks.Resource(RawOnLoadExportsHostNames.NewRole + RawOnLoadExportsHostNames.BeamExtension));
                using (context.EnterSourceCode(exactImage, foreignPrepared))
                    await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifBeamOwnerHostNames.OwnerMismatch);
                using (context.EnterSourceCode(exactImage, ticket))
                    await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifBeamOwnerHostNames.OwnerMismatch);
                using (context.EnterSourceCode(Image(foreignPrepared), foreignPrepared))
                    await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifBeamOwnerHostNames.UnknownImage);

                object temporaryOwner = new();
                var rollback = new InvalidOperationException(NifBeamOwnerHostNames.RollbackFailure);
                try
                {
                    runtime.Modules.Apply(registry =>
                    {
                        registry.RegisterSource(RawOnLoadExportsHostNames.Module, NifBeamOwnerHostNames.RollbackFunction, 0, (_, _) => ValueTask.FromResult<Term>(Nil.Value), temporaryOwner);
                        throw rollback;
                    });
                }
                catch (InvalidOperationException failure) when (ReferenceEquals(failure, rollback))
                {
                }
                Require(!runtime.Modules.HasExport(RawOnLoadExportsHostNames.Module, NifBeamOwnerHostNames.RollbackFunction, 0));
                Require(ReferenceEquals(runtime.Modules.CurrentCodeImage(RawOnLoadExportsHostNames.Module), exactImage));
                using (context.EnterSourceCode(exactImage, temporaryOwner))
                    await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifBeamOwnerHostNames.OwnerMismatch);

                // Re-enter only the identity captured from the real callback above.
                // This is an internal coordinator composition check, not a load_nif BIF.
                using (context.EnterSourceCode(captured.Image, captured.Owner))
                {
                    object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
                    Require(NifAdmissionHostBridge.Property(reservation, NifAdmissionHostNames.Origin).ToString() == NifAdmissionHostNames.Current);
                    try
                    {
                        await NifAdmissionHostBridge.Initialize(reservation, _ => ValueTask.FromResult(NifAdmissionHostBridge.Initialized(
                            nativeOwner,
                            [new KeyValuePair<(string Name, int Arity), ErlangFunction>((RawOnLoadExportsHostNames.Body, 0), async (caller, _) =>
                            {
                                SourceCodeEntry actual = (SourceCodeEntry)NifAdmissionHostBridge.Call(caller, NifBeamOwnerHostNames.CaptureCaller)!;
                                Require(ReferenceEquals(actual.Image, exactImage) && ReferenceEquals(actual.Owner, actualPrepared));
                                Interlocked.Increment(ref actualFunEntries);
                                await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, caller), NifAdmissionHostNames.Reload);

                                return Term.A(NifBeamOwnerHostNames.NativeResult);
                            })]
                        )));
                    }
                    finally
                    {
                        await NifAdmissionHostBridge.Dispose(reservation);
                    }
                }
                Require(nativeOwner.Calls == 0);
                Require((await function.Invoke(context, [])).Equals(Term.A(NifBeamOwnerHostNames.NativeResult)));
                Require(actualFunEntries == 1);
                Require(runtime.Modules.HasExport(RawOnLoadExportsHostNames.Module, RawOnLoadExportsHostNames.Body, 0) == !clear);
                if (clear)
                {
                    if (runtime.Modules.HasOldCode(RawOnLoadExportsHostNames.Module))
                        await Purge(context);
                    runtime.Modules.DeleteModule(RawOnLoadExportsHostNames.Module);
                    using (context.EnterSourceCode(captured.Image, captured.Owner))
                        await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifAdmissionHostNames.OldCode);
                    await Purge(context);
                    await ((Task)NifAdmissionHostBridge.Call(runtime.Modules, NifBeamOwnerHostNames.RetireImage, exactImage)!).WaitAsync(NifAdmissionHostNames.Deadline);
                    Require(nativeOwner.Calls == 1);
                    try
                    {
                        await function.Invoke(context, []);
                        throw new InvalidOperationException(NifBeamOwnerHostNames.Broken);
                    }
                    catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.UndefinedFunction)))
                    {
                    }
                }
            });
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        Require(nativeOwner.Calls == 1);
    }

    private static ValueTask<Term> Finish(ProcessContext context, bool keep)
        => context.Runtime.Modules.Call(context, ErlangModuleNames.Module, BeamLoaderAdmissionNames.FinishOnLoad, Term.A(RawOnLoadExportsHostNames.Module), Term.A(keep ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));

    private static ValueTask<Term> Purge(ProcessContext context)
        => context.Runtime.Modules.Call(context, ErlangModuleNames.Module, CodePurgeHostNames.Purge, Term.A(RawOnLoadExportsHostNames.Module));

    private static void CollectibleCycle()
    {
        WeakReference reference = PreparedCycle();
        for (int attempt = 0; attempt < NifBeamOwnerHostParameters.CollectionAttempts && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Require(!reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference PreparedCycle()
    {
        PreparedBeamImage prepared = new ModuleRegistry().PrepareBeam(RawOnLoadExportsHostChecks.Resource(RawOnLoadExportsHostNames.OldRole + RawOnLoadExportsHostNames.BeamExtension));
        Require(ReferenceEquals(NifAdmissionHostBridge.Property(Image(prepared), NifBeamOwnerHostNames.OwnerProperty), prepared));

        return new WeakReference(prepared);
    }
}

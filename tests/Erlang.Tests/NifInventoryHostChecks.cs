using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

public static class NifInventoryHostChecks
{
    public static async Task Run()
    {
        await CurrentAndCached();
        await PendingPublication(true);
        await PendingPublication(false);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifInventoryHostNames.Broken);
    }

    private static Term Expected(params string[] names) => Cons.From(names.Select(name => Term.Tuple(Term.A(name), Term.I(0))));

    private static IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> Bindings(params string[] names)
        => names.Select(name => new KeyValuePair<(string Name, int Arity), ErlangFunction>(
            (name, 0),
            (_, _) => ValueTask.FromResult<Term>(Term.I(NifInventoryHostNames.Value))
        )).ToArray();

    private static (ModuleCodeImage Image, object Owner) Register(ModuleRegistry registry)
    {
        var owner = new object();
        foreach (string name in new[] { NifInventoryHostNames.First, NifInventoryHostNames.Second })
            registry.RegisterSource(
                NifInventoryHostNames.Module,
                name,
                0,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value),
                owner
            );
        registry.PublishModuleCode(
            NifInventoryHostNames.Module,
            new ModuleCodeDescriptor(
                [(NifInventoryHostNames.First, 0), (NifInventoryHostNames.Second, 0)],
                [(NifInventoryHostNames.First, 0), (NifInventoryHostNames.Second, 0)],
                new BitString(new byte[MD5.HashSizeInBytes]),
                Nil.Value,
                sourceOwner: owner
            )
        );

        return (registry.CurrentCodeImage(NifInventoryHostNames.Module)!, owner);
    }

    private static async Task Install(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        object owner,
        NifAdmissionTestOwner nativeOwner,
        params string[] functions
    )
    {
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            using var flow = context.EnterSourceCode(image, owner);
            object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context).WaitAsync(NifInventoryHostNames.Deadline);
            try
            {
                await NifAdmissionHostBridge.Initialize(reservation, _ => ValueTask.FromResult(NifAdmissionHostBridge.Initialized(nativeOwner, Bindings(functions))))
                    .WaitAsync(NifInventoryHostNames.Deadline);
            }
            finally
            {
                // Dispose joins and rethrows an initialization failure already observed above.
                try
                {
                    await NifAdmissionHostBridge.Dispose(reservation).WaitAsync(NifInventoryHostNames.Deadline);
                }
                catch (ArgumentException) when (nativeOwner.Calls == 1)
                {
                }
            }
        }
        );
    }

    private static async Task CurrentAndCached()
    {
        await using var runtime = new ProcessRuntime();
        var first = Register(runtime.Modules);
        Term before = runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Nifs);
        Term beforeAll = runtime.Modules.ModuleInformation(NifInventoryHostNames.Module);
        Require(before.Equals(Nil.Value));
        Require(ReferenceEquals(beforeAll, runtime.Modules.ModuleInformation(NifInventoryHostNames.Module)));
        var failed = new NifAdmissionTestOwner();
        try
        {
            await Install(
                runtime,
                first.Image,
                first.Owner,
                failed,
                NifInventoryHostNames.Missing
            );
            throw new InvalidOperationException(NifInventoryHostNames.Broken);
        }
        catch (ArgumentException)
        {
        }
        Require(failed.Calls == 1);
        Require(ReferenceEquals(before, runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Nifs)));
        Require(ReferenceEquals(beforeAll, runtime.Modules.ModuleInformation(NifInventoryHostNames.Module)));
        var firstOwner = new NifAdmissionTestOwner();
        await Install(
            runtime,
            first.Image,
            first.Owner,
            firstOwner,
            NifInventoryHostNames.Second,
            NifInventoryHostNames.First
        );
        try
        {
            Term installed = runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Nifs);
            Term installedAll = runtime.Modules.ModuleInformation(NifInventoryHostNames.Module);
            Require(installed.Equals(Expected(NifInventoryHostNames.Second, NifInventoryHostNames.First)));
            Require(before.Equals(Nil.Value) && !ReferenceEquals(beforeAll, installedAll));
            Require(ReferenceEquals(installed, runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Nifs)));
            Require(ReferenceEquals(installedAll, runtime.Modules.ModuleInformation(NifInventoryHostNames.Module)));
            Require(runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Native).Equals(Term.A(NifInventoryHostNames.False)));
            Require(runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.NativeAddresses).Equals(Nil.Value));
            await NifAdmissionHostBridge.Context(
                runtime,
                async context =>
            {
                using var flow = context.EnterSourceCode(first.Image, first.Owner);
                await NifAdmissionHostBridge.Failure(NifAdmissionHostBridge.Reserve(runtime.Modules, context), NifAdmissionHostNames.Reload);
            }
            );
            Require(ReferenceEquals(installed, runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Nifs)));
            runtime.Modules.DeleteModule(NifInventoryHostNames.Module);
            var current = Register(runtime.Modules);
            Require(runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Nifs).Equals(Nil.Value));
            Require(installed.Equals(Expected(NifInventoryHostNames.Second, NifInventoryHostNames.First)));
            await NifAdmissionHostBridge.Retire(first.Image).WaitAsync(NifInventoryHostNames.Deadline);
            Require(firstOwner.Calls == 1);
            var currentOwner = new NifAdmissionTestOwner();
            await Install(
                runtime,
                current.Image,
                current.Owner,
                currentOwner,
                NifInventoryHostNames.First
            );
            Term latest = runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Nifs);
            Require(latest.Equals(Expected(NifInventoryHostNames.First)));
            await NifAdmissionHostBridge.Retire(first.Image).WaitAsync(NifInventoryHostNames.Deadline);
            Require(ReferenceEquals(latest, runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Nifs)));
            await NifAdmissionHostBridge.Retire(current.Image).WaitAsync(NifInventoryHostNames.Deadline);
            Require(currentOwner.Calls == 1 && runtime.Modules.ModuleInformation(NifInventoryHostNames.Module, NifInventoryHostNames.Nifs).Equals(Nil.Value));
            Require(latest.Equals(Expected(NifInventoryHostNames.First)));
        }
        finally
        {
            await NifAdmissionHostBridge.Retire(first.Image).WaitAsync(NifInventoryHostNames.Deadline);
        }
    }

    private static void RequireUnpublished(ModuleRegistry registry)
    {
        try
        {
            registry.ModuleInformation(BeamOnLoadHostNames.FreshSuccessModule, NifInventoryHostNames.Nifs);
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return;
        }
        throw new InvalidOperationException(NifInventoryHostNames.Broken);
    }

    private static async Task PendingPublication(bool keep)
    {
        BeamOnLoadResourceHashChecks.Run();
        await using var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle observer = runtime.Spawn(async context =>
        {
            ready.TrySetResult(context);
            await finish.Task.WaitAsync(NifInventoryHostNames.Deadline);

            return Nil.Value;
        });
        ProcessContext controller = await ready.Task.WaitAsync(NifInventoryHostNames.Deadline);
        runtime.Register(BeamOnLoadHostNames.Observer, observer.Pid);
        Assembly assembly = typeof(NifInventoryHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(NifInventoryHostNames.Resource, StringComparison.Ordinal));
        using Stream resource = assembly.GetManifestResourceStream(name)!;
        using var bytes = new MemoryStream();
        resource.CopyTo(bytes);
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(bytes.ToArray());
        BeamOnLoadTicket ticket = runtime.Modules.StagePreparedOnLoad(prepared);
        ModuleCodeImage image = (ModuleCodeImage)NifAdmissionHostBridge.Property(ticket, NifAdmissionHostNames.CodeImage);
        var nativeOwner = new NifAdmissionTestOwner();
        ProcessHandle? callback = null;
        bool settled = false;
        try
        {
            await Install(
                runtime,
                image,
                ticket,
                nativeOwner,
                BeamOnLoadHostNames.Run
            );
            RequireUnpublished(runtime.Modules);
            callback = runtime.Modules.StartPendingOnLoad(runtime, ticket);
            Term? notice = await controller.ReceiveAsync(
                term => term is TupleTerm { Items.Count: 5 } tuple
                && tuple.Items[0].Equals(Term.A(BeamOnLoadHostNames.Notice)) ? term : null,
                NifInventoryHostNames.Deadline
            );
            Require(notice is not null);
            RequireUnpublished(runtime.Modules);
            runtime.Send(callback.Pid, Term.Tuple(Term.A(BeamOnLoadHostNames.Continue), observer.Pid));
            BeamOnLoadCallbackOutcome outcome = await ticket.Outcome.WaitAsync(NifInventoryHostNames.Deadline);
            await callback.Completion.WaitAsync(NifInventoryHostNames.Deadline);
            await ticket.CallbackQuiescence.WaitAsync(NifInventoryHostNames.Deadline);
            Require(outcome.Failure is null && outcome.Returned!.Equals(Term.A(BeamOnLoadHostNames.Ok)));
            Require(runtime.Modules.FinishPendingOnLoad(ticket, keep) == keep);
            settled = true;
            if (keep)
                Require(runtime.Modules.ModuleInformation(prepared.Name, NifInventoryHostNames.Nifs).Equals(Expected(BeamOnLoadHostNames.Run)));
            else
                RequireUnpublished(runtime.Modules);
        }
        finally
        {
            if (callback is not null)
            {
                runtime.Send(callback.Pid, Term.Tuple(Term.A(BeamOnLoadHostNames.Continue), observer.Pid));
                await callback.Completion.WaitAsync(NifInventoryHostNames.Deadline);
                await ticket.CallbackQuiescence.WaitAsync(NifInventoryHostNames.Deadline);
            }
            if (!settled)
                runtime.Modules.FinishPendingOnLoad(ticket, false);
            await NifAdmissionHostBridge.Retire(image).WaitAsync(NifInventoryHostNames.Deadline);
            finish.TrySetResult();
            await observer.Completion.WaitAsync(NifInventoryHostNames.Deadline);
        }
        Require(nativeOwner.Calls == 1);
    }
}

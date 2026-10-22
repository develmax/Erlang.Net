using Erlang;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using static Erlang.Tests.NetKernel400HostNames;

namespace Erlang.Tests;

internal static class NetKernel400HostChecks
{

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var kernel = new RuntimeNodeFoundationHostActor(runtime);
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        await using var outsider = new RuntimeNodeFoundationHostActor(foreign);
        byte[] bytes = NetKernel400HostAccess.Resource(ImageResource);
        Check(Convert.ToHexString(SHA256.HashData(bytes)) == ImageHash);
        Check(!runtime.Modules.HasExport(ModuleName, Function, 1));
        byte[] importBytes = NetKernel400HostAccess.Resource(ImportsResource);
        Check(Convert.ToHexString(SHA256.HashData(importBytes)) == ImportsHash);
        string[] imports = JsonSerializer.Deserialize<string[]>(importBytes)!;
        var before = LibrarySelectionPolicyNegativeAccess.Bodies(runtime.Modules).ToDictionary(entry => entry.Key, entry => entry.Value);
        string[] loaded = runtime.Modules.LoadedModules.Order(StringComparer.Ordinal).ToArray();
        try
        {
            runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection([]));
            throw new InvalidOperationException(Invariant);
        }
        catch (NotSupportedException)
        {
        }
        Check(!runtime.Modules.HasExport(ModuleName, Function, 1));
        Check(loaded.SequenceEqual(runtime.Modules.LoadedModules.Order(StringComparer.Ordinal)));
        Check(before.Count == LibrarySelectionPolicyNegativeAccess.Bodies(runtime.Modules).Count);
        foreach (var entry in before)
            Check(ReferenceEquals(LibrarySelectionPolicyNegativeAccess.Bodies(runtime.Modules)[entry.Key], entry.Value));

        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(imports));
        Check(image.Name == ModuleName && image.NativePrimitiveExports.Count == 1);
        Check(!runtime.Modules.HasExport(ModuleName, Function, 1));
        Check(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        Check(ReferenceEquals(runtime.Modules.SourceOwner(ModuleName, Function, 1), image));
        Check(ReferenceEquals(runtime.Modules.SourceOwner(ModuleName, ModuleInfo, 1), image));

        Task<Term> Call(Term pid) => actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                ModuleName,
                Function,
                pid
            ));
        Check((await Call(actor.Process.Pid)).Equals(NetKernel400HostAtoms.True));
        Check((await Call(new Pid(runtime.Node, 123, uint.MaxValue))).Equals(NetKernel400HostAtoms.True));
        Check((await Call(new Pid(Remote, 123))).Equals(NetKernel400HostAtoms.False));
        await Reject(Call(Nil.Value));
        await Reject(Call(outsider.Process.Pid));

        await kernel.Invoke(context =>
        {
            runtime.Register(ModuleName, context.Self);
            MethodInfo admission = typeof(ProcessRuntime).GetMethod(Admission, BindingFlags.Instance | BindingFlags.NonPublic)!;
            Check(((Term)admission.Invoke(runtime, [context, Term.A(Local), Term.I(1)])!).Equals(NetKernel400HostAtoms.True));

            return true;
        });
        foreach (bool unicode in new[] { false, true })
        {
            TupleTerm handle = runtime.DistributionPending.BeginPending(Term.A(Remote));
            Check((await Call(new Pid(Remote, 123, 1))).Equals(NetKernel400HostAtoms.False));
            await controller.Invoke(context => runtime.DistributionPending.AdoptConnected(
                handle,
                context,
                DistributionPassThroughFlags.Mandatory | (unicode ? UnicodeFlag : 0),
                1
            ));
            Term expected = unicode ? NetKernel400HostAtoms.True : NetKernel400HostAtoms.False;
            Check((await Call(new Pid(Remote, 123, 1))).Equals(expected));
            Check((await Call(new Pid(Remote, 123, uint.MaxValue))).Equals(expected));
            runtime.DistributionPending.CloseConnected(handle);
            Check((await Call(new Pid(Remote, 123, 1))).Equals(NetKernel400HostAtoms.False));
        }

        await using var retiring = new RuntimeNodeFoundationHostActor(runtime);
        TupleTerm retiringHandle = runtime.DistributionPending.BeginPending(Term.A(Remote));
        await retiring.Invoke(context => runtime.DistributionPending.AdoptConnected(
            retiringHandle,
            context,
            DistributionPassThroughFlags.Mandatory | UnicodeFlag,
            1
        ));
        using (DistributionPendingLease retiringLease = runtime.DistributionPending.Acquire(retiringHandle))
        {
            Action<Exception> reject = (Action<Exception>)typeof(RuntimeDistributionPending)
                .GetMethod(CaptureDeferredFailure, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(runtime.DistributionPending, [retiringHandle])!;
            object runtimeGate = typeof(ProcessRuntime).GetField(RuntimeGate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
            lock (runtimeGate)
            {
                reject(new InvalidDataException(Invariant));
                Check(retiringLease.Snapshot().State == DistributionPendingState.Exiting);
                Term value = (Term)typeof(ProcessRuntime).GetMethod(Query, BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(runtime, [new Pid(Remote, 123, uint.MaxValue)])!;
                Check(value.Equals(NetKernel400HostAtoms.True));
            }
            Task<bool> wake = retiring.InvokeAsync(async context =>
            {
                _ = await context.ReceiveAsync<Term>(message => message, Deadline);

                return true;
            });
            Term termination = await retiring.Process.Completion.WaitAsync(Deadline);
            Check(termination.Equals(NetKernel400HostAtoms.Killed));
            try
            {
                await wake.WaitAsync(Deadline);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ErlangException failure) when (failure.Reason.Equals(NetKernel400HostAtoms.Killed))
            {
            }
            Check((await Call(new Pid(Remote, 123, 1))).Equals(NetKernel400HostAtoms.False));
            Check(retiringLease.Snapshot().State == DistributionPendingState.Idle);
        }

        await using var blocked = new ProcessRuntime();
        ErlangFunction existing = (_, _) => ValueTask.FromResult<Term>(Nil.Value);
        blocked.Modules.Register(
            ModuleName,
            Function,
            1,
            existing
        );
        var blockedBefore = LibrarySelectionPolicyNegativeAccess.Bodies(blocked.Modules).ToDictionary(entry => entry.Key, entry => entry.Value);
        string[] blockedModules = blocked.Modules.LoadedModules.Order(StringComparer.Ordinal).ToArray();
        object? blockedImage = blocked.Modules.CurrentCodeImage(ModuleName);
        try
        {
            blocked.Modules.PrepareBeam(bytes, new BeamDependencySelection(imports));
            throw new InvalidOperationException(Invariant);
        }
        catch (NotSupportedException)
        {
        }
        Check(blockedModules.SequenceEqual(blocked.Modules.LoadedModules.Order(StringComparer.Ordinal)));
        Check(ReferenceEquals(blockedImage, blocked.Modules.CurrentCodeImage(ModuleName)));
        Check(blockedBefore.Count == LibrarySelectionPolicyNegativeAccess.Bodies(blocked.Modules).Count);
        foreach (var entry in blockedBefore)
            Check(ReferenceEquals(LibrarySelectionPolicyNegativeAccess.Bodies(blocked.Modules)[entry.Key], entry.Value));

        Type provider = typeof(ProcessRuntime).Assembly.GetType(ProviderType, true)!;
        MethodInfo capture = provider.GetMethod(Capture, BindingFlags.Static | BindingFlags.NonPublic)!;
        object?[] request = [runtime, ModuleName, Function, 1, null];
        Check((bool)capture.Invoke(null, request)!);
        var body = (ErlangFunction)request[4]!;
        try
        {
            await outsider.InvokeAsync(async context => await body(context, [context.Self]));
            throw new InvalidOperationException(Invariant);
        }
        catch (InvalidOperationException failure) when (failure.Message == ForeignRuntime)
        {
        }
        var expired = new ProcessRuntime();
        await expired.DisposeAsync();
        try
        {
            typeof(ProcessRuntime).GetMethod(Query, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(expired, [new Pid(expired.Node, 123)]);
            throw new InvalidOperationException(Invariant);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is ObjectDisposedException)
        {
        }
        foreach (object?[] refusal in new object?[][]
        {
            [null, ModuleName, Function, 1, null],
            [runtime, ModuleName, Function, 0, null],
            [runtime, WrongModule, Function, 1, null],
            [runtime, ModuleName, WrongFunction, 1, null]
        })
            Check(!(bool)capture.Invoke(null, refusal)! && refusal[4] is null);
    }

    private static async Task Reject(Task<Term> operation)
    {
        try
        {
            await operation;
            throw new InvalidOperationException(Invariant);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(NetKernel400HostAtoms.BadArgument))
        {
        }
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Invariant);
    }
}

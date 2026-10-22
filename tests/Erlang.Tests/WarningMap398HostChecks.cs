using Erlang;
using System.Reflection;
using static Erlang.Tests.WarningMap398HostNames;

namespace Erlang.Tests;

internal static class WarningMap398HostChecks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Invariant);
    }

    private static ProcessRuntime Create(RuntimeWarningMapping mapping) => new(
        ProcessNodeDefaults.Local,
        null,
        ProcessLimit,
        null,
        ActorConcurrency,
        new ProcessRuntimeOptions(mapping)
    );

    internal static async Task Run(ReadOnlyMemory<byte> originalImage, IEnumerable<string> actualImportModules)
    {
        Require(new ProcessRuntimeOptions().WarningMapping == RuntimeWarningMapping.Warning);
        Require(typeof(ProcessRuntimeOptions).GetProperty(nameof(ProcessRuntimeOptions.WarningMapping))!.SetMethod is null);
        try
        {
            _ = new ProcessRuntimeOptions((RuntimeWarningMapping)(-1));
            throw new InvalidOperationException(Invariant);
        }
        catch (ArgumentOutOfRangeException) { }
        try
        {
            _ = new ProcessRuntime(
                ProcessNodeDefaults.Local,
                null,
                ProcessLimit,
                null,
                ActorConcurrency,
                null!
            );
            throw new InvalidOperationException(Invariant);
        }
        catch (ArgumentNullException) { }
        Type helper = typeof(ProcessRuntime).Assembly.GetType(NativeType, true)!;
        MethodInfo capture = helper.GetMethod(CaptureMethod, BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var bad in new[] { ((ProcessRuntime?)null, ModuleName, Function, 0), ((ProcessRuntime?)null, OtherModule, Function, 0) })
        {
            object?[] a = [bad.Item1, bad.Item2, bad.Item3, bad.Item4, null];
            Require(!(bool)capture.Invoke(null, a)! && a[4] is null);
        }
        await using var defaultRuntime = new ProcessRuntime();
        Require(defaultRuntime.WarningMapping == RuntimeWarningMapping.Warning);
        await using var warning = Create(RuntimeWarningMapping.Warning);
        await using var info = Create(RuntimeWarningMapping.Info);
        await using var error = Create(RuntimeWarningMapping.Error);
        var runtimes = new[] { warning, info, error };
        var atoms = new[] { WarningAtom, InfoAtom, ErrorAtom };
        var images = new List<PreparedBeamImage>();
        for (int n = 0; n < runtimes.Length; n++)
        {
            ProcessRuntime runtime = runtimes[n];
            Require(!runtime.Modules.HasExport(ModuleName, Function, 0));
            PreparedBeamImage image = runtime.Modules.PrepareBeam(originalImage, new BeamDependencySelection(actualImportModules));
            Require(image.Name == ModuleName && image.NativePrimitiveExports.Count == 1);
            Require(image.NativePrimitiveExports[0] == new BeamFunctionSignature(Function, 0));
            Require(!runtime.Modules.HasExport(ModuleName, Function, 0));
            Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            Require(ReferenceEquals(runtime.Modules.SourceOwner(ModuleName, Function, 0), image));
            Require(ReferenceEquals(runtime.Modules.SourceOwner(ModuleName, ModuleInfoFunction, 1), image));
            images.Add(image);
        }
        await using (var warningActor = new RuntimeNodeFoundationHostActor(warning))
        await using (var infoActor = new RuntimeNodeFoundationHostActor(info))
        await using (var errorActor = new RuntimeNodeFoundationHostActor(error))
        {
            var actors = new[] { warningActor, infoActor, errorActor };
            for (int n = 0; n < runtimes.Length; n++)
            {
                int index = n;
                Term value = await actors[index].InvokeAsync(async context => await runtimes[index].Modules.Call(context, ModuleName, Function));
                Require(value.Equals(Term.A(atoms[index])));
                Term again = await actors[index].InvokeAsync(async context => await runtimes[index].Modules.Call(context, ModuleName, Function));
                Require(ReferenceEquals(value, again));
                Term exports = await actors[index].InvokeAsync(async context => await runtimes[index].Modules.Call(
                    context,
                    ModuleName,
                    ModuleInfoFunction,
                    Term.A(ExportsMetadata)
                ));
                Require(Cons.Items(exports).Count() == OriginalExportCount);
                Term md5 = await actors[index].InvokeAsync(async context => await runtimes[index].Modules.Call(
                    context,
                    ModuleName,
                    ModuleInfoFunction,
                    Term.A(Md5Metadata)
                ));
                Require(md5.Equals(images[index].NativeMd5));
            }
            ErlangFunction actual = LibrarySelectionPolicyNegativeAccess.Captured(images[0])[(Function, 0)];
            ProcessContext foreign = await infoActor.Ready;
            try
            {
                _ = await actual(foreign, []);
                throw new InvalidOperationException(ForeignContextAdmitted);
            }
            catch (InvalidOperationException e) when (e.Message == ForeignRuntime) { }
            foreach (var bad in new[] { (ModuleName, Function, 1), (ModuleName, OtherModule, 0), (OtherModule, Function, 0) })
            {
                object?[] a = [warning, bad.Item1, bad.Item2, bad.Item3, null];
                Require(!(bool)capture.Invoke(null, a)! && a[4] is null);
            }
            var checks = Enumerable.Range(0, 24).Select(async n =>
            {
                int i = n % 3;
                Term value = await actors[i].InvokeAsync(async c => await runtimes[i].Modules.Call(c, ModuleName, Function));
                Require(value.Equals(Term.A(atoms[i])));
            });
            await Task.WhenAll(checks);
        }
        // The actual captured callback cannot revive an owner after shutdown.
        ErlangFunction retained = LibrarySelectionPolicyNegativeAccess.Captured(images[0])[(Function, 0)];
        ProcessContext saved;
        await using (var actor = new RuntimeNodeFoundationHostActor(warning)) saved = await actor.Ready;
        await warning.DisposeAsync();
        MethodInfo read = typeof(ProcessRuntime).GetMethod(ReadPolicyMethod, BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            _ = read.Invoke(warning, []);
            throw new InvalidOperationException(ClosedPolicyReadAdmitted);
        }
        catch (TargetInvocationException e) when (e.InnerException is ObjectDisposedException) { }
        try
        {
            _ = await retained(saved, []);
            throw new InvalidOperationException(DisposedOwnerAdmitted);
        }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
    }
}

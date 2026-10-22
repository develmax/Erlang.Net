using System.Reflection;
using System.Text.Json;
using Erlang;
using static Erlang.Tests.IoPrintableRange404Names;

namespace Erlang.Tests;

internal static class IoPrintableRange404Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Invariant);
    }

    private static ProcessRuntime Create(RuntimePrintableRange range) => new(
        ProcessNodeDefaults.Local,
        null,
        ProcessLimit,
        null,
        ActorConcurrency,
        new ProcessRuntimeOptions(RuntimeWarningMapping.Warning, range)
    );

    internal static async Task Run()
    {
        IoPrintableRange404Checks.Require(new ProcessRuntimeOptions().PrintableRange == RuntimePrintableRange.Latin1);
        // These exact legacy client source forms must compile without an overload ambiguity.
        ProcessRuntimeOptions positionalDefault = new ProcessRuntimeOptions(default);
        ProcessRuntimeOptions positionalZero = new ProcessRuntimeOptions(0);
        IoPrintableRange404Checks.Require(positionalDefault.WarningMapping == RuntimeWarningMapping.Warning
            && positionalDefault.PrintableRange == RuntimePrintableRange.Latin1);
        IoPrintableRange404Checks.Require(positionalZero.WarningMapping == RuntimeWarningMapping.Warning
            && positionalZero.PrintableRange == RuntimePrintableRange.Latin1);
        IoPrintableRange404Checks.Require(typeof(ProcessRuntimeOptions).GetConstructor([typeof(RuntimeWarningMapping)]) is not null);
        IoPrintableRange404Checks.Require(typeof(ProcessRuntimeOptions).GetConstructor([typeof(RuntimePrintableRange)]) is null);
        IoPrintableRange404Checks.Require(typeof(ProcessRuntimeOptions).GetProperty(nameof(ProcessRuntimeOptions.PrintableRange))!.SetMethod is null);
        foreach (int invalid in new[] { -1, 2, int.MaxValue })
        {
            bool rejected = false;
            try
            {
                _ = new ProcessRuntimeOptions(RuntimeWarningMapping.Warning, (RuntimePrintableRange)invalid);
            }
            catch (ArgumentOutOfRangeException)
            {
                rejected = true;
            }
            IoPrintableRange404Checks.Require(rejected);
        }
        byte[] bytes = LibraryDependencyOriginalHostChecks.ReadRetainedResource(ImageResource);
        string[] imports = JsonSerializer.Deserialize<string[]>(LibraryDependencyOriginalHostChecks.ReadRetainedResource(ImportsResource))!;
        await using var defaults = new ProcessRuntime();
        await using var latin1 = Create(RuntimePrintableRange.Latin1);
        await using var unicode = Create(RuntimePrintableRange.Unicode);
        var runtimes = new[] { defaults, latin1, unicode };
        var expected = new Atom[] { IoPrintableRange404Atoms.Latin1, IoPrintableRange404Atoms.Latin1, IoPrintableRange404Atoms.Unicode };
        var images = new List<PreparedBeamImage>();
        foreach (ProcessRuntime runtime in runtimes)
        {
            IoPrintableRange404Checks.Require(!runtime.Modules.HasExport(ModuleName, Function, 0));
            PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection(imports));
            IoPrintableRange404Checks.Require(image.Name == ModuleName && image.NativePrimitiveExports.Count == 1
                && image.NativePrimitiveExports[0] == new BeamFunctionSignature(Function, 0));
            IoPrintableRange404Checks.Require(!runtime.Modules.HasExport(ModuleName, Function, 0));
            IoPrintableRange404Checks.Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            IoPrintableRange404Checks.Require(ReferenceEquals(runtime.Modules.SourceOwner(ModuleName, Function, 0), image));
            IoPrintableRange404Checks.Require(ReferenceEquals(runtime.Modules.SourceOwner(ModuleName, ModuleInfo, 1), image));
            images.Add(image);
        }
        ProcessContext? canceledContext = null;
        ErlangFunction? canceledBody = null;
        await using (var defaultActor = new RuntimeNodeFoundationHostActor(defaults))
        await using (var latinActor = new RuntimeNodeFoundationHostActor(latin1))
        await using (var unicodeActor = new RuntimeNodeFoundationHostActor(unicode))
        {
            var actors = new[] { defaultActor, latinActor, unicodeActor };
            for (int index = 0; index < actors.Length; index++)
            {
                int n = index;
                Term first = await actors[n].InvokeAsync(async context => await runtimes[n].Modules.Call(context, ModuleName, Function));
                Term again = await actors[n].InvokeAsync(async context => await runtimes[n].Modules.Call(context, ModuleName, Function));
                IoPrintableRange404Checks.Require(first.Equals(expected[n]) && ReferenceEquals(first, again));
            }
            ErlangFunction retained = LibrarySelectionPolicyNegativeAccess.Captured(images[0])[(Function, 0)];
            ProcessContext foreign = await unicodeActor.Ready;
            bool foreignRejected = false;
            try
            {
                _ = await retained(foreign, []);
            }
            catch (InvalidOperationException error) when (error.Message == ForeignRuntime)
            {
                foreignRejected = true;
            }
            IoPrintableRange404Checks.Require(foreignRejected);
            canceledContext = await defaultActor.Ready;
            canceledBody = retained;
        }
        // Actual actor completion cancels this ProcessContext while its owner runtime is still open.
        IoPrintableRange404Checks.Require(canceledContext!.Cancellation.IsCancellationRequested);
        bool canceledRejected = false;
        try
        {
            _ = await canceledBody!(canceledContext, []);
        }
        catch (OperationCanceledException)
        {
            canceledRejected = true;
        }
        IoPrintableRange404Checks.Require(canceledRejected);
        MethodInfo capture = typeof(ProcessRuntime).Assembly.GetType(NativeType, true)!.GetMethod(CaptureMethod, BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var signature in new[] { ((ProcessRuntime?)null, ModuleName, Function, 0), (defaults, OtherModule, Function, 0), (defaults, ModuleName, OtherFunction, 0), (defaults, ModuleName, Function, 1) })
        {
            object?[] arguments = [signature.Item1, signature.Item2, signature.Item3, signature.Item4, null];
            IoPrintableRange404Checks.Require(!(bool)capture.Invoke(null, arguments)! && arguments[4] is null);
        }
        await latin1.DisposeAsync();
        MethodInfo read = typeof(ProcessRuntime).GetMethod(ReadMethod, BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool disposedRejected = false;
        try
        {
            _ = read.Invoke(latin1, null);
        }
        catch (TargetInvocationException error) when (error.InnerException is ObjectDisposedException)
        {
            disposedRejected = true;
        }
        IoPrintableRange404Checks.Require(disposedRejected);
    }
}

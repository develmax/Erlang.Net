using System.Reflection;

namespace Erlang.Tests;

internal static class LibrarySelectionCancellationPhaseAccess
{
    internal static object RuntimeGate(ProcessRuntime runtime)
        => Field(runtime, LibrarySelectionCancellationPhaseNames.RuntimeGate);

    internal static SemaphoreSlim Purger(ProcessRuntime runtime)
        => (SemaphoreSlim)Field(runtime, LibrarySelectionCancellationPhaseNames.Purger);

    internal static ProcessContext[] ProcessesWhileLocked(ProcessRuntime runtime)
        => ((Dictionary<Pid, ProcessContext>)Field(runtime, LibrarySelectionCancellationPhaseNames.Processes)).Values.ToArray();

    internal static object Prepare(ModuleRegistry registry, RuntimeLibrarySelection request)
        => typeof(ModuleRegistry).GetMethod(LibrarySelectionCancellationPhaseNames.Prepare, LibrarySelectionCancellationPhaseNames.Members)!
            .Invoke(registry, [request])!;

    internal static void Publish(ModuleRegistry registry, object plan)
        => typeof(ModuleRegistry).GetMethod(LibrarySelectionCancellationPhaseNames.Publish, LibrarySelectionCancellationPhaseNames.Members)!
            .Invoke(registry, [plan]);

    internal static Task<RuntimeLibrarySelectionSnapshot> Finish(ProcessRuntime runtime, object plan, CancellationToken cancellation)
        => (Task<RuntimeLibrarySelectionSnapshot>)typeof(ProcessRuntime)
            .GetMethod(LibrarySelectionCancellationPhaseNames.Finish, LibrarySelectionCancellationPhaseNames.Members)!
            .Invoke(runtime, [plan, cancellation])!;

    internal static PropertyInfo Signals => typeof(Mailbox)
        .GetProperty(LibrarySelectionCancellationPhaseNames.Signals, LibrarySelectionCancellationPhaseNames.Members)!;

    internal static Task<Term> Done(ProcessContext context)
        => ((TaskCompletionSource<Term>)typeof(ProcessContext)
            .GetProperty(LibrarySelectionCancellationPhaseNames.Done, LibrarySelectionCancellationPhaseNames.Members)!
            .GetValue(context)!).Task;

    internal static Task Unwound(ProcessContext context)
        => ((TaskCompletionSource)typeof(ProcessContext)
            .GetProperty(LibrarySelectionCancellationPhaseNames.Unwound, LibrarySelectionCancellationPhaseNames.Members)!
            .GetValue(context)!).Task;

    internal static int ActiveLeases(ProcessContext context)
    {
        lock (Field(context, LibrarySelectionCancellationPhaseNames.CodeGate))
        {
            var entries = (Dictionary<ModuleCodeImage, int>)Field(context, LibrarySelectionCancellationPhaseNames.ActiveCode);

            return entries.Values.Sum();
        }
    }

    private static object Field(object target, string name)
        => target.GetType().GetField(name, LibrarySelectionCancellationPhaseNames.Members)!.GetValue(target)!;
}

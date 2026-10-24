using System.Reflection;

namespace Erlang.Tests;

internal static class LibrarySelectionPublicCancellationAccess
{
    internal static AsyncLocal<Action<string, ProcessRuntime, ProcessContext?>?> Observer()
        => (AsyncLocal<Action<string, ProcessRuntime, ProcessContext?>?>)typeof(ProcessRuntime)
            .GetField(LibrarySelectionPublicCancellationNames.ObserverField, LibrarySelectionPublicCancellationNames.StaticMembers)!
            .GetValue(null)!;

    internal static Task Dispatcher(ProcessRuntime runtime)
        => (Task)typeof(ProcessRuntime).GetProperty(
            LibrarySelectionPublicCancellationNames.DispatcherCompletion,
            LibrarySelectionPublicCancellationNames.InstanceMembers
        )!.GetValue(runtime)!;

    internal static Task Initializer(ProcessRuntime runtime)
    {
        object initializer = typeof(ProcessRuntime).GetField(
            LibrarySelectionPublicCancellationNames.Initializer,
            LibrarySelectionPublicCancellationNames.InstanceMembers
        )!.GetValue(runtime)!;

        return (Task)initializer.GetType().GetProperty(
            LibrarySelectionPublicCancellationNames.Completion,
            LibrarySelectionPublicCancellationNames.InstanceMembers
        )!.GetValue(initializer)!;
    }

    internal static bool Disposed(ProcessRuntime runtime)
        => (bool)typeof(ProcessRuntime).GetField(
            LibrarySelectionPublicCancellationNames.Disposed,
            LibrarySelectionPublicCancellationNames.InstanceMembers
        )!.GetValue(runtime)!;
}

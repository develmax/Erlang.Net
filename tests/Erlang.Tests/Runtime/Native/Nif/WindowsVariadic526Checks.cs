using System.Reflection;
using System.Runtime.InteropServices;
using static Erlang.Tests.WindowsVariadic526Names;

namespace Erlang.Tests;

internal static class WindowsVariadic526Checks
{
    internal static Task RequiredSlots()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            return Task.CompletedTask;
        const BindingFlags members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = typeof(ProcessRuntime).Assembly.GetType(Table)!;
        using var owner = (IDisposable)Activator.CreateInstance(
            type,
            members,
            null,
            [false],
            null
        )!;
        nint slots = (nint)type.GetProperty(PointerProperty, members)!.GetValue(owner)!;
        if (Marshal.ReadIntPtr(slots, TupleSlot * IntPtr.Size) == 0
            || Marshal.ReadIntPtr(slots, ListSlot * IntPtr.Size) == 0
            || Marshal.ReadIntPtr(slots, ThreadSlot * IntPtr.Size) == 0)
            throw new InvalidOperationException(Failure);

        return Task.CompletedTask;
    }
}

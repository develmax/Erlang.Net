using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeHostScope531Names;

namespace Erlang.Tests;

internal static class NativeHostScope531Access
{
    private static Type EnvironmentType531 => typeof(ProcessRuntime).Assembly.GetType(EnvironmentType, true)!;

    internal static IDisposable Environment(ProcessRuntime runtime, object provider)
        => (IDisposable)Activator.CreateInstance(
            EnvironmentType531,
            Members,
            null,
            new[] { (object)runtime, NativeResource524Access.Property(provider, Image) },
            null
        )!;

    internal static int Environments => (int)EnvironmentType531.GetProperty(ActiveCount, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

    internal static int ScopeRoots(NativeTermHostScope524 scope)
        => ((ICollection)typeof(NativeTermHostScope524).GetField(Roots, Members)!.GetValue(scope)!).Count;

    internal static long PayloadRoots(object payload)
    {
        object gate = payload.GetType().GetField(Gate, Members)!.GetValue(payload)!;
        lock (gate)
            return (long)payload.GetType().GetField(Roots, Members)!.GetValue(payload)!;
    }

    internal static long PhysicalReferences(object payload)
    {
        Type layout = typeof(ProcessRuntime).Assembly.GetType(Layout, true)!;
        int offset = (int)layout.GetProperty(UserOffset, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        nint user = (nint)NativeResource524Access.Property(payload, NativeHostScope531Names.Pointer);

        return Marshal.ReadInt64(user - offset, NativeResource527TestLayout.ReferenceCount);
    }

    internal static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}

using System.Reflection;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;

namespace Erlang.Tests;

internal static class NativeResource536Access
{
    internal const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeThreadType536();

    internal static int ThreadType()
    {
        Type callbacks = typeof(ProcessRuntime).Assembly.GetType(NativeResource536TestNames.CallbackType, true)!;
        nint pointer = callbacks.GetMethod(NativeResource536TestNames.Read, Static)!.MethodHandle.GetFunctionPointer();

        return Marshal.GetDelegateForFunctionPointer<NativeThreadType536>(pointer)();
    }

    internal static int AuxiliaryLane()
        => (int)typeof(ProcessRuntime).Assembly.GetType(NativeResource536TestNames.AuxiliaryScope, true)!
            .GetProperty(NativeResource536TestNames.Lane, Static)!.GetValue(null)!;

    internal static bool ActualSelectedAuxiliary(ProcessRuntime runtime, int lane)
    {
        object dispatcher = typeof(ProcessRuntime).GetField(NativeResource536TestNames.Dispatcher, Instance)!.GetValue(runtime)!;
        Array lanes = (Array)dispatcher.GetType().GetField(NativeResource536TestNames.Lanes, Instance)!.GetValue(dispatcher)!;
        object selected = lanes.GetValue(lane - 1)!;

        return (bool)Property(selected, NativeResource536TestNames.AuxiliaryRunning)
            && PropertyOrNull(selected, NativeResource536TestNames.Running) is null;
    }

    private static object? PropertyOrNull(object value, string name)
        => value.GetType().GetProperty(name, Instance)!.GetValue(value);

    internal static int BrokerThread(ProcessRuntime runtime)
    {
        object domain = Property(runtime, NativeResource524Names.Domain);
        var worker = (Thread)domain.GetType().GetField(NativeResource536TestNames.Worker, Instance)!.GetValue(domain)!;

        return worker.ManagedThreadId;
    }

    internal static void ReleaseViaGuestGc(ProcessContext context, object payload, BitString binary)
    {
        object heap = Property(context, NativeResource536TestNames.Heap);
        using var frame = (IDisposable)Call(heap, NativeResource536TestNames.Enter)!;
        int slot = (int)Call(frame, NativeResource536TestNames.Reserve)!;
        Call(
            frame,
            NativeResource536TestNames.Set,
            slot,
            binary
        );
        Call(payload, NativeResource536TestNames.ReleaseNative);
        Call(frame, NativeResource536TestNames.Clear, slot);
        Call(heap, NativeResource536TestNames.Collect);
    }
}

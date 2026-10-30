using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.NifNativeBackend;

internal static class OrdinaryBinaryWritableTestAccess
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags Statics = BindingFlags.Static | BindingFlags.NonPublic;

    internal static object Backing(BitString value) => typeof(BitString).GetField(OrdinaryBinaryWritableCaseNames.Backing, Members)!.GetValue(value)!;

    internal static object? TermWriter(BitString value) => typeof(BitString).GetField(OrdinaryBinaryWritableCaseNames.Writer, Members)!.GetValue(value);

    internal static object? BackingWriter(BitString value) => Backing(value).GetType().GetField(OrdinaryBinaryWritableCaseNames.Writer, Members)!.GetValue(Backing(value));

    internal static int Capacity(BitString value) => (int)typeof(BitString).GetProperty(OrdinaryBinaryWritableCaseNames.Referenced, Members)!.GetValue(value)!;

    internal static bool ImmutableCaptureAllowed(BitString value) => (bool)Backing(value).GetType().GetProperty(OrdinaryBinaryWritableCaseNames.CaptureImmutable, Members)!.GetValue(Backing(value))!;

    internal static object? Native(BitString value) => Backing(value).GetType().GetProperty(OrdinaryBinaryWritableCaseNames.NativeStorage, Members)!.GetValue(Backing(value));

    internal static BitString Initialize() => (BitString)Invoke(
        typeof(BitString).GetMethod(OrdinaryBinaryWritableCaseNames.Initialize, Statics)!,
        null,
        [OrdinaryBinaryWritableCaseNames.InitialCapacity]
    )!;

    internal static BitString Append(BitString prefix, params byte[] complete) => (BitString)Invoke(typeof(BitString).GetMethod(OrdinaryBinaryWritableCaseNames.Append, Statics)!, null, [prefix, new BitString(complete)])!;

    internal static void PinSharing(BitString value) => Invoke(typeof(BitString).GetMethod(OrdinaryBinaryWritableCaseNames.PinSharing, Members)!, value, []);

    private static object? Invoke(MethodInfo method, object? target, object?[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}
